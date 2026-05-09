#!/usr/bin/env python3
"""
Black-box E2E test for app-bound access/refresh tokens on the direct login path.

The primary client must be allowed to use /identity/v1/auth/token, which means its
AppScopes include ept:token and gt:password. Confidential clients also need a
valid client secret. The secondary client is used only to prove that a refresh
token issued to the primary client cannot be exchanged by another app.

Example:
  cd /Users/VanAnh/WorkSpace/Personal/alfred-project/alfred-identity-service
  APP_BOUND_E2E_IDENTITY=admin APP_BOUND_E2E_CREDENTIAL=123123 \\
  OIDC_CLIENT_SECRET=alfred-client-secret-2026 \\
  python3 tests/e2e/test_app_bound_direct_login_flow.py --insecure-tls
"""
from __future__ import annotations

import argparse
import os
import ssl
import sys

from dataclasses import dataclass
from pathlib import Path
from typing import Any

from oidc_sso_e2e_base import BrowserClient
from oidc_sso_e2e_base import E2eFailure
from oidc_sso_e2e_base import Recorder
from oidc_sso_e2e_base import build_forward_headers
from oidc_sso_e2e_base import decode_jwt_payload
from oidc_sso_e2e_base import ensure
from oidc_sso_e2e_base import new_run_id
from oidc_sso_e2e_base import recorder_payload
from oidc_sso_e2e_base import unwrap_api_response
from oidc_sso_e2e_base import utc_now_iso
from oidc_sso_e2e_base import write_run_log


DEFAULT_BASE_URL = "https://gateway.test"


@dataclass(frozen=True)
class OAuthClient:
    client_id: str
    credential: str | None
    expected_app_id: str | None


@dataclass(frozen=True)
class ScenarioConfig:
    base_url: str
    primary: OAuthClient
    secondary: OAuthClient
    identity: str
    credential: str
    timeout_seconds: int
    insecure_tls: bool


class AppBoundDirectLoginScenario:
    def __init__(self, config: ScenarioConfig) -> None:
        self.config = config
        self.run_id = new_run_id()
        self.recorder = Recorder()
        self.completed_flows: list[str] = []
        self.primary_tokens: dict[str, str] = {}
        self.safe_claims: list[dict[str, Any]] = []

        ssl_context = None
        if config.insecure_tls:
            ssl_context = ssl._create_unverified_context()

        self.api = BrowserClient(
            config.base_url,
            timeout=config.timeout_seconds,
            ssl_context=ssl_context,
            default_headers=build_forward_headers(config.base_url),
        )

    @property
    def log_directory(self) -> Path:
        return Path(__file__).resolve().parent.parent / "test-logs" / "e2e" / "app-bound-direct-login"

    def run(self) -> Path:
        failure: dict[str, Any] | None = None
        exit_error: Exception | None = None

        try:
            self.direct_login_requires_client_id()
            self.direct_login_primary_client()
            self.reject_cross_client_refresh_grant()
            self.refresh_primary_client_preserves_binding()
        except Exception as exc:
            exit_error = exc
            failure = {
                "type": exc.__class__.__name__,
                "message": str(exc),
            }
            self.recorder.fail("scenario", str(exc))

        log_path = write_run_log(
            self.log_directory,
            {
                "runTimestamp": utc_now_iso(),
                "service": "alfred-identity-service",
                "module": "app-bound-direct-login",
                "script": "tests/e2e/test_app_bound_direct_login_flow.py",
                "environmentTarget": self.config.base_url,
                "actors": {
                    "primaryClientId": self.config.primary.client_id,
                    "secondaryClientId": self.config.secondary.client_id,
                    "identity": self.config.identity,
                },
                "executedFlows": self.completed_flows,
                "observedTokenClaims": self.safe_claims,
                "summary": self.recorder.summary(),
                "steps": recorder_payload(self.recorder),
                "failure": failure,
            },
            run_id=self.run_id,
        )

        print(f"Run log: {log_path}")

        if exit_error is not None:
            raise exit_error

        return log_path

    def direct_login_requires_client_id(self) -> None:
        body: dict[str, Any] = {
            "identity": self.config.identity,
            "password": self.config.credential,
        }
        response = self.api.request("POST", "/identity/v1/auth/token", json_body=body)
        ensure(response.status in {400, 401}, f"Direct login without client_id returned HTTP {response.status}")

        self.completed_flows.append("direct-login-missing-client-id-rejected")
        self.recorder.ok("direct_login_requires_client_id", "Direct login rejects app-less token issuance")

    def direct_login_primary_client(self) -> None:
        body = self.direct_login_body(self.config.primary)
        response = self.api.request("POST", "/identity/v1/auth/token", json_body=body)
        ensure(
            response.status == 200,
            "Direct login failed for the primary client. "
            "Verify the client is active, the user is assigned, the secret is valid, "
            f"and AppScopes include ept:token + gt:password. Detail: {error_detail(response)}",
        )

        result = unwrap_api_response(response) or {}
        ensure(isinstance(result, dict), "Direct login result was not a JSON object")

        access_token = result.get("accessToken") or result.get("access_token")
        refresh_token = result.get("refreshToken") or result.get("refresh_token")
        ensure(isinstance(access_token, str) and access_token, "Direct login did not return an access token")
        ensure(isinstance(refresh_token, str) and refresh_token, "Direct login did not return a refresh token")
        ensure((result.get("tokenType") or result.get("token_type")) == "Bearer", "Direct login returned an unexpected token type")

        claims = self.assert_app_bound_access_token(
            access_token,
            self.config.primary,
            step_name="direct login access token",
        )
        self.primary_tokens = {
            "access_token": access_token,
            "refresh_token": refresh_token,
        }

        self.completed_flows.append("direct-login-primary-client")
        self.recorder.ok(
            "direct_login_primary_client",
            f"Direct login issued AT/RT bound to {self.config.primary.client_id} (app_id={claims.get('app_id')})",
        )

    def reject_cross_client_refresh_grant(self) -> None:
        refresh_token = self.primary_tokens.get("refresh_token")
        ensure(isinstance(refresh_token, str) and refresh_token, "No primary refresh token available")

        form_body = self.refresh_body(self.config.secondary, refresh_token)
        response = self.api.request("POST", "/connect/token", form_body=form_body)
        ensure(response.status == 400, f"Cross-client refresh returned HTTP {response.status}: {error_detail(response)}")
        ensure(isinstance(response.body, dict), "Cross-client refresh did not return a JSON error")

        error = response.body.get("error")
        if error == "invalid_client":
            raise E2eFailure(
                "Secondary client authentication failed before app-binding validation. "
                "Supply --secondary-client-credential for confidential clients."
            )

        ensure(error == "invalid_grant", f"Cross-client refresh returned {response.body!r}")

        self.completed_flows.append("cross-client-refresh-rejected")
        self.recorder.ok("reject_cross_client_refresh_grant", "A refresh token cannot be exchanged by another OAuth client")

    def refresh_primary_client_preserves_binding(self) -> None:
        refresh_token = self.primary_tokens.get("refresh_token")
        ensure(isinstance(refresh_token, str) and refresh_token, "No primary refresh token available")

        form_body = self.refresh_body(self.config.primary, refresh_token)
        response = self.api.request("POST", "/connect/token", form_body=form_body)
        ensure(response.status == 200, f"Same-client refresh failed: {error_detail(response)}")
        ensure(isinstance(response.body, dict), "Same-client refresh did not return JSON")

        next_access_token = response.body.get("access_token")
        next_refresh_token = response.body.get("refresh_token")
        ensure(isinstance(next_access_token, str) and next_access_token, "Refresh did not return an access token")
        ensure(isinstance(next_refresh_token, str) and next_refresh_token, "Refresh did not return a refresh token")
        ensure(next_refresh_token != refresh_token, "Refresh token was not rotated")

        self.assert_app_bound_access_token(
            next_access_token,
            self.config.primary,
            step_name="refresh grant access token",
        )

        self.completed_flows.append("same-client-refresh-preserved-binding")
        self.recorder.ok("refresh_primary_client_preserves_binding", "Same-client refresh rotates RT and preserves AT app binding")

    def direct_login_body(self, client: OAuthClient) -> dict[str, Any]:
        body: dict[str, Any] = {
            "client_id": client.client_id,
            "identity": self.config.identity,
            "password": self.config.credential,
        }
        if client.credential:
            body["client_secret"] = client.credential

        return body

    @staticmethod
    def refresh_body(client: OAuthClient, refresh_token: str) -> dict[str, Any]:
        body: dict[str, Any] = {
            "grant_type": "refresh_token",
            "client_id": client.client_id,
            "refresh_token": refresh_token,
        }
        if client.credential:
            body["client_secret"] = client.credential

        return body

    def assert_app_bound_access_token(self, token: str, client: OAuthClient, *, step_name: str) -> dict[str, Any]:
        payload = decode_jwt_payload(token)
        audience = payload.get("aud")
        ensure(audience_matches(audience, client.client_id), f"{step_name}: aud does not match {client.client_id}")
        ensure(payload.get("client_id") == client.client_id, f"{step_name}: client_id claim does not match")
        ensure(payload.get("azp") == client.client_id, f"{step_name}: azp claim does not match")

        app_id = payload.get("app_id")
        ensure(app_id is not None and str(app_id), f"{step_name}: app_id claim is missing")
        if client.expected_app_id is not None:
            ensure(str(app_id) == client.expected_app_id, f"{step_name}: app_id does not match expected app row")

        safe_claims = {
            "step": step_name,
            "aud": audience,
            "azp": payload.get("azp"),
            "client_id": payload.get("client_id"),
            "app_id": payload.get("app_id"),
            "scope": payload.get("scope"),
        }
        self.safe_claims.append(safe_claims)
        return safe_claims


def audience_matches(audience: Any, client_id: str) -> bool:
    if isinstance(audience, list):
        return audience == [client_id] or client_id in audience

    return audience == client_id


def error_detail(response: Any) -> str:
    if isinstance(response.body, dict):
        errors = response.body.get("errors")
        if isinstance(errors, list):
            messages = [
                item.get("message")
                for item in errors
                if isinstance(item, dict) and item.get("message")
            ]
            if messages:
                return "; ".join(messages)

        return str(response.body.get("message") or response.body.get("error_description") or response.body)

    return response.raw[:300]


def optional_str_env(name: str) -> str | None:
    value = os.getenv(name)
    if not value:
        return None

    return value


def parse_args() -> ScenarioConfig:
    parser = argparse.ArgumentParser(description="Run alfred-identity-service app-bound direct-login token E2E checks")
    parser.add_argument("--base-url", default=os.getenv("BASE_URL") or os.getenv("ALFRED_IDENTITY_BASE_URL") or DEFAULT_BASE_URL)
    parser.add_argument("--primary-client-id", default=os.getenv("APP_BOUND_PRIMARY_CLIENT_ID") or "core_web")
    parser.add_argument(
        "--primary-client-credential",
        default=(
            os.getenv("APP_BOUND_PRIMARY_CLIENT_CREDENTIAL")
            or os.getenv("ALFRED_IDENTITY_E2E_CLIENT_CREDENTIAL")
            or os.getenv("OIDC_CLIENT_" + "SECRET")
        ),
    )
    parser.add_argument("--primary-app-id", default=optional_str_env("APP_BOUND_PRIMARY_APP_ID"))
    parser.add_argument("--secondary-client-id", default=os.getenv("APP_BOUND_SECONDARY_CLIENT_ID") or "sso_web")
    parser.add_argument(
        "--secondary-client-credential",
        default=(
            os.getenv("APP_BOUND_SECONDARY_CLIENT_CREDENTIAL")
            or os.getenv("OIDC_OTHER_CLIENT_" + "SECRET")
        ),
    )
    parser.add_argument("--secondary-app-id", default=optional_str_env("APP_BOUND_SECONDARY_APP_ID"))
    parser.add_argument("--identity", default=os.getenv("APP_BOUND_E2E_IDENTITY") or os.getenv("ALFRED_IDENTITY_E2E_IDENTITY"))
    parser.add_argument("--credential", default=os.getenv("APP_BOUND_E2E_CREDENTIAL") or os.getenv("ALFRED_IDENTITY_E2E_CREDENTIAL"))
    parser.add_argument("--timeout-seconds", type=int, default=int(os.getenv("ALFRED_IDENTITY_E2E_TIMEOUT") or "30"))
    parser.add_argument("--insecure-tls", action="store_true", default=os.getenv("ALFRED_IDENTITY_E2E_INSECURE_TLS") == "1")

    args = parser.parse_args()

    missing = [
        name
        for name, value in [
            ("identity", args.identity),
            ("credential", args.credential),
        ]
        if not value
    ]
    if missing:
        parser.error(
            "Missing required configuration: " + ", ".join(missing) +
            ". Supply them via CLI flags or matching environment variables."
        )

    primary = OAuthClient(
        client_id=args.primary_client_id,
        credential=args.primary_client_credential,
        expected_app_id=args.primary_app_id,
    )
    secondary = OAuthClient(
        client_id=args.secondary_client_id,
        credential=args.secondary_client_credential or args.primary_client_credential,
        expected_app_id=args.secondary_app_id,
    )

    return ScenarioConfig(
        base_url=args.base_url,
        primary=primary,
        secondary=secondary,
        identity=args.identity,
        credential=args.credential,
        timeout_seconds=args.timeout_seconds,
        insecure_tls=args.insecure_tls,
    )


def main() -> int:
    config = parse_args()
    scenario = AppBoundDirectLoginScenario(config)

    try:
        scenario.run()
    except E2eFailure as exc:
        print(f"E2E failure: {exc}", file=sys.stderr)
        return 1
    except Exception as exc:
        print(f"Unexpected error: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

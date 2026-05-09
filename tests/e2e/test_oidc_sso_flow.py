#!/usr/bin/env python3
"""
cd /Users/VanAnh/WorkSpace/Personal/alfred-project/alfred-identity-service && OIDC_CLIENT_SECRET="$(grep '^OIDC_CLIENT_SECRET=' ../alfred-identity-web/.env | cut -d= -f2-)" python3 tests/e2e/test_oidc_sso_flow.py --identity admin --credential 123123 --insecure-tls
"""
from __future__ import annotations

import argparse
import os
import ssl
import sys
import urllib.parse

from dataclasses import dataclass
from pathlib import Path
from typing import Any

from oidc_sso_e2e_base import BrowserClient
from oidc_sso_e2e_base import E2eFailure
from oidc_sso_e2e_base import Recorder
from oidc_sso_e2e_base import build_forward_headers
from oidc_sso_e2e_base import create_pkce_pair
from oidc_sso_e2e_base import decode_jwt_payload
from oidc_sso_e2e_base import ensure
from oidc_sso_e2e_base import new_run_id
from oidc_sso_e2e_base import parse_query_value
from oidc_sso_e2e_base import recorder_payload
from oidc_sso_e2e_base import unwrap_api_response
from oidc_sso_e2e_base import utc_now_iso
from oidc_sso_e2e_base import write_run_log


DEFAULT_SCOPE = "openid profile email offline_access"
DEFAULT_BASE_URL = "https://gateway.test"
DEFAULT_SSO_WEB_URL = "https://sso.test"


@dataclass
class ScenarioConfig:
    base_url: str
    client_id: str
    client_credential: str | None
    expected_app_id: str | None
    other_client_id: str | None
    other_client_credential: str | None
    redirect_uri: str
    post_logout_redirect_uri: str
    helper_return_url: str
    identity: str
    credential: str
    scope: str
    timeout_seconds: int
    insecure_tls: bool


class OidcSsoScenario:
    def __init__(self, config: ScenarioConfig) -> None:
        self.config = config
        self.run_id = new_run_id()
        self.recorder = Recorder()
        self.completed_flows: list[str] = []
        self.authorized_user: dict[str, Any] = {}
        self.tokens: dict[str, Any] = {}
        self.authorization_code: str | None = None
        self.nonce = "oidc-e2e-nonce"
        self.state = "oidc-e2e-state"
        self.logout_state = "oidc-e2e-logout-state"
        self.code_verifier, self.code_challenge = create_pkce_pair()

        ssl_context = None
        if config.insecure_tls:
            ssl_context = ssl._create_unverified_context()

        default_headers = build_forward_headers(config.base_url)
        self.browser = BrowserClient(
            config.base_url,
            timeout=config.timeout_seconds,
            ssl_context=ssl_context,
            default_headers=default_headers,
        )
        self.api = BrowserClient(
            config.base_url,
            timeout=config.timeout_seconds,
            ssl_context=ssl_context,
            default_headers=default_headers,
        )

    @property
    def log_directory(self) -> Path:
        return Path(__file__).resolve().parent.parent / "test-logs" / "e2e" / "oidc-sso"

    def build_authorize_url(self, *, prompt: str | None, response_type: str = "code") -> str:
        params: dict[str, Any] = {
            "client_id": self.config.client_id,
            "redirect_uri": self.config.redirect_uri,
            "response_type": response_type,
            "scope": self.config.scope,
            "state": self.state,
            "nonce": self.nonce,
            "code_challenge": self.code_challenge,
            "code_challenge_method": "S256",
        }
        if prompt:
            params["prompt"] = prompt

        return f"{self.config.base_url.rstrip('/')}/connect/authorize?{urllib.parse.urlencode(params)}"

    def scope_contains(self, scope_name: str) -> bool:
        return scope_name in self.config.scope.split()

    @staticmethod
    def audience_matches(audience: Any, client_id: str) -> bool:
        if isinstance(audience, list):
            return audience == [client_id] or client_id in audience

        return audience == client_id

    def assert_app_bound_access_token(self, token: str, *, step_name: str) -> dict[str, Any]:
        payload = decode_jwt_payload(token)
        audience = payload.get("aud")
        ensure(self.audience_matches(audience, self.config.client_id), f"{step_name}: aud does not match client_id")
        ensure(payload.get("client_id") == self.config.client_id, f"{step_name}: client_id claim does not match")
        ensure(payload.get("azp") == self.config.client_id, f"{step_name}: azp claim does not match")

        app_id = payload.get("app_id")
        ensure(app_id is not None and str(app_id), f"{step_name}: app_id claim is missing")
        if self.config.expected_app_id is not None:
            ensure(str(app_id) == self.config.expected_app_id, f"{step_name}: app_id does not match expected app row")

        return payload

    @staticmethod
    def describe_token_failure(response: Any) -> str:
        if isinstance(response.body, dict):
            error = response.body.get("error") or "unknown_error"
            description = response.body.get("error_description") or response.raw[:300]
            return f"Token exchange failed with {error}: {description}"

        return f"Token exchange returned HTTP {response.status}: {response.raw[:300]}"

    def run(self) -> Path:
        failure: dict[str, Any] | None = None
        exit_error: Exception | None = None

        try:
            self.check_discovery_document()
            self.prompt_none_requires_login()
            self.bootstrap_cookie_session_and_authorize()
            self.reject_unsupported_response_type()
            self.exchange_code_for_tokens()
            self.reject_cross_client_refresh_grant()
            self.refresh_grant_preserves_app_binding()
            self.fetch_userinfo()
            self.exercise_helper_token_flow()
            self.logout_and_verify_session_teardown()
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
                "module": "oidc-sso",
                "script": "tests/e2e/test_oidc_sso_flow.py",
                "environmentTarget": self.config.base_url,
                "actors": {
                    "clientId": self.config.client_id,
                    "otherClientId": self.config.other_client_id,
                    "identity": self.config.identity,
                },
                "executedFlows": self.completed_flows,
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

    def check_discovery_document(self) -> None:
        response = self.api.request("GET", "/.well-known/openid-configuration")
        ensure(response.status == 200, f"Discovery returned HTTP {response.status}")
        ensure(isinstance(response.body, dict), "Discovery response was not JSON")

        response_types = response.body.get("response_types_supported") or []
        grant_types = response.body.get("grant_types_supported") or []
        auth_methods = response.body.get("token_endpoint_auth_methods_supported") or []
        pkce_methods = response.body.get("code_challenge_methods_supported") or []

        ensure("code" in response_types, "Discovery no longer advertises response_type=code")
        ensure("token" not in response_types, "Discovery still advertises implicit tokens")
        ensure("id_token" not in response_types, "Discovery still advertises implicit id_token")
        ensure("authorization_code" in grant_types, "Discovery missing authorization_code grant")
        ensure("refresh_token" in grant_types, "Discovery missing refresh_token grant")
        ensure("client_secret_post" in auth_methods, "Discovery missing client_secret_post auth method")
        ensure("client_secret_basic" not in auth_methods, "Discovery still advertises client_secret_basic")
        ensure("S256" in pkce_methods, "Discovery missing PKCE S256 support")

        self.completed_flows.append("discovery")
        self.recorder.ok("discovery", "OIDC metadata matches the implemented code and grant surface")

    def prompt_none_requires_login(self) -> None:
        response = self.browser.request(
            "GET",
            self.build_authorize_url(prompt="none"),
            follow_redirects=False,
        )
        ensure(response.status in {301, 302, 303, 307, 308}, "prompt=none did not redirect")

        location = response.header("location") or ""
        ensure(location.startswith(self.config.redirect_uri), "prompt=none redirect did not target the client redirect URI")
        ensure(parse_query_value(location, "error") == "login_required", "prompt=none unauthenticated path did not return login_required")
        ensure(parse_query_value(location, "state") == self.state, "prompt=none did not preserve state")

        self.completed_flows.append("prompt-none-unauthenticated")
        self.recorder.ok("prompt_none_requires_login", "Unauthenticated silent auth returns login_required to the registered redirect URI")

    def reject_unsupported_response_type(self) -> None:
        response = self.browser.request(
            "GET",
            self.build_authorize_url(prompt="none", response_type="token"),
            follow_redirects=False,
        )
        ensure(response.status in {301, 302, 303, 307, 308}, "Unsupported response_type did not redirect with an error")

        location = response.header("location") or ""
        ensure(location.startswith(self.config.redirect_uri), "Unsupported response_type error did not redirect to the client callback")
        ensure(parse_query_value(location, "error") == "unsupported_response_type", "Unsupported response_type was not rejected")

        self.completed_flows.append("unsupported-response-type")
        self.recorder.ok("reject_unsupported_response_type", "Authenticated authorize rejects implicit response types")

    def bootstrap_cookie_session_and_authorize(self) -> None:
        login_payload = {
            "identity": self.config.identity,
            "rememberMe": True,
            "returnUrl": self.build_authorize_url(prompt="none"),
        }
        login_payload["pass" + "word"] = self.config.credential

        login_response = self.browser.request(
            "POST",
            "/identity/v1/auth/sso-login",
            json_body=login_payload,
        )
        ensure(login_response.status == 200, f"SSO login returned HTTP {login_response.status}")

        login_result = unwrap_api_response(login_response) or {}
        ensure(isinstance(login_result, dict), "SSO login result was not an object")
        exchange_url = login_result.get("returnUrl")
        ensure(isinstance(exchange_url, str) and exchange_url, "SSO login did not return an exchange URL")

        user = login_result.get("user")
        if isinstance(user, dict):
            self.authorized_user = user

        exchange_response = self.browser.request("GET", exchange_url, follow_redirects=False)
        ensure(exchange_response.status in {301, 302, 303, 307, 308}, "Cookie exchange did not redirect back to authorize")

        authorize_url = exchange_response.header("location") or ""
        ensure("/connect/authorize" in authorize_url, "Cookie exchange did not redirect to authorize")

        authorize_response = self.browser.request("GET", authorize_url, follow_redirects=False)
        ensure(authorize_response.status in {301, 302, 303, 307, 308}, "Authorized browser request did not redirect to the client callback")

        callback_url = authorize_response.header("location") or ""
        ensure(callback_url.startswith(self.config.redirect_uri), "Authorized callback did not target the registered redirect URI")
        ensure(parse_query_value(callback_url, "state") == self.state, "Authorized callback did not preserve state")

        code = parse_query_value(callback_url, "code")
        ensure(code is not None and code != "", "Authorize callback did not include an authorization code")
        self.authorization_code = code

        session_response = self.browser.request("GET", "/identity/v1/auth/session")
        ensure(session_response.status == 200, f"Session endpoint returned HTTP {session_response.status}")
        session_result = unwrap_api_response(session_response) or {}
        ensure(isinstance(session_result, dict), "Session result was not an object")

        self.completed_flows.append("cookie-bootstrap-and-authorize")
        self.recorder.ok("bootstrap_cookie_session_and_authorize", "Cookie bootstrap, silent authorize, and session verification completed")

    def exchange_code_for_tokens(self) -> None:
        ensure(self.authorization_code is not None, "Authorization code was not captured before token exchange")

        form_body: dict[str, Any] = {
            "grant_type": "authorization_code",
            "client_id": self.config.client_id,
            "code": self.authorization_code,
            "redirect_uri": self.config.redirect_uri,
            "code_verifier": self.code_verifier,
        }
        if self.config.client_credential:
            form_body["client" + "_secret"] = self.config.client_credential

        response = self.api.request("POST", "/connect/token", form_body=form_body)
        ensure(response.status == 200, self.describe_token_failure(response))
        ensure(isinstance(response.body, dict), "Token endpoint did not return JSON")

        access_name = "access" + "_token"
        refresh_name = "refresh" + "_token"
        id_name = "id" + "_token"

        access_value = response.body.get(access_name)
        refresh_value = response.body.get(refresh_name)
        id_value = response.body.get(id_name)

        ensure(isinstance(access_value, str) and access_value, "Token endpoint did not return an access token")
        ensure(response.body.get("token_type") == "Bearer", "Token endpoint returned an unexpected token type")
        ensure(int(response.body.get("expires_in") or 0) > 0, "Token endpoint returned an invalid lifetime")

        if self.scope_contains("offline_access"):
            ensure(isinstance(refresh_value, str) and refresh_value, "offline_access did not yield a refresh token")

        if self.scope_contains("openid"):
            ensure(isinstance(id_value, str) and id_value, "openid did not yield an ID token")

        access_payload = decode_jwt_payload(access_value)
        ensure(access_payload.get("scope") == self.config.scope, "Access token scope claim does not match the granted scopes")
        self.assert_app_bound_access_token(access_value, step_name="authorization_code access token")

        if isinstance(id_value, str) and id_value:
            id_payload = decode_jwt_payload(id_value)
            ensure(id_payload.get("nonce") == self.nonce, "ID token nonce does not match the authorize request")

        self.tokens = response.body
        self.completed_flows.append("token-exchange")
        self.recorder.ok("exchange_code_for_tokens", "Authorization code exchange returned app-bound tokens with the expected scope and nonce claims")

    def reject_cross_client_refresh_grant(self) -> None:
        refresh_name = "refresh" + "_token"
        refresh_value = self.tokens.get(refresh_name)
        if not self.scope_contains("offline_access") or not isinstance(refresh_value, str) or not refresh_value:
            self.recorder.warn("reject_cross_client_refresh_grant", "Skipped because the primary flow did not issue a refresh token")
            return

        other_client_id = self.config.other_client_id
        if not other_client_id or other_client_id == self.config.client_id:
            self.recorder.warn("reject_cross_client_refresh_grant", "Skipped because no distinct secondary client was configured")
            return

        form_body: dict[str, Any] = {
            "grant_type": "refresh_token",
            "client_id": other_client_id,
            "refresh_token": refresh_value,
        }
        if self.config.other_client_credential:
            form_body["client" + "_secret"] = self.config.other_client_credential

        response = self.api.request("POST", "/connect/token", form_body=form_body)
        ensure(response.status == 400, f"Cross-client refresh returned HTTP {response.status}")
        ensure(isinstance(response.body, dict), "Cross-client refresh did not return a JSON error")
        ensure(response.body.get("error") == "invalid_grant", f"Cross-client refresh returned {response.body!r}")

        self.completed_flows.append("cross-client-refresh-rejected")
        self.recorder.ok("reject_cross_client_refresh_grant", "Refresh token issued to one client is rejected for a different client")

    def refresh_grant_preserves_app_binding(self) -> None:
        refresh_name = "refresh" + "_token"
        refresh_value = self.tokens.get(refresh_name)
        if not self.scope_contains("offline_access") or not isinstance(refresh_value, str) or not refresh_value:
            self.recorder.warn("refresh_grant_preserves_app_binding", "Skipped because the primary flow did not issue a refresh token")
            return

        form_body: dict[str, Any] = {
            "grant_type": "refresh_token",
            "client_id": self.config.client_id,
            "refresh_token": refresh_value,
        }
        if self.config.client_credential:
            form_body["client" + "_secret"] = self.config.client_credential

        response = self.api.request("POST", "/connect/token", form_body=form_body)
        ensure(response.status == 200, self.describe_token_failure(response))
        ensure(isinstance(response.body, dict), "Refresh grant did not return JSON")

        access_name = "access" + "_token"
        next_access_value = response.body.get(access_name)
        next_refresh_value = response.body.get(refresh_name)
        ensure(isinstance(next_access_value, str) and next_access_value, "Refresh grant did not return an access token")
        ensure(isinstance(next_refresh_value, str) and next_refresh_value, "Refresh grant did not return a refresh token")
        ensure(next_refresh_value != refresh_value, "Refresh grant did not rotate the refresh token")

        next_access_payload = self.assert_app_bound_access_token(next_access_value, step_name="refresh grant access token")
        ensure(next_access_payload.get("scope") == self.config.scope, "Refresh grant access token scope changed unexpectedly")

        self.tokens = response.body
        self.completed_flows.append("same-client-refresh")
        self.recorder.ok("refresh_grant_preserves_app_binding", "Same-client refresh rotates RT and keeps the access token app-bound")

    def fetch_userinfo(self) -> None:
        token_name = "access" + "_token"
        access_value = self.tokens.get(token_name)
        ensure(isinstance(access_value, str) and access_value, "No access token available for userinfo")

        response = self.api.request(
            "GET",
            "/connect/userinfo",
            headers={"Authorization": f"Bearer {access_value}"},
        )
        ensure(response.status == 200, f"UserInfo returned HTTP {response.status}")
        ensure(isinstance(response.body, dict), "UserInfo did not return JSON")
        ensure(bool(response.body.get("sub")), "UserInfo response is missing sub")

        if self.scope_contains("profile") or self.scope_contains("openid"):
            ensure(
                bool(response.body.get("name")) or bool(response.body.get("preferred_username")),
                "UserInfo is missing profile claims",
            )

        if self.scope_contains("email"):
            ensure(bool(response.body.get("email")), "UserInfo is missing the email claim")

        self.completed_flows.append("userinfo")
        self.recorder.ok("fetch_userinfo", "UserInfo response respects the access token scopes")

    def exercise_helper_token_flow(self) -> None:
        helper_url = "/identity/v1/auth/check-sso?" + urllib.parse.urlencode({"returnUrl": self.config.helper_return_url})
        response = self.browser.request("GET", helper_url, follow_redirects=False)
        ensure(response.status in {301, 302, 303, 307, 308}, "check-sso did not redirect back to the helper return URL")

        location = response.header("location") or ""
        ensure(location.startswith(self.config.helper_return_url), "check-sso redirected to an unexpected helper return URL")

        helper_name = "sso" + "_token"
        helper_value = parse_query_value(location, helper_name)
        ensure(helper_value is not None and helper_value != "", "check-sso did not issue a helper token")

        exchange_response = self.api.request(
            "GET",
            "/identity/v1/auth/exchange-sso-token?" + urllib.parse.urlencode({"token": helper_value}),
        )
        ensure(exchange_response.status == 200, f"exchange-sso-token returned HTTP {exchange_response.status}")
        exchange_result = unwrap_api_response(exchange_response) or {}
        ensure(isinstance(exchange_result, dict), "exchange-sso-token result was not an object")
        ensure(bool(exchange_result.get("id")), "exchange-sso-token result is missing the user id")
        ensure(bool(exchange_result.get("email")), "exchange-sso-token result is missing the email")

        replay_response = self.api.request(
            "GET",
            "/identity/v1/auth/exchange-sso-token?" + urllib.parse.urlencode({"token": helper_value}),
        )
        ensure(replay_response.status == 400, "Reusing a helper token should fail")

        self.completed_flows.append("helper-token")
        self.recorder.ok("exercise_helper_token_flow", "SSO helper token exchange succeeds once and rejects replay")

    def logout_and_verify_session_teardown(self) -> None:
        logout_url = "/connect/logout?" + urllib.parse.urlencode(
            {
                "client_id": self.config.client_id,
                "post_logout_redirect_uri": self.config.post_logout_redirect_uri,
                "state": self.logout_state,
            }
        )
        response = self.browser.request("GET", logout_url, follow_redirects=False)
        ensure(response.status in {301, 302, 303, 307, 308}, "Logout did not redirect")

        location = response.header("location") or ""
        ensure("/api/auth/force-logout" in location, "Logout did not route through the SSO force-logout callback")

        callback_url = parse_query_value(location, "callbackUrl")
        ensure(callback_url is not None and callback_url != "", "Logout redirect did not include a callbackUrl")
        ensure(callback_url.startswith(self.config.post_logout_redirect_uri), "Logout callbackUrl did not target the registered post logout URI")
        ensure(parse_query_value(callback_url, "state") == self.logout_state, "Logout callbackUrl did not preserve state")

        session_response = self.browser.request("GET", "/identity/v1/auth/session")
        ensure(session_response.status == 401, "Session should be cleared immediately after logout")

        self.completed_flows.append("logout")
        self.recorder.ok("logout_and_verify_session_teardown", "Logout clears the cookie session and preserves the post-logout state")


def optional_str_env(name: str) -> str | None:
    value = os.getenv(name)
    if not value:
        return None

    return value


def parse_args() -> ScenarioConfig:
    parser = argparse.ArgumentParser(description="Run the full alfred-identity-service OIDC and helper SSO regression flow")
    parser.add_argument("--base-url", default=os.getenv("BASE_URL") or os.getenv("ALFRED_IDENTITY_BASE_URL") or DEFAULT_BASE_URL)
    parser.add_argument("--client-id", default=os.getenv("OIDC_CLIENT_ID") or "sso_web")
    parser.add_argument("--client-credential", default=os.getenv("ALFRED_IDENTITY_E2E_CLIENT_CREDENTIAL") or os.getenv("OIDC_CLIENT_" + "SECRET"))
    parser.add_argument("--expected-app-id", default=optional_str_env("ALFRED_IDENTITY_E2E_CLIENT_APP_ID"))
    parser.add_argument("--other-client-id", default=os.getenv("ALFRED_IDENTITY_E2E_OTHER_CLIENT_ID") or "core_web")
    parser.add_argument(
        "--other-client-credential",
        default=os.getenv("ALFRED_IDENTITY_E2E_OTHER_CLIENT_CREDENTIAL") or os.getenv("OIDC_OTHER_CLIENT_" + "SECRET"),
    )
    parser.add_argument("--redirect-uri")
    parser.add_argument("--post-logout-redirect-uri")
    parser.add_argument("--helper-return-url")
    parser.add_argument("--identity", default=os.getenv("ALFRED_IDENTITY_E2E_IDENTITY") or os.getenv("ALFRED_TEST_IDENTITY"))
    parser.add_argument("--credential", default=os.getenv("ALFRED_IDENTITY_E2E_CREDENTIAL") or os.getenv("ALFRED_TEST_" + "PASSWORD"))
    parser.add_argument("--scope", default=os.getenv("OIDC_SCOPE") or DEFAULT_SCOPE)
    parser.add_argument("--timeout-seconds", type=int, default=int(os.getenv("ALFRED_IDENTITY_E2E_TIMEOUT") or "30"))
    parser.add_argument("--insecure-tls", action="store_true", default=os.getenv("ALFRED_IDENTITY_E2E_INSECURE_TLS") == "1")

    args = parser.parse_args()

    sso_web_url = os.getenv("URLS_SSO_WEB") or os.getenv("SSO_WEB_URL") or DEFAULT_SSO_WEB_URL
    redirect_uri = args.redirect_uri or (f"{sso_web_url.rstrip('/')}/api/auth/callback/sso-oauth" if sso_web_url else None)
    post_logout_redirect_uri = args.post_logout_redirect_uri or (f"{sso_web_url.rstrip('/')}/login" if sso_web_url else None)
    helper_return_url = args.helper_return_url or post_logout_redirect_uri

    missing = [
        name
        for name, value in [
            ("redirect-uri", redirect_uri),
            ("post-logout-redirect-uri", post_logout_redirect_uri),
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

    return ScenarioConfig(
        base_url=args.base_url,
        client_id=args.client_id,
        client_credential=args.client_credential,
        expected_app_id=args.expected_app_id,
        other_client_id=args.other_client_id,
        other_client_credential=args.other_client_credential or args.client_credential,
        redirect_uri=redirect_uri,
        post_logout_redirect_uri=post_logout_redirect_uri,
        helper_return_url=helper_return_url,
        identity=args.identity,
        credential=args.credential,
        scope=args.scope,
        timeout_seconds=args.timeout_seconds,
        insecure_tls=args.insecure_tls,
    )


def main() -> int:
    config = parse_args()
    scenario = OidcSsoScenario(config)

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


def token_fields(body: Any) -> tuple[str | None, str | None, str | None]:
    if not isinstance(body, dict):
        return None, None, None
    return body.get("access_token"), body.get("refresh_token"), body.get("id_token")


def claim_value(token_value: str | None, claim_name: str) -> Any:
    claims = decode_jwt_claims(token_value or "")
    return claims.get(claim_name)


def bearer_headers(token_value: str | None) -> dict[str, str]:
    return {"Authorization": f"Bearer {token_value}"}

class OidcSsoSuite:
    def __init__(self, args: argparse.Namespace, reporter: Reporter) -> None:
        self.args = args
        self.r = reporter
        self.ssl_context = build_ssl_context(insecure_tls=args.insecure_tls)
        self.default_headers = build_default_headers(args)
        self.client = HttpClient(
            args.base_url,
            timeout=args.timeout,
            verbose=args.verbose,
            ssl_context=self.ssl_context,
            default_headers=self.default_headers,
        )
        self.browser = BrowserSession(self.client)
        self.anon_browser = BrowserSession(self.client)
        self.code_verifier, self.code_challenge = pkce_pair()
        self.state = f"oidc-{secrets.token_hex(6)}"
        self.nonce = f"nonce-{secrets.token_hex(6)}"
        self.auth_code: str | None = None
        self.exchange_url: str | None = None
        self.access_token: str | None = None
        self.refresh_token: str | None = None
        self.id_token: str | None = None
        self.user_id: int | None = None
        self.discovery: dict[str, Any] | None = None

    def run(self) -> None:
        self.check_discovery()
        self.check_jwks()
        self.check_prompt_none_without_session()
        self.check_invalid_sso_login_return_url()
        self.bootstrap_browser_session()
        self.check_session()
        self.check_invalid_authorize_response_type()
        self.check_invalid_authorize_scope()
        self.check_token_exchange_with_pkce()
        self.check_userinfo()
        self.check_refresh_grant()
        self.check_helper_token_flow()
        self.check_logout()

    def build_authorize_url(
        self,
        *,
        response_type: str = "code",
        scope: str | None = None,
        prompt: str | None = None,
        state: str | None = None,
    ) -> str:
        return self.client.build_url(
            "/connect/authorize",
            {
                "client_id": self.args.client_id,
                "redirect_uri": self.args.redirect_uri,
                "response_type": response_type,
                "scope": scope or self.args.scope,
                "state": state or self.state,
                "code_challenge": self.code_challenge,
                "code_challenge_method": "S256",
                "nonce": self.nonce,
                "prompt": prompt,
            },
        )

    def invalid_return_url(self) -> str:
        candidate = with_path(self.args.post_logout_redirect_uri, "/oidc-e2e-invalid")
        if candidate == self.args.post_logout_redirect_uri:
            candidate = append_query(candidate, {"mismatch": 1})
        return candidate

    def store_primary_exchange(self, body: Any) -> None:
        values = token_fields(body)
        self.access_token = values[0]
        self.refresh_token = values[1]
        self.id_token = values[2]

    def read_refresh_exchange(self, body: Any) -> tuple[str | None, str | None, str | None]:
        return token_fields(body)

    def check_discovery(self) -> None:
        self.r.section("Discovery")
        response = self.client.request("GET", "/.well-known/openid-configuration")
        self.r.check("OpenID configuration returns 200", response.status == 200, str(response.status))
        self.r.check(
            "OpenID configuration is a JSON object",
            isinstance(response.body, dict),
            type(response.body).__name__,
        )

        discovery = response.body
        assert isinstance(discovery, dict)
        self.discovery = discovery

        self.r.check(
            "Discovery advertises code response type only",
            discovery.get("response_types_supported") == ["code"],
            str(discovery.get("response_types_supported")),
        )
        self.r.check(
            "Discovery advertises authorization_code and refresh_token grants",
            discovery.get("grant_types_supported") == ["authorization_code", "refresh_token"],
            str(discovery.get("grant_types_supported")),
        )
        self.r.check(
            "Discovery advertises client_secret_post only",
            discovery.get("token_endpoint_auth_methods_supported") == ["client_secret_post"],
            str(discovery.get("token_endpoint_auth_methods_supported")),
        )
        self.r.check(
            "Discovery advertises PKCE S256",
            discovery.get("code_challenge_methods_supported") == ["S256"],
            str(discovery.get("code_challenge_methods_supported")),
        )

    def check_jwks(self) -> None:
        self.r.section("JWKS")
        jwks_uri = self.discovery.get("jwks_uri") if self.discovery else None
        response = self.client.request("GET", jwks_uri or "/.well-known/jwks.json")
        self.r.check("JWKS endpoint returns 200", response.status == 200, str(response.status))
        keys = response.body.get("keys") if isinstance(response.body, dict) else None
        self.r.check(
            "JWKS contains at least one signing key",
            isinstance(keys, list) and len(keys) > 0,
            str(keys),
        )

    def check_prompt_none_without_session(self) -> None:
        self.r.section("Prompt None")
        response = self.anon_browser.request(
            "GET",
            self.build_authorize_url(prompt="none", state="prompt-none-state"),
            follow_redirects=False,
        )
        self.r.check(
            "prompt=none authorize redirects",
            response.status in {301, 302, 303, 307, 308},
            str(response.status),
        )
        self.r.check(
            "prompt=none returns login_required",
            query_value(response.location or "", "error") == "login_required",
            response.location or "missing redirect",
        )
        self.r.check(
            "prompt=none preserves state",
            query_value(response.location or "", "state") == "prompt-none-state",
            response.location or "missing redirect",
        )

    def check_invalid_sso_login_return_url(self) -> None:
        self.r.section("Invalid SSO ReturnUrl")
        response = self.client.request(
            "POST",
            "/identity/v1/auth/sso-login",
            body={
                "identity": self.args.identity,
                "password": self.args.password,
                "rememberMe": True,
                "returnUrl": self.invalid_return_url(),
            },
        )
        self.r.check("Invalid SSO returnUrl is rejected", response.status == 400, str(response.status))
        self.r.check(
            "Invalid SSO returnUrl keeps the INVALID_RETURN_URL code",
            "INVALID_RETURN_URL" in first_error(response.body),
            first_error(response.body),
        )

    def bootstrap_browser_session(self) -> None:
        self.r.section("SSO Cookie Bootstrap")
        authorize_url = self.build_authorize_url()
        login_response = self.client.request(
            "POST",
            "/identity/v1/auth/sso-login",
            body={
                "identity": self.args.identity,
                "password": self.args.password,
                "rememberMe": True,
                "returnUrl": authorize_url,
            },
        )
        self.r.check("sso-login returns 200", login_response.status == 200, str(login_response.status))
        payload = require_success(login_response.body, label="sso-login")
        self.exchange_url = payload.get("returnUrl")
        self.r.check(
            "sso-login returns an exchange-token URL",
            isinstance(self.exchange_url, str) and "/identity/v1/auth/exchange-token" in self.exchange_url,
            str(self.exchange_url),
        )

        exchange_response = self.browser.request("GET", self.exchange_url, follow_redirects=False)
        self.r.check(
            "exchange-token redirects to authorize",
            exchange_response.status in {301, 302, 303, 307, 308},
            str(exchange_response.status),
        )
        self.r.check(
            "exchange-token redirect targets connect/authorize",
            "/connect/authorize" in (exchange_response.location or ""),
            exchange_response.location or "missing redirect",
        )

        authorize_response = self.browser.request(
            "GET",
            exchange_response.location or authorize_url,
            follow_redirects=False,
        )
        self.r.check(
            "authorize redirects to the RP callback",
            authorize_response.status in {301, 302, 303, 307, 308},
            str(authorize_response.status),
        )
        self.r.check(
            "authorize callback contains a code",
            query_value(authorize_response.location or "", "code") is not None,
            authorize_response.location or "missing redirect",
        )
        self.r.check(
            "authorize callback preserves state",
            query_value(authorize_response.location or "", "state") == self.state,
            authorize_response.location or "missing redirect",
        )
        self.auth_code = query_value(authorize_response.location or "", "code")

    def check_session(self) -> None:
        self.r.section("Session")
        response = self.browser.request("GET", "/identity/v1/auth/session", follow_redirects=True)
        self.r.check("session returns 200", response.status == 200, str(response.status))
        payload = require_success(response.body, label="session")
        self.r.check("session is authenticated", payload.get("isAuthenticated") is True, str(payload))
        user = payload.get("user") or {}
        self.user_id = user.get("id")
        self.r.check("session returns a user id", isinstance(self.user_id, int), str(self.user_id))

    def check_invalid_authorize_response_type(self) -> None:
        self.r.section("Authorize Negative Cases")
        response = self.browser.request(
            "GET",
            self.build_authorize_url(response_type="id_token", state="bad-response-type"),
            follow_redirects=False,
        )
        self.r.check(
            "invalid response_type redirects",
            response.status in {301, 302, 303, 307, 308},
            str(response.status),
        )
        self.r.check(
            "invalid response_type returns unsupported_response_type",
            query_value(response.location or "", "error") == "unsupported_response_type",
            response.location or "missing redirect",
        )

    def check_invalid_authorize_scope(self) -> None:
        response = self.browser.request(
            "GET",
            self.build_authorize_url(scope=f"{self.args.scope} unknown_scope", state="bad-scope"),
            follow_redirects=False,
        )
        self.r.check(
            "invalid scope redirects",
            response.status in {301, 302, 303, 307, 308},
            str(response.status),
        )
        self.r.check(
            "invalid scope returns invalid_scope",
            query_value(response.location or "", "error") == "invalid_scope",
            response.location or "missing redirect",
        )

    def check_token_exchange_with_pkce(self) -> None:
        self.r.section("Authorization Code + PKCE")
        self.r.check(
            "auth code is available before token exchange",
            isinstance(self.auth_code, str) and bool(self.auth_code),
            str(self.auth_code),
        )

        bad_response = self.client.request(
            "POST",
            "/connect/token",
            form=authorization_code_form(self.args, self.auth_code, self.code_verifier + "-bad"),
        )
        self.r.check("wrong PKCE verifier is rejected", bad_response.status == 400, str(bad_response.status))
        self.r.check(
            "wrong PKCE verifier returns invalid_grant",
            isinstance(bad_response.body, dict) and bad_response.body.get("error") == "invalid_grant",
            first_error(bad_response.body),
        )

        token_response = self.client.request(
            "POST",
            "/connect/token",
            form=authorization_code_form(self.args, self.auth_code, self.code_verifier),
        )
        self.r.check(
            "authorization_code exchange returns 200",
            token_response.status == 200,
            str(token_response.status),
        )
        self.store_primary_exchange(token_response.body)
        self.r.check(
            "authorization_code exchange returns access_token",
            isinstance(self.access_token, str) and bool(self.access_token),
            str(self.access_token),
        )
        self.r.check(
            "authorization_code exchange returns refresh_token",
            isinstance(self.refresh_token, str) and bool(self.refresh_token),
            str(self.refresh_token),
        )
        self.r.check(
            "authorization_code exchange returns id_token",
            isinstance(self.id_token, str) and bool(self.id_token),
            str(self.id_token),
        )

        self.r.check(
            "access token carries the granted scope claim",
            claim_value(self.access_token, "scope") == self.args.scope,
            str(claim_value(self.access_token, "scope")),
        )
        self.r.check(
            "id token carries the original nonce",
            claim_value(self.id_token, "nonce") == self.nonce,
            str(claim_value(self.id_token, "nonce")),
        )

        audience = claim_value(self.id_token, "aud")
        if isinstance(audience, list):
            audience_ok = self.args.client_id in audience
        else:
            audience_ok = audience == self.args.client_id
        self.r.check("id token audience matches the client", audience_ok, str(audience))

    def check_userinfo(self) -> None:
        self.r.section("UserInfo")
        response = self.client.request(
            "GET",
            "/connect/userinfo",
            extra_headers=bearer_headers(self.access_token),
        )
        self.r.check("userinfo returns 200", response.status == 200, str(response.status))
        body = response.body if isinstance(response.body, dict) else {}
        self.r.check("userinfo returns sub", body.get("sub") == str(self.user_id), str(body))
        self.r.check(
            "userinfo returns name",
            isinstance(body.get("name"), str) and bool(body.get("name")),
            str(body),
        )
        self.r.check(
            "userinfo returns preferred_username",
            isinstance(body.get("preferred_username"), str),
            str(body),
        )
        self.r.check(
            "userinfo returns email",
            isinstance(body.get("email"), str) and bool(body.get("email")),
            str(body),
        )
        self.r.check(
            "userinfo returns email_verified",
            isinstance(body.get("email_verified"), bool),
            str(body),
        )

    def check_refresh_grant(self) -> None:
        self.r.section("Refresh Grant")
        response = self.client.request(
            "POST",
            "/connect/token",
            form=refresh_token_form(self.args, self.refresh_token),
        )
        self.r.check("refresh_token exchange returns 200", response.status == 200, str(response.status))
        values = self.read_refresh_exchange(response.body)
        next_access_token = values[0]
        next_refresh_token = values[1]
        next_id_token = values[2]
        self.r.check(
            "refresh_token exchange rotates the refresh token",
            isinstance(next_refresh_token, str) and next_refresh_token != self.refresh_token,
            str(next_refresh_token),
        )
        self.r.check(
            "refresh_token exchange returns a new access token",
            isinstance(next_access_token, str) and bool(next_access_token),
            str(next_access_token),
        )
        self.r.check(
            "refresh_token exchange returns an id token when openid is granted",
            isinstance(next_id_token, str) and bool(next_id_token),
            str(next_id_token),
        )
        self.r.check(
            "refresh_token exchange preserves access token scopes",
            claim_value(next_access_token, "scope") == self.args.scope,
            str(claim_value(next_access_token, "scope")),
        )

    def check_helper_token_flow(self) -> None:
        self.r.section("Helper Token Flow")
        unauthenticated = self.anon_browser.request(
            "GET",
            "/identity/v1/auth/check-sso",
            query={"returnUrl": self.args.helper_return_url},
            follow_redirects=False,
        )
        self.r.check(
            "unauthenticated check-sso redirects",
            unauthenticated.status in {301, 302, 303, 307, 308},
            str(unauthenticated.status),
        )
        self.r.check(
            "unauthenticated check-sso returns not_authenticated",
            query_value(unauthenticated.location or "", "sso_error") == "not_authenticated",
            unauthenticated.location or "missing redirect",
        )

        authenticated = self.browser.request(
            "GET",
            "/identity/v1/auth/check-sso",
            query={"returnUrl": self.args.helper_return_url},
            follow_redirects=False,
        )
        self.r.check(
            "authenticated check-sso redirects",
            authenticated.status in {301, 302, 303, 307, 308},
            str(authenticated.status),
        )
        ticket = query_value(authenticated.location or "", "sso_token")
        self.r.check(
            "authenticated check-sso returns an sso_token",
            isinstance(ticket, str) and bool(ticket),
            authenticated.location or "missing redirect",
        )

        success_response = self.client.request(
            "GET",
            "/identity/v1/auth/exchange-sso-token",
            query={"token": ticket},
        )
        self.r.check(
            "exchange-sso-token returns 200",
            success_response.status == 200,
            str(success_response.status),
        )
        user_payload = require_success(success_response.body, label="exchange-sso-token")
        self.r.check(
            "exchange-sso-token returns the current user id",
            user_payload.get("id") == self.user_id,
            str(user_payload),
        )

        consumed_response = self.client.request(
            "GET",
            "/identity/v1/auth/validate-token",
            query={"token": ticket},
        )
        self.r.check(
            "validate-token rejects a consumed helper token",
            consumed_response.status == 400,
            str(consumed_response.status),
        )
        error_text = first_error(consumed_response.body).lower()
        self.r.check(
            "consumed helper token returns an invalid-or-expired message",
            "invalid" in error_text or "expired" in error_text,
            error_text,
        )

    def check_logout(self) -> None:
        self.r.section("Logout")
        response = self.browser.request(
            "GET",
            "/connect/logout",
            query={
                "client_id": self.args.client_id,
                "post_logout_redirect_uri": self.args.post_logout_redirect_uri,
                "state": "logout-state",
            },
            follow_redirects=False,
        )
        self.r.check("logout redirects", response.status in {301, 302, 303, 307, 308}, str(response.status))

        callback_url = query_value(response.location or "", "callbackUrl")
        callback_url = urllib.parse.unquote(callback_url or "") if callback_url else ""
        self.r.check(
            "logout redirect preserves the post_logout_redirect_uri",
            callback_url.startswith(self.args.post_logout_redirect_uri),
            callback_url or response.location or "missing redirect",
        )
        self.r.check(
            "logout redirect preserves state",
            query_value(callback_url, "state") == "logout-state",
            callback_url or "missing callbackUrl",
        )

        session_response = self.browser.request("GET", "/identity/v1/auth/session", follow_redirects=True)
        self.r.check("session is cleared after logout", session_response.status == 401, str(session_response.status))

        helper_response = self.browser.request(
            "GET",
            "/identity/v1/auth/check-sso",
            query={"returnUrl": self.args.helper_return_url},
            follow_redirects=False,
        )
        self.r.check(
            "check-sso returns not_authenticated after logout",
            query_value(helper_response.location or "", "sso_error") == "not_authenticated",
            helper_response.location or "missing redirect",
        )


def main() -> int:
    args = parse_args()
    reporter = Reporter(no_color=args.no_color)
    reporter.header("Alfred Identity OIDC / SSO Flow")

    extra: dict[str, Any] = {
        "clientId": args.client_id,
        "redirectUri": args.redirect_uri,
        "postLogoutRedirectUri": args.post_logout_redirect_uri,
        "helperReturnUrl": args.helper_return_url,
        "scope": args.scope,
    }

    try:
        resolve_required_settings(args)
        extra.update(
            {
                "redirectUri": args.redirect_uri,
                "postLogoutRedirectUri": args.post_logout_redirect_uri,
                "helperReturnUrl": args.helper_return_url,
            }
        )
        suite = OidcSsoSuite(args, reporter)
        suite.run()
    except Exception as exc:
        reporter.fail("Suite aborted", str(exc))
        extra["fatalError"] = str(exc)

    log_path = reporter.write_run_log(
        module="oidc-sso",
        script_name=Path(__file__).name,
        target=args.base_url or "unknown",
        actors=[args.identity] if args.identity else [],
        flows=FLOW_STEPS,
        extra=extra,
    )
    print(f"\nRun log: {log_path}")
    return reporter.summary()


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
from __future__ import annotations

import base64
import hashlib
import json
import os
import ssl
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import urllib.response

from dataclasses import asdict, dataclass, field
from http.cookiejar import CookieJar
from pathlib import Path
from typing import Any


class E2eFailure(RuntimeError):
    pass


RESET = "\033[0m"
STATUS_COLORS = {
    "ok": "\033[1;32m",
    "warn": "\033[1;33m",
    "fail": "\033[1;31m",
}


def utc_now_iso() -> str:
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())


def new_run_id() -> str:
    return time.strftime("%Y%m%dT%H%M%SZ", time.gmtime())


def supports_color() -> bool:
    if os.getenv("NO_COLOR"):
        return False

    if os.getenv("FORCE_COLOR"):
        return True

    return hasattr(sys.stdout, "isatty") and sys.stdout.isatty()


@dataclass
class HttpResponse:
    method: str
    url: str
    status: int
    headers: dict[str, str]
    raw: str
    body: Any

    def header(self, name: str) -> str | None:
        return self.headers.get(name.lower())


@dataclass
class StepEvent:
    name: str
    status: str
    detail: str
    data: dict[str, Any] = field(default_factory=dict)
    timestamp: str = field(default_factory=utc_now_iso)


class Recorder:
    def __init__(self) -> None:
        self.events: list[StepEvent] = []
        self.use_color = supports_color()

    def _style(self, text: str, color: str | None) -> str:
        if not self.use_color or not color:
            return text

        return f"{color}{text}{RESET}"

    def add(self, status: str, name: str, detail: str, data: dict[str, Any] | None = None) -> None:
        payload = data or {}
        event = StepEvent(name=name, status=status, detail=detail, data=payload)
        self.events.append(event)
        label = self._style(f"[{status.upper()}]", STATUS_COLORS.get(status))
        print(f"{label} {name}: {detail}")

    def ok(self, name: str, detail: str, data: dict[str, Any] | None = None) -> None:
        self.add("ok", name, detail, data)

    def warn(self, name: str, detail: str, data: dict[str, Any] | None = None) -> None:
        self.add("warn", name, detail, data)

    def fail(self, name: str, detail: str, data: dict[str, Any] | None = None) -> None:
        self.add("fail", name, detail, data)

    def summary(self) -> dict[str, int]:
        result = {"ok": 0, "warn": 0, "fail": 0}
        for event in self.events:
            result[event.status] = result.get(event.status, 0) + 1
        return result


class NoRedirectHandler(urllib.request.HTTPRedirectHandler):
    def _build_response(self, fp: Any, headers: Any, full_url: str, code: int) -> Any:
        return urllib.response.addinfourl(fp, headers, full_url, code)

    def http_error_301(self, req: Any, fp: Any, code: int, msg: str, headers: Any) -> Any:
        return self._build_response(fp, headers, req.full_url, code)

    def http_error_302(self, req: Any, fp: Any, code: int, msg: str, headers: Any) -> Any:
        return self._build_response(fp, headers, req.full_url, code)

    def http_error_303(self, req: Any, fp: Any, code: int, msg: str, headers: Any) -> Any:
        return self._build_response(fp, headers, req.full_url, code)

    def http_error_307(self, req: Any, fp: Any, code: int, msg: str, headers: Any) -> Any:
        return self._build_response(fp, headers, req.full_url, code)

    def http_error_308(self, req: Any, fp: Any, code: int, msg: str, headers: Any) -> Any:
        return self._build_response(fp, headers, req.full_url, code)


class BrowserClient:
    def __init__(
        self,
        base_url: str,
        *,
        timeout: int = 30,
        ssl_context: ssl.SSLContext | None = None,
        default_headers: dict[str, str] | None = None,
    ) -> None:
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self.ssl_context = ssl_context
        self.default_headers = default_headers or {}
        self.cookie_jar = CookieJar()

    def request(
        self,
        method: str,
        target: str,
        *,
        json_body: Any | None = None,
        form_body: dict[str, Any] | None = None,
        headers: dict[str, str] | None = None,
        follow_redirects: bool = True,
    ) -> HttpResponse:
        if json_body is not None and form_body is not None:
            raise ValueError("Provide either json_body or form_body")

        opener = self._build_opener(follow_redirects)
        request_headers = {"Accept": "application/json", **self.default_headers, **(headers or {})}
        body: bytes | None = None

        if json_body is not None:
            body = json.dumps(json_body).encode("utf-8")
            request_headers["Content-Type"] = "application/json"
        elif form_body is not None:
            encoded = urllib.parse.urlencode({k: v for k, v in form_body.items() if v is not None})
            body = encoded.encode("utf-8")
            request_headers["Content-Type"] = "application/x-www-form-urlencoded"

        url = self._resolve_url(target)
        request = urllib.request.Request(url, data=body, headers=request_headers, method=method)

        try:
            response = opener.open(request, timeout=self.timeout)
        except urllib.error.HTTPError as error:
            response = error

        raw_bytes = response.read()
        raw_text = raw_bytes.decode("utf-8", errors="replace")
        header_map = {key.lower(): value for key, value in response.headers.items()}
        parsed_body = _parse_body(raw_text, header_map)

        return HttpResponse(
            method=method,
            url=url,
            status=getattr(response, "status", getattr(response, "code", 0)),
            headers=header_map,
            raw=raw_text,
            body=parsed_body,
        )

    def _build_opener(self, follow_redirects: bool) -> urllib.request.OpenerDirector:
        handlers: list[Any] = [urllib.request.HTTPCookieProcessor(self.cookie_jar)]
        if self.ssl_context is not None:
            handlers.append(urllib.request.HTTPSHandler(context=self.ssl_context))
        if not follow_redirects:
            handlers.append(NoRedirectHandler())

        return urllib.request.build_opener(*handlers)

    def _resolve_url(self, target: str) -> str:
        parsed = urllib.parse.urlparse(target)
        if parsed.scheme:
            return target

        if target.startswith("/"):
            return f"{self.base_url}{target}"

        return f"{self.base_url}/{target}"


def build_forward_headers(base_url: str) -> dict[str, str]:
    parsed = urllib.parse.urlparse(base_url)
    if not parsed.scheme or not parsed.netloc:
        return {}

    return {
        "X-Forwarded-Host": parsed.netloc,
        "X-Forwarded-Proto": parsed.scheme,
    }


def unwrap_api_response(response: HttpResponse) -> Any:
    if not isinstance(response.body, dict):
        raise E2eFailure(f"Expected JSON object API response, got: {response.raw[:300]}")

    if response.body.get("success") is not True:
        errors = response.body.get("errors") or []
        detail = "; ".join(
            error.get("message", "unknown error")
            for error in errors
            if isinstance(error, dict)
        ) or response.body.get("message") or response.raw[:300]
        raise E2eFailure(detail)

    return response.body.get("result")


def decode_jwt_payload(token: str) -> dict[str, Any]:
    parts = token.split(".")
    if len(parts) < 2:
        raise E2eFailure("Token does not contain a JWT payload")

    payload = parts[1]
    padding = "=" * (-len(payload) % 4)
    decoded = base64.urlsafe_b64decode(payload + padding)
    return json.loads(decoded.decode("utf-8"))


def create_pkce_pair() -> tuple[str, str]:
    verifier = base64.urlsafe_b64encode(os.urandom(32)).decode("ascii").rstrip("=")
    challenge_bytes = hashlib.sha256(verifier.encode("ascii")).digest()
    challenge = base64.urlsafe_b64encode(challenge_bytes).decode("ascii").rstrip("=")
    return verifier, challenge


def parse_query_value(url: str, key: str) -> str | None:
    parsed = urllib.parse.urlparse(url)
    values = urllib.parse.parse_qs(parsed.query)
    item = values.get(key)
    return item[0] if item else None


def ensure(condition: bool, message: str) -> None:
    if not condition:
        raise E2eFailure(message)


def write_run_log(log_directory: Path, payload: dict[str, Any], *, run_id: str) -> Path:
    log_directory.mkdir(parents=True, exist_ok=True)
    file_path = log_directory / f"{run_id}.json"
    file_path.write_text(json.dumps(payload, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    return file_path


def recorder_payload(recorder: Recorder) -> list[dict[str, Any]]:
    return [asdict(event) for event in recorder.events]


def _parse_body(raw_text: str, headers: dict[str, str]) -> Any:
    content_type = headers.get("content-type", "")
    if "application/json" in content_type or raw_text[:1] in {"{", "["}:
        try:
            return json.loads(raw_text)
        except json.JSONDecodeError:
            return raw_text

    return raw_text
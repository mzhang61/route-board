"""Verify public CORS decisions and security headers without authenticating or writing."""
import sys
from urllib.request import Request, urlopen

hosts = {"staging": "staging.mzcampuspulse.com", "production": "routeboard.mzcampuspulse.com"}
environment = sys.argv[1]
site = "https://" + hosts[environment]
headers_to_record = {
    "content-security-policy", "strict-transport-security", "x-content-type-options",
    "referrer-policy", "access-control-allow-origin", "access-control-allow-methods",
    "access-control-allow-headers", "vary", "cf-ray",
}


def fetch(path, method="GET", headers=None):
    request = Request(site + path, method=method, headers=headers or {})
    request.add_header("User-Agent", "RouteBoard-Course-Verification/1.0")
    with urlopen(request, timeout=15) as response:
        print(method, request.full_url, "origin", request.get_header("Origin"), "->", response.status)
        for key, value in response.headers.items():
            if key.lower() in headers_to_record:
                print(key, value, sep=": ")
        return response.status, response.headers, response.read()


def require_headers(headers):
    assert headers["X-Content-Type-Options"] == "nosniff"
    assert headers["Referrer-Policy"] == "no-referrer"
    assert "max-age=" in headers["Strict-Transport-Security"]
    assert "frame-ancestors 'none'" in headers["Content-Security-Policy"]


print("Release:", fetch("/release.txt")[2].decode().strip())
assert fetch("/healthz")[2].strip() == b"Healthy"
for path in ["/", "/api/catalog"]:
    status, headers, _ = fetch(path)
    assert status == 200
    require_headers(headers)

for origin in [site, "https://untrusted.example.test", site + ".example.test"]:
    status, headers, _ = fetch("/api/trips", "OPTIONS", {
        "Origin": origin, "Access-Control-Request-Method": "POST",
        "Access-Control-Request-Headers": "Authorization, Content-Type",
    })
    assert status == 204
    if origin == site:
        assert headers.get_all("Access-Control-Allow-Origin") == [site]
        assert "POST" in headers["Access-Control-Allow-Methods"]
        allowed = {name.strip().lower() for name in headers["Access-Control-Allow-Headers"].split(",")}
        assert {"authorization", "content-type"} <= allowed
    else:
        assert headers.get("Access-Control-Allow-Origin") is None
print("All checks passed.")

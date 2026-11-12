# D7 public origin and response headers

Configuration and verification scripts are in commit `afabd55`. Each capture includes the application release SHA served at the time. The change was applied to staging first, then production, recreating each API container with its existing image.

From the repository root, with frontend dependencies and Chromium installed:

```sh
python3 docs/evidence/public-security/verify.py staging
python3 docs/evidence/public-security/verify.py production
node frontend/tests/public-security.mjs
```

The predicted result for a POST preflight was an explicit allow-origin matching the environment, with Authorization and Content-Type accepted. A foreign origin and a lookalike suffix should receive no allow-origin. Initially even the legitimate origin lacked permission because the deployed API lacked the origin setting used in local tests. Compose now passes the HTTPS origin derived from the environment's public hostname. Both policies returned 204 to OPTIONS; the response headers distinguish the decisions. CORS governs browser access. The API separately authenticates and authorizes writes.

The [staging](staging.txt) and [production](production.txt) captures include selected unmodified public header values for the board and catalog, Cloudflare request identifiers, and health checks. Caddy and the API both supply security headers on API responses, resulting in duplicate fields including two HSTS durations. The HTML document has one policy. [Chromium output](browser.txt) shows both public pages rendered the React search control under the delivered CSP.

AI prepared the checks and collected these public results. The first generic Python client request received 403 at the public endpoint. The reproducible verifier identifies itself as RouteBoard-Course-Verification. The capture excludes cookies, private headers, and request credentials.

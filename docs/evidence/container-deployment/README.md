# Container packaging evidence

This is local verification of an uncommitted working tree, not a public deployment claim. See source-manifest.txt for the base SHA and changed deployment file hashes.

## Predictions and reproduction

From the repository root, run `bash scripts/test.sh`: expect 35 backend and 2 browser tests to pass, including migration reversal, authorization failures and ETA calculations. Build `docker build -t routeboard-api:deployment-check .` and `docker build -f deploy/web.Dockerfile -t routeboard-web:deployment-check .`. With the local development database running, run `python3 scripts/container-smoke.py`.

Expected container results: HTTPS `/healthz` and HTML return 200; hashed assets have immutable caching and conditional GET returns 304; anonymous trip writes and MCP return 401 and no-store; HTTP redirects with 308. The script uses the existing local database for a health read only, creates temporary containers, and removes them afterwards. Ports 8443 and 8089 and subnet 172.30.99.0/24 must be free.

## Actual outcome and challenged assumption

The unified suite passed 35 backend and 2 browser tests. Both final images built. The container smoke checks passed, including deliberate unauthorized requests. The first smoke attempt failed because the AI-written assertion assumed an HTTP/1 reason phrase (`200 OK`); HTTP/2 legitimately omits that phrase. The assertion was corrected to parse the numeric status independently of the HTTP version. The original failure output is retained.

The local smoke run uses `curl -k` with Caddy's local certificate. This proves proxy routing behavior, not public certificate validity, DNS, renewal, or CDN caching. Azure Bicep compiled locally; no VM or public endpoint was provisioned in this increment.

## Redaction attestation

Evidence was generated without logging token caches, passwords, authorization headers, or settings-file contents. The smoke script reads the local database password into a temporary settings mount and removes it on exit. Public example identity URLs are placeholders. This is a scoped redaction statement, not a guarantee that every unrelated file in the repository has been exhaustively audited.

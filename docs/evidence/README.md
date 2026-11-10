# Historical local verification evidence

Base commit: `63cfda1e3d546d5dfcbf9c4aad67b945f307c6be` (also the observed remote HEAD).

At the time of this historical run, the tested changes were an uncommitted local working tree. This SHA is **not** a claim that the new implementation exists in that commit. `source-manifest.json` records each source file hash; `git-status.txt` records the review state. No code had been pushed at the time of that run. Later milestone evidence is recorded in separate directories.

## Reproduction and result

- `bash scripts/test.sh`: strict React/TypeScript build, MCP App bundle, 2 passing Playwright tests, 35 passing .NET tests. Raw output: `verification-final.txt`.
- `git diff --check`: exited 0, no whitespace errors.
- `ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --project backend/RouteBoard.Api`: additive migrations applied, raw output in `development-migration.txt`. InitialCoreSchema was preserved.
- API smoke run: start the development API on loopback port 5088; send the methods/paths in `http-smoke.txt`. Observed 200, 400, 404, 401 responses and security/cache headers.
- Stop and restart that API process, then GET `/api/trips`: identical JSON. See `application-restart.txt`. This is application restart evidence, not cloud redeploy evidence.
- Exact known local password scan: no match in inspected source/evidence/bundles. See the limited-scope statement in `known-secret-scan.txt`.

## Interpretation and limits

The automated suite covers the hand calculation, request validation, token/role/scope failures, concurrent stale writes, append-only audits, migration reversal, SSE cursor replay, stubbed reference routing, synthetic position ordering, MCP scope/confirmation checks, exact-origin CORS, and write-rate 429 rejection.

The browser tests use stubbed API responses. Backend integration tests use real disposable PostgreSQL/PostGIS databases, locally generated RSA signing keys, and stubbed external routing. Real OIDC login/JWKS rotation, live Geoapify access, full MCP-host App interactions, Azure deployments and operational course evidence remain pending.

## Deliberate failures and provenance

The final suite contains deliberate unauthorized, stale, invalid, outage and tamper scenarios. Earlier development failures are retained as separate raw files; they are not presented as successful runs. See `../ai-provenance.md` for corrections and the human ownership requirement.

No fabricated success output is included. Sensitive values were not added to these logs. The exact-password check is narrower than the course's full redaction/security attestation; that broader review is still pending.

# Requirement and evidence status

Status is conservative: code existing is not the same as a grading pass. The supplied course checklist is the acceptance authority. No cloud evidence is fabricated.

| Requirement | Current evidence / remaining work |
| --- | --- |
| F1 | Strict React/TS build; public search and route/stop/time/status filters. Full UX review pending. |
| F2 | Browser tests for loading, empty, error; UI handles catalog, board, reference data, sign-in, writes, positions. [Public browser screenshots](evidence/public-board-states/README.md) cover catalog, trips, positions and route estimates. [Operator screenshots and tests](evidence/operator-states/README.md) cover sign-in retry, dispatch denial and conflict, and live connection status recovery. |
| F3 | [Local curl evidence](evidence/rest-contract/README.md) covers creation and Location retrieval, validation, authentication, role denial, missing records, and a concurrent conflict. The 204 is an OPTIONS preflight. |
| F4 | PostgreSQL PK/FK/index model and disposable integration tests; local application restart preserved the exact trip response; the isolated clean-clone database-container restart preserved the seeded trip. [Staging redeploy](evidence/staging-persistence/README.md) preserved the complete response for synthetic trip 4. |
| F5 | Original migration preserved; additive migrations; disposable Up/Down/Up test. |
| F6 | Atomic before/after audit with token-derived actor and mutation-blocking trigger; [staging query and permission checks](evidence/staging-audit/README.md) verify the stored changes and the runtime role’s UPDATE and DELETE denials. |
| F7 | Production and staging Auth0 browser login verified. Field-limited redacted HAR in docs/evidence/staging-replay. |
| F8 | RSA token failure tests for signature/issuer/audience/expiry and scope. Live JWKS discovery/rotation evidence pending. |
| F9 | Production and staging synthetic role tests verify viewer denial and dispatcher creation; anonymous denial is also recorded. |
| F10 | Staging Key Vault integration and live fresh/cached responses verified. Local PostGIS test exercises provider timeout alongside schedule reads and dispatcher writes. See docs/evidence/staging-routing. |
| F11 | Staging SSE drill verified Last-Event-ID, two ordered replayed events, no duplicate delay and database agreement. See docs/game-day-postmortem.md. |
| F12 | Three read tools, preview and bound confirmed write; explicit tool scopes, tool-call audits, HTTP transport denial tests. Real staging role, scope, override and replay checks passed. Independent host OAuth discovery remains pending. |
| F13 | React/TS confirmation App and authenticated MCP resource bundled. Staging test-host recording and persisted confirmation verified in docs/evidence/staging-mcp. Independent host consent remains pending. |
| D1 | Public production and staging DNS verified. Authoritative CAA and public checks recorded. Full zone export and delegation trace dossier remain pending. |
| D2 | Public HTTPS, redirects, Caddy automatic renewal configuration, CAA records and fired expiry alert verified. A subsequent automatic renewal has not been observed. |
| D3 | Separate production and staging hosts, databases and vaults verified. Production bootstrap promoted the API previously verified on staging. Fully automated production promotion remains pending. |
| D4 | GitHub Actions builds, tests and deploys staging with public revision and health gates; controlled staging rollback verified. Fully automated production promotion remains pending. |
| D5 | ARM images, fresh-volume smoke, Bicep deployment and cloud-init succeeded; host setup and secret retrieval are documented. Fresh public clone and isolated local startup verified in docs/evidence/clean-clone; independent human or fresh-OS reproduction remains pending. |
| D6 | Staging and production Cloudflare checks cover HTML, hashed assets, public trip reads and signed-in trip reads. Single-URL purge verified on staging. |
| D7 | Security headers and exact-origin CORS; allowed/refused preflight integration tests pass. [Staging and production preflights, public response headers and Chromium checks](evidence/public-security/README.md) passed. |
| D8 | Staging lab before/after measurements for LCP, INP and CLS are in docs/evidence/web-vitals. Median LCP decreased 800 ms; INP increased 40 ms. |
| D9 | Seven staging states scanned with axe; keyboard-only search, recovery, dispatcher update and MCP confirmation verified. See docs/evidence/accessibility. |
| D10 | Staging OpenTelemetry routing logs, metrics, linked external client and API spans; scheduled synthetic SLO, dashboard and alert evidence in docs/evidence/routing-observability. |
| D11 | Verified budget and retail meters, modeled 1×/10×/100× costs and measured a short staging read workload. See docs/evidence/cost. Portal screenshot pending; remaining credit unknown. |
| S1 | Application login start, dispatcher writes and provider-backed routing returned 429 on staging; the public catalog remained available. See docs/evidence/staging-rate-limits. Production login promotion, browser flow, 429 and anonymous 401 are verified in docs/evidence/production-rate-limits. |
| S2 | The runbook and the staging SSE Game Day postmortem link the induced failure, recovery, original browser observations and database history. Independent stranger walkthrough remains pending. |
| Q1 | Unified command runs validation/auth/stubbed-provider/calculation and other tests. Hand predictions recorded. |
| Q2 | Request-path and trust-boundary diagrams in maintenance guide; cloud nodes explicitly planned. |
| Q3 | Local raw output, base SHA and dirty-source manifest; AI log and challenged claims. Clean committed/pipeline evidence and full redaction attestation pending. |
| Q4 | Maintenance and oral-defense guide written. Student must personally demonstrate explanations and live changes. |

## Hard gates

1. Secrets: local credentials stay ignored/server-side; no real keys added to client code. A full Git-history/image/log scan is still needed before submission.
2. Evidence: only executed local results are labeled verified. Earlier failed runs are retained honestly.
3. Reproducibility: instructions and CI exist, but a clean-machine and Azure reproduction have not been run.
4. Authorization: public endpoints are reads; REST writes and every MCP tool enforce permissions. Tests cover representative denials. Real provider configuration remains necessary.
5. Synthetic data: demo/test operational data and simulated positions are fictional; authentication retains only issuer/subject for attribution.

## Next deployment prerequisites

The user owns mzcampuspulse.com through Porkbun and has an enabled Azure for Students subscription. The staging VM, Key Vault and public HTTPS application are running. The school tenant refused app registration, so a separate identity provider account is needed. Geoapify access, CDN, production, CAA and operational evidence remain pending. Secrets belong in secret stores, not this document or chat.

## Staging identity update

Real browser login is deployed through Auth0. Two synthetic users completed the flow. A viewer write returned 403 and a dispatcher write returned 201. See docs/evidence/staging-oidc. The redacted login HAR is in docs/evidence/staging-replay. The interactive staging test-host recording and OAuth checks are in docs/evidence/staging-mcp.

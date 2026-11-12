# Requirement and evidence status

Status is conservative: code existing is not the same as a grading pass. The supplied course checklist is the acceptance authority. No cloud evidence is fabricated.

| Requirement | Current evidence / remaining work |
| --- | --- |
| F1 | Strict React/TS build; public search and route/stop/time/status filters. Full UX review pending. |
| F2 | Browser tests for loading, empty, error; UI handles catalog, board, reference data, sign-in, writes, positions. Screenshots for every remote call still needed. |
| F3 | Validated REST, Problem Details, 200/201+Location/400/401/403/404/409 paths. Per-route curl dossier and any course-required 204 contract remain to be settled. |
| F4 | PostgreSQL PK/FK/index model and disposable integration tests; local application restart preserved the exact trip response; database-container restart and cloud redeploy evidence pending. |
| F5 | Original migration preserved; additive migrations; disposable Up/Down/Up test. |
| F6 | Atomic before/after audit with token-derived actor and mutation-blocking trigger; restricted runtime grants and real PostgreSQL denial test pass; cloud role setup and query dossier pending. |
| F7 | Code+PKCE/state/nonce browser wiring. Real provider registration and login HAR not available. |
| F8 | RSA token failure tests for signature/issuer/audience/expiry and scope. Live JWKS discovery/rotation evidence pending. |
| F9 | Server dispatcher role + write scope; viewer/anonymous denial tests. Real provider role assignment pending. |
| F10 | Staging Key Vault integration and live fresh/cached responses verified. Local PostGIS test exercises provider timeout alongside schedule reads and dispatcher writes. See docs/evidence/staging-routing. |
| F11 | Persistent snapshots, SSE cursor replay, browser ID deduplication, backend replay test. Full kill/reconnect multi-event browser demo remains. |
| F12 | Three read tools, preview and bound confirmed write; explicit tool scopes, tool-call audits, HTTP transport denial tests. Real staging role, scope, override and replay checks passed. Independent host OAuth discovery remains pending. |
| F13 | React/TS confirmation App and authenticated MCP resource bundled. Staging test-host recording and persisted confirmation verified in docs/evidence/staging-mcp. Independent host consent remains pending. |
| D1 | Staging A record resolves to Azure; DNS/public HTTP evidence captured. Production DNS and full zone/trace dossier remain pending. |
| D2 | Real staging certificate, HTTPS health and HTTP redirect verified; automated renewal configured. CAA and fired expiry alert still pending. |
| D3 | Staging VM, separate vault and application containers deployed. Production and staging-to-production gates are pending. |
| D4 | CI builds/tests and builds images; local release script has migration and HTTPS health gates. Remote CI, promotion and rollback evidence pending. |
| D5 | ARM images, fresh-volume smoke, Bicep deployment and cloud-init succeeded; host setup and secret retrieval are documented. Independent clean-clone reproduction remains pending. |
| D6 | API no-store default. CDN and HIT/MISS/304/invalidation evidence pending. |
| D7 | Security headers and exact-origin CORS; allowed/refused preflight integration tests pass. Deployed frontend CSP and browser/CDN evidence pending. |
| D8 | Staging lab before/after measurements for LCP, INP and CLS are in docs/evidence/web-vitals. Median LCP decreased 800 ms; INP increased 40 ms. |
| D9 | Seven staging states scanned with axe; keyboard-only search, recovery, dispatcher update and MCP confirmation verified. See docs/evidence/accessibility. |
| D10 | Staging OpenTelemetry routing logs, metrics, linked external client and API spans; scheduled synthetic SLO, dashboard and alert evidence in docs/evidence/routing-observability. |
| D11 | Verified budget and retail meters, modeled 1×/10×/100× costs and measured a short staging read workload. See docs/evidence/cost. Portal screenshot pending; remaining credit unknown. |
| S1 | REST writes, MCP transport and provider-backed route limiter configured. HTTP write-rate test proves 429 after 30 requests. Provider-route/login throttle transcripts remain pending. |
| S2 | Local runbook written. An actual intentional operational Game Day and postmortem remain. Unit-test failures are not a substitute. |
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

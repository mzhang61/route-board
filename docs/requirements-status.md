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
| F10 | Server Geoapify client and stub tests; no live API key/access/quota/cost confirmation. Cache stampede control and outage demo remain. |
| F11 | Persistent snapshots, SSE cursor replay, browser ID deduplication, backend replay test. Full kill/reconnect multi-event browser demo remains. |
| F12 | Three read tools, preview and bound confirmed write; explicit tool scopes, tool-call audits, HTTP transport denial tests. OAuth discovery/real host interoperability pending. |
| F13 | React/TS confirmation App and authenticated MCP resource bundled. Actual host recording and host consent verification pending. |
| D1 | Planned production mzcampuspulse.net, staging staging.mzcampuspulse.net. No DNS changes or dig evidence. |
| D2 | Production HTTPS/HSTS middleware exists; real TLS, CAA, redirects, renewal, expiry alert not deployed. |
| D3 | Student subscription enabled and staging resource group created; separate Compose configuration exists, but neither application environment is deployed. |
| D4 | CI builds/tests and builds images; local release script has migration and HTTPS health gates. Remote CI, promotion and rollback evidence pending. |
| D5 | Both container builds and fresh-volume deployment smoke pass; Bicep compiles. Clean-clone/cloud reproduction and full host bootstrap pending. |
| D6 | API no-store default. CDN and HIT/MISS/304/invalidation evidence pending. |
| D7 | Security headers and exact-origin CORS; allowed/refused preflight integration tests pass. Deployed frontend CSP and browser/CDN evidence pending. |
| D8 | No Core Web Vitals before/after claim. Measurement and improvement pending. |
| D9 | Empty-board axe check and keyboard form test pass locally. Full populated/dispatcher/MCP keyboard walkthrough pending. |
| D10 | Basic framework logging only. OpenTelemetry, distributed trace, SLI/SLO and fired alert pending. |
| D11 | Existing USD 20/month budget and thresholds checked; candidate VM compute price verified. Full cost model, credit balance and priced 1×/10×/100× analysis pending. |
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

The user owns mzcampuspulse.net through Porkbun and has an enabled Azure for Students subscription. A staging resource group exists and Compute registration is complete. Needed: complete host bootstrap/cost estimate, real OIDC registration and role/scope assignments, Geoapify plan and credentials, and DNS/CDN setup. Secrets belong in secret stores, not this document or chat. No VM or DNS records have been created.

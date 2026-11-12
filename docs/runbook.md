# Local runbook and Azure deployment plan

## Local triage

1. Follow README prerequisites and setup, then run `docker compose ps`. The database should be healthy on loopback port 5433.
2. Run the API on 5080 and request `/healthz`. A non-200 means database readiness is unavailable; inspect application/database status without printing credentials.
3. If migration tables are absent, run the documented EF update command. Do not use `database update 0` on development/production.
4. If writes return 401, check issuer/audience/expiration and provider setup. If 403, check role and exact scope. Never disable validation to make the demo work.
5. If 409, refresh the current trip and review the change before retrying with its new version. Do not automatically overwrite another operator's work.
6. If external routing is unavailable, use the fixed timetable and inspect timestamp/status. Check provider quota privately. Do not log the credential-bearing URL.
7. If SSE disconnects, reconnect with its last event ID. The board refreshes snapshots and ignores duplicate IDs. Keep replay history intact.
8. If positions stop, the UI labels old timestamps stale. Check `Simulation:Enabled`, running trips, and worker error categories. The simulator never creates real GPS observations.
9. Run `bash scripts/test.sh` before changing code. Test databases have unique `routeboard_test_` names; cleanup runs after tests. Inspect any leftovers before deleting only confirmed test databases.

## Planned Azure layout (not provisioned)

- Production frontend: `https://mzcampuspulse.com`.
- Staging frontend: `https://staging.mzcampuspulse.com`.
- API ingress, database, secret store, identity audiences, storage and configuration separated by environment.
- Separate migration identity and limited runtime database identity.
- Use a static-web host for the React bundle and a container host for the API/MCP resource. Disable proxy buffering and verify timeout support for SSE.
- Put the selected Azure edge/CDN in front. Do not select a paid tier until its current cost is reviewed against the student's budget.
- Use Azure workload identity for CI deployment rather than a long-lived publish password. Protect production with an environment approval/gate and promote the tested build unchanged.

## DNS/TLS steps after resources exist

Export the current Porkbun zone before editing it. Record exact Azure-provided target names instead of inventing A/CNAME targets. Begin staging cutover with 300-second TTL so mistakes can be corrected promptly; after stability use 3600 seconds to reduce resolver traffic. Use an apex alias/flattening feature only if supported by the actual hosting/registrar combination.

Add provider validation records, confirm certificate issuance requirements, and configure CAA to authorize the actual certificate issuer. Do not guess an issuer or add a restrictive CAA record before the certificate service is chosen. Demonstrate HTTP redirect, certificate chain, renewal automation, and a deliberately triggered expiry alert. Capture `dig +trace` and the redacted zone export.

## Deployment gates and rollback to demonstrate

Build/test → immutable artifact → staging migration → staging deploy → `/healthz` + auth/replay smoke tests → production approval → production migration/deploy → smoke tests. Save commit SHA and image digest/build ID at every step. Force one staging failure and show production did not change. Deploy a deliberate bad app version in staging and restore the previous image. Backward-compatible migrations avoid making rollback dependent on data destruction.

## Required Game Day (not yet performed)

In staging, disconnect an arrival-board client, record multiple authorized changes, reconnect, and compare its recovered state with the database. Force provider timeouts while performing a dispatch update. Observe bounded failures, accurate stale labels, consistent audit/event counts, and live-state recovery. Trigger the SLO alert and record time to detection/recovery.

Write the postmortem only after the experiment: actual timeline, impact, trigger, detection, root cause, recovery, and corrective actions, with raw logs. No prewritten outcome is evidence.

### Certificate expiry alert

GitHub Actions checks the verified production and staging TLS certificates daily. The job fails and emits a GitHub error annotation for any certificate within 21 days of expiry or any failed TLS handshake. Inspect the failed run to identify the host. Check DNS, ports 80/443 and Caddy's certificate renewal logs, then rerun the normal check. Workflow notification delivery depends on the operator's GitHub notification settings.

To reproduce, run python3 scripts/check-certificates.py. For a controlled alert, manually dispatch the Public certificate health workflow with drill enabled. Its 365-day threshold applies only to that run. Expect the job to fail on currently valid short-lived certificates. Repeat with drill disabled to verify normal status.

### Staging route estimate SLO

The external Staging route SLO workflow samples the seeded RB-DEMO route three times every six hours. The SLI is the fraction of samples returning HTTP 200, a positive estimate with a fresh, cached or stale status, and a total round trip of five seconds or less. The target is three good samples per check. This measures sampled availability of usable estimates, including cached fallback. It does not measure provider freshness or all production traffic.

Open the workflow run summary for latency, status, outcome and trace ID. If it fails, check the public health endpoint and route catalog first. Confirm the fixed probe ID still identifies RB-DEMO. Inspect the API container logs for the matching trace ID; compare the request span, the routing log's outcome and the cumulative routeboard.routing.estimates counter. Compare client and server durations before attributing time to the provider. A failed workflow emits an error annotation. Delivery of email notifications depends on GitHub account settings.

To practice, dispatch the workflow with drill=true. One call deliberately requests a missing route; expect one 404 and a failed check. Dispatch normally for recovery. No operational records are written. To reproduce manually, install scripts/observability/requirements.txt in a Python virtual environment and run python scripts/observability/routing_probe.py from the repository root.

### Arrival-board Game Day

Follow docs/game-day-postmortem.md for the intentional staging client disconnect, expected ETA, recovery procedure and SQL verification. Use the synthetic dispatcher fixture. Each rehearsal creates a new synthetic trip.

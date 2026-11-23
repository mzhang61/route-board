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

## Deployed Azure layout

- Production: `https://routeboard.mzcampuspulse.com/`; staging: `https://staging.mzcampuspulse.com/`.
- Each environment has its own Azure host, PostgreSQL/PostGIS data, Key Vault, and Auth0 API audience. Cloudflare provides DNS and CDN; Caddy terminates HTTPS and forwards to the private API.
- The host managed identity reads only that environment's secrets. Runtime and migration database settings are mounted separately.
- Staging receives tested releases through the automated workflow/timer. Production promotion is operator-triggered and has not been fully automated.
- See `requirements-status.md` and the environment evidence folders before claiming a control is complete.

## DNS/TLS operations

The current DNS delegation, record export, TTL rationale, CAA, public certificate checks and fired expiry alert are recorded in `evidence/public-dns/` and `evidence/public-tls/`. Before editing the zone, export its current records and record why each TTL is appropriate. Do not replace deployment targets or certificate policy from an example. Reproduce the recorded DNS traces with the commands in the DNS evidence README. Certificate checks use normal TLS validation; never disable verification to make a release appear healthy.

## Release gates and rollback evidence

The staging release records its revision/image information and checks `/healthz`. The staging timer keeps the prior image configuration and restores it after a candidate health/revision failure. The raw VM journal and successful restore pipeline are indexed in [staging rollback evidence](evidence/staging-rollback/README.md). Production promotion remains manual. A code rollback does not revert database contents, so migrations must remain backward-compatible.

### Staging release check and recovery

1. Open the [GitHub Actions workflow](https://github.com/mzhang61/route-board/actions) and select the run for the commit under review. Confirm `verify` passed before checking the `staging` job.
2. Check the public revision and health endpoints:

       curl -fsS https://staging.mzcampuspulse.com/release.txt
       curl -fsS https://staging.mzcampuspulse.com/healthz

   The release file must contain the run's full commit SHA, and health must return `Healthy`. Check production separately at `https://routeboard.mzcampuspulse.com/release.txt` and `/healthz`; the staging workflow does not promote production.
3. If the staging candidate fails, do not promote it and do not edit the production host or either database. The staging timer polls every three minutes. It restores the previously active image configuration after a candidate health or revision failure.
4. In Azure Portal, open **Virtual machines → routeboard-staging-host → Operations → Run command → RunShellScript**. Use these read-only commands to inspect timer state and the recovery log:

       sudo systemctl status routeboard-staging.timer routeboard-staging.service --no-pager
       sudo journalctl -u routeboard-staging.service --since '30 minutes ago' --no-pager | grep -E 'Candidate failed|Previous staging release recovered|Release health check passed|Deployed '

   Do not include environment files, Key Vault values, access tokens, or full unfiltered application logs in evidence.
5. Recheck `/release.txt` and `/healthz` on staging. A recovered service should report the prior successful commit SHA and `Healthy`. If those checks do not pass after the timer's next poll, keep production unchanged and follow the Azure incident steps in `deploy/azure-staging.md`.
6. The recorded revision-mismatch exercise and the exact sanitized output are in [the D4 evidence](evidence/staging-rollback/README.md). The GitHub drill run is labeled cancelled because the restore commit superseded it; the Azure VM journal records the actual rollback.

## Completed Game Day

The staging Game Day deliberately interrupted the public browser connection while a dispatcher committed two synthetic trip updates. On recovery, the browser sent its previous `Last-Event-ID`, replayed both events in order, and matched the final trip and database state. The exact observations and limitation (it did not test a full browser restart) are in `game-day-postmortem.md` and `evidence/staging-replay/`.

The SLO alert drill is separately recorded in `evidence/routing-observability/`. Do not describe it as part of the SSE Game Day; the two drills exercised different failure paths.

### Certificate expiry alert

GitHub Actions checks the verified production and staging TLS certificates daily. The job fails and emits a GitHub error annotation for any certificate within 21 days of expiry or any failed TLS handshake. Inspect the failed run to identify the host. Check DNS, ports 80/443 and Caddy's certificate renewal logs, then rerun the normal check. Workflow notification delivery depends on the operator's GitHub notification settings.

To reproduce, run python3 scripts/check-certificates.py. For a controlled alert, manually dispatch the Public certificate health workflow with drill enabled. Its 365-day threshold applies only to that run. Expect the job to fail on currently valid short-lived certificates. Repeat with drill disabled to verify normal status.

### Staging route estimate SLO

The external Staging route SLO workflow samples the seeded RB-DEMO route three times every six hours. The SLI is the fraction of samples returning HTTP 200, a positive estimate with a fresh, cached or stale status, and a total round trip of five seconds or less. The target is three good samples per check. This measures sampled availability of usable estimates, including cached fallback. It does not measure provider freshness or all production traffic.

Open the workflow run summary for latency, status, outcome and trace ID. If it fails, check the public health endpoint and route catalog first. Confirm the fixed probe ID still identifies RB-DEMO. Inspect the API container logs for the matching trace ID; compare the request span, the routing log's outcome and the cumulative routeboard.routing.estimates counter. Compare client and server durations before attributing time to the provider. A failed workflow emits an error annotation. Delivery of email notifications depends on GitHub account settings.

To practice, dispatch the workflow with drill=true. One call deliberately requests a missing route; expect one 404 and a failed check. Dispatch normally for recovery. No operational records are written. To reproduce manually, install scripts/observability/requirements.txt in a Python virtual environment and run python scripts/observability/routing_probe.py from the repository root.

### Arrival-board Game Day

Follow docs/game-day-postmortem.md for the intentional staging client disconnect, expected ETA, recovery procedure and SQL verification. Use the synthetic dispatcher fixture. Each rehearsal creates a new synthetic trip.

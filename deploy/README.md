# Deployment tooling

Status: local container and fresh-database verification completed; no public application deployment yet.

## Build and verify

Run from the repository root:

```sh
bash scripts/test.sh
docker build -t routeboard-api:release-check .
docker build -f deploy/web.Dockerfile -t routeboard-web:deployment-check .
python3 scripts/release-smoke.py
az bicep build --file deploy/host.bicep
```

The release smoke test uses a fresh isolated database volume, runs migrations twice, applies runtime grants, and checks the API over a local HTTPS proxy. It removes only its temporary containers and volumes. It requires the local `routeboard-postgis:17-local` image built by the development Compose setup. Test ports 8090/8444 and subnet 172.30.98.0/24 must be free.

OIDC authority, client ID and scopes are public web build arguments. Never pass a client secret. Login remains unavailable until a real identity provider is configured; protected endpoints do not gain an authentication bypass.

## Release inputs

Use separate Compose project names, databases, credentials, settings files, certificates and network ranges per environment. Required environment-file values are API_IMAGE, WEB_IMAGE, DB_IMAGE, SITE_ADDRESS (domain only), OIDC_ORIGIN, SETTINGS_FILE, MIGRATION_SETTINGS_FILE, DATABASE_PASSWORD_FILE and RUNTIME_PASSWORD_FILE. Settings files contain the runtime or migration connection string respectively. The runtime username is `routeboard_runtime`; the database administrator is `routeboard_migrator`. Keep all settings/password files outside Git and in an owner-only directory. Mounted settings are not a substitute for the required Azure secret-store integration, which remains pending.

Promote immutable image digests. For a configured host, run:

```sh
bash deploy/release.sh staging /absolute/path/release.env
```

The command validates configuration, waits for PostgreSQL, runs the explicit `--migrate` maintenance command, applies restricted runtime grants, starts API/ingress, and checks public HTTPS `/healthz`. A failed check exits nonzero and must stop promotion. Automatic rollback is not implemented yet. The API never applies schema migrations as part of normal startup.

The SQL initializer creates the runtime login only on a fresh database volume. Never delete an existing volume to fix a failed initialization. Diagnose and repair the specific failure with the administrator account. Credential rotation requires an explicit administrator password update and replacement runtime configuration; changing a Docker secret alone does not rotate an existing PostgreSQL login.

The runtime can read the catalog, update trips/positions and append audit/replay events. It cannot mutate audit history, disable audit triggers, create tables or delete replay events. The migration owner remains privileged, so protecting that credential is still necessary.

## Ingress and networking

Only web ports are published. Caddy terminates HTTPS, serves React and proxies API/MCP/SSE. The API accepts forwarded headers only from PROXY_IP. APP_DYNAMIC_RANGE excludes that static address; otherwise Docker can allocate it to the database first and prevent ingress startup. For additional stacks, choose nonoverlapping APP_SUBNET, APP_DYNAMIC_RANGE and PROXY_IP values.

HTML revalidates; fingerprinted assets are immutable; API responses remain no-store. Caddy automatically flushes event-stream responses while preserving disconnect cancellation. CDN public cache policy and HIT/MISS/invalidation evidence remain pending. Two ingress containers cannot bind the same host ports; use separate hosts or implement a shared ingress before colocating environments.

## Azure host and costs

The template targets a pinned Ubuntu 24.04 ARM image in West US. Region/SKU availability was checked against the student's subscription. Older B1ms was restricted; B2pts_v2 and B2pls_v2 were listed without location restrictions. Actual allocation can still fail due to capacity or quota.

The official retail API returned USD 0.01/hour for standard Linux B2pts_v2 in West US: 730 × 0.01 = USD 7.30/month compute. Disk, public IPv4, traffic, backup and telemetry are additional. Do not interpret this as an all-in quote or a guarantee of remaining student credit. The smaller VM needs memory/load validation before production use. No billable VM was created.

The Bicep template is not a complete deployment: host package installation, secret retrieval, backup, CI release gating and rollback still need implementation. Real DNS/CAA/certificate renewal, alert and CDN evidence must come from an actual deployment.

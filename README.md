# RouteBoard

A fictional shuttle dispatch and arrival board built with ASP.NET Core 10, PostgreSQL/PostGIS, and strict React/TypeScript. All schedules, vehicles, observations, and positions are synthetic. External routing is reference information, not real GPS or live traffic.

## Instructor review

1. Open the [production board](https://routeboard.mzcampuspulse.com/), select a route, and choose **Search trips**. Open a trip to compare its scheduled, estimated and simulated observed times. Public browsing requires no account.
2. For the live role demonstration, request the synthetic staging viewer and dispatcher credentials from the project owner through the private course channel. Use separate browser sessions on the [staging board](https://staging.mzcampuspulse.com/). Try creating a synthetic trip as each role. Expect a permission denial for the viewer and a saved trip for the dispatcher. The hosted app supplies its own database and routing credentials. [Recorded role checks](docs/evidence/staging-oidc/README.md)
3. For a local review, follow [clean-machine setup](#clean-machine-setup) and [run locally](#run-locally). From the repository root, run `bash scripts/test.sh` with the local database running. Compare the results with the [hand predictions](docs/test-predictions.md). [Clean-clone evidence](docs/evidence/clean-clone/README.md)

| Review topic | Where to verify it |
| --- | --- |
| Coverage and outstanding proof | [Requirement checklist and five hard gates](docs/requirements-status.md) |
| REST, concurrency and audit | [HTTP transcripts](docs/evidence/rest-contract/README.md) · [Database audit](docs/evidence/staging-audit/README.md) |
| Live updates and interactive MCP | [Disconnect and replay](docs/game-day-postmortem.md) · [MCP demo and recording](docs/evidence/staging-mcp/README.md) |
| Deployment and operations | [GitHub Actions](https://github.com/mzhang61/route-board/actions/workflows/ci.yml) · [Infrastructure configuration](deploy/host.bicep) · [Runbook](docs/runbook.md) |
| Load, scaling and cost | [Workload assumptions and measurements](docs/evidence/cost/README.md) |
| Architecture and individual defense | [Request path and trust boundaries](docs/architecture-maintenance.md) · [Oral-defense Q&A](docs/oral-defense.md) · [AI-use log](docs/ai-provenance.md) |

## Current implementation

- Search trips by route/vehicle text, route, stop, departure interval, and status.
- Create trips and record departure, ordered stop arrival, total delay, and cancellation through protected endpoints.
- Calculate scheduled and estimated arrivals; display simulated observations separately.
- Reject stale updates; atomically persist trip state, append-only audit, and replayable events.
- Serve SSE replay and a browser board with explicit loading, empty, and error states.
- Validate JWT signature, issuer, audience, expiry, role, and scopes.
- Call Geoapify from the server with bounded timeouts/retries, response validation, and timestamped cache fallback.
- Expose scoped MCP read tools and a confirmed write tool, with a bundled React confirmation App.
- Optionally generate synthetic coordinates by linear interpolation, persisting latest position and sequence.

Browse the [production board](https://routeboard.mzcampuspulse.com/) or [staging board](https://staging.mzcampuspulse.com/). For the current evidence and remaining submission work, see [the checklist](docs/requirements-status.md).

## Prerequisites

- .NET 10 SDK and Python 3
- Node.js 26 and npm
- Docker with PostgreSQL/PostGIS support
- A real OIDC provider for dispatcher login (public browsing does not require it)

## Clean-machine setup

Start Docker before running these commands. The public local demo and automated suite use a generated database password and synthetic records. No Azure subscription, Auth0 account or Geoapify key is required for this path. Port 5433 must be free. To use a different port, export ROUTEBOARD_DB_PORT before setup and keep it set in every terminal running these commands. The setup script writes to the current OS user's .NET User Secrets store for this project; use a separate OS account for independent development installations on the same machine.

```bash
git clone https://github.com/mzhang61/route-board.git
cd route-board
python3 scripts/setup-local.py
docker compose up -d --build --wait
dotnet tool restore
dotnet restore RouteBoard.slnx
npm ci --prefix frontend
(cd frontend && npx playwright install chromium)
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --project backend/RouteBoard.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/RouteBoard.Api --no-launch-profile -- --seed-demo
```

The setup script creates a local Docker password if missing and stores the application connection string in .NET User Secrets without printing it. Never reuse this local password for a cloud deployment. Do not remove an existing Docker volume to resolve a credential mismatch; restore the matching local credential instead.

## Run locally

```bash
python3 scripts/build-mcp.py
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5080 dotnet run --project backend/RouteBoard.Api --no-launch-profile
```

In another terminal:

```bash
cd frontend
npm run dev
```

Open the loopback URL Vite prints. Its development proxy forwards API calls to port 5080. `/healthz` checks database connectivity. There is no authentication bypass. Without OIDC configuration, public browsing works and writes remain protected.

Set `Simulation__Enabled=true` on the API process to generate synthetic positions for running trips. Positions are not road-following and do not drive dispatch state automatically.

## One verification command

```bash
bash scripts/test.sh
```

This builds strict TypeScript and the MCP App, runs browser/axe checks, and runs the backend suite against uniquely named disposable PostgreSQL databases. It does not roll back `routeboard_dev`. The database role must be allowed to create test databases. In CI or a different local setup, supply `ROUTEBOARD_TEST_ADMIN_CONNECTION` through a secret/environment store. Never paste it into evidence.

Tests deliberately use generated RSA keys and stubbed provider responses. They do not prove a real provider login, JWKS rotation, or a successful live Geoapify call.

## Configuration

Server secret/configuration sources: .NET User Secrets in development; an environment-specific Azure Key Vault on the deployed hosts. Required public identity settings are `Authentication:Authority` and `Authentication:Audience`. The API accepts RS256 access tokens, `roles` claims, `sub`, and the space-delimited `scp` or `scope` claim. REST dispatcher operations require `dispatcher` and `trips.write`.

For the browser, configure public `VITE_OIDC_AUTHORITY`, `VITE_OIDC_CLIENT_ID`, `VITE_OIDC_SCOPE`, and the API-specific `VITE_OIDC_AUDIENCE` values. Register `/auth/callback` for each exact frontend origin. No client secret or provider API key belongs in a Vite variable. OIDC uses code + PKCE, a generated nonce, library-managed state, and session storage; it does not load user profiles from the user-info endpoint.

`Geoapify:ApiKey` is server-only. Do not enable live calls until account access, caching terms, quota, attribution, and cost have been confirmed. The current cache is fresh for one hour and retained for up to 24 hours in memory; restart loses it. Existing fixed synthetic timetables remain available when reference routing fails.

## Documentation

- [Architecture and maintenance](docs/architecture-maintenance.md)
- [Oral-defense questions](docs/oral-defense.md)
- [Requirement and evidence status](docs/requirements-status.md)
- [Runbook and deployment plan](docs/runbook.md)
- [AI provenance and challenged claims](docs/ai-provenance.md)
- [Hand predictions](docs/test-predictions.md)
- [Local evidence](docs/evidence/README.md)

GitHub Actions tests main and publishes verified ARM images for staging. Production promotion uses the verified API image and a web build with production-specific public OIDC settings. See [Azure deployment](deploy/azure-staging.md) and [production verification](docs/evidence/production-rate-limits/README.md).

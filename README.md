# RouteBoard

A fictional shuttle dispatch and arrival board built with ASP.NET Core 10, PostgreSQL/PostGIS, and strict React/TypeScript. All schedules, vehicles, observations, and positions are synthetic. External routing is reference information, not real GPS or live traffic.

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

This is a local implementation increment, **not a completed course submission**. Real OIDC login, live provider access, MCP host interaction, Azure staging/production, DNS/TLS/CDN, operational alerts, and several required evidence artifacts remain unverified. See [the checklist](docs/requirements-status.md).

## Prerequisites

- .NET 10 SDK and Python 3
- Node.js 26 and npm
- Docker with PostgreSQL/PostGIS support
- A real OIDC provider for dispatcher login (public browsing does not require it)

## Clean-machine setup

```bash
git clone https://github.com/mzhang61/route-board.git
cd route-board
python3 scripts/setup-local.py
docker compose up -d --build
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

Server secret/configuration sources: .NET User Secrets in development; an Azure secret store in the future deployment. Required public identity settings are `Authentication:Authority` and `Authentication:Audience`. The API accepts RS256 access tokens, `roles` claims, `sub`, and the space-delimited `scp` or `scope` claim. REST dispatcher operations require `dispatcher` and `trips.write`.

For the browser, configure public `VITE_OIDC_AUTHORITY`, `VITE_OIDC_CLIENT_ID`, and `VITE_OIDC_SCOPE` values. Register `/auth/callback` for each exact frontend origin. No client secret or provider API key belongs in a Vite variable. OIDC uses code + PKCE, a generated nonce, library-managed state, and session storage; it does not load user profiles from the user-info endpoint.

`Geoapify:ApiKey` is server-only. Do not enable live calls until account access, caching terms, quota, attribution, and cost have been confirmed. The current cache is fresh for one hour and retained for up to 24 hours in memory; restart loses it. Existing fixed synthetic timetables remain available when reference routing fails.

## Documentation

- [Architecture and maintenance](docs/architecture-maintenance.md)
- [Oral-defense questions](docs/oral-defense.md)
- [Requirement and evidence status](docs/requirements-status.md)
- [Runbook and deployment plan](docs/runbook.md)
- [AI provenance and challenged claims](docs/ai-provenance.md)
- [Hand predictions](docs/test-predictions.md)
- [Local evidence](docs/evidence/README.md)

The GitHub Actions definition runs verification only. The Dockerfile packages the API and MCP resource; the web bundle is a separate hosting artifact. Neither cloud CD nor a cloud container build has been demonstrated yet.

# Clean-clone local grading rehearsal

Verified public repository revision e0f61e463d4c1327c3eec4b97364874e0b4ec904. Follow the root README from clone through Run locally and One verification command. The prediction was a new synthetic route and one trip, a healthy API, an anonymous write refusal and a passing test suite. The browser opened the public board through Vite and activated the first View trip button. Chromium displayed the seeded trip heading.

The rehearsal ran on macOS ARM64 with existing SDKs and Docker Desktop. It used a fresh clone, newly created PostGIS volume, isolated .NET user secrets (APPDATA), and new NuGet package, npm and Playwright browser directories. Docker reused cached image layers. The machine and global developer tools were already installed. This is an automated clean-repository test; an independent person on a newly provisioned OS has not run it.

For an isolated rehearsal alongside an existing development installation, set ROUTEBOARD_DB_PORT=15433, COMPOSE_PROJECT_NAME=routeboard-grading-clean and point APPDATA to a fresh private directory on macOS. Keep those variables set in all shells. Optional cache overrides used here were NUGET_PACKAGES, npm_config_cache and PLAYWRIGHT_BROWSERS_PATH. Run the database setup, package installations, migrations and seed from the root README. Start API on 5080 and Vite on 5173. Verify /healthz through the frontend proxy, open the trip, then run bash scripts/test.sh. No cloud credentials are required.

For the negative check, send an anonymous POST with an empty JSON object to /api/trips and expect 401. For the restart test, use docker compose exec -T db psql -U routeboard -d routeboard_dev and run the SQL below before and after docker compose restart db followed by docker compose up -d --wait db:

    SELECT "Id", "RouteId", "VehicleId", "ScheduledDeparture", "Status", "Version" FROM trips ORDER BY "Id";

The exact rows matched. The test suite passed and git status --porcelain was empty after building and testing. The retained excerpt contains no secrets. Codex performed this run and corrected the initial assumption that the default port would be available: an existing development database already used 5433. No existing database was reconfigured.

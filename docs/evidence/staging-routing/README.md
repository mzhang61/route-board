# Staging Geoapify routing check

Base revision b7c743d2a5bd4bb978f8ccef2fa109d6f15c7e29. The controller and deployment changes accompany this evidence. After a cold API start, the prediction was a fresh estimate followed by a cached response with the same retrieval timestamp. Use the ID for RB-DEMO from the catalog.

    curl -fsS https://staging.mzcampuspulse.com/api/catalog
    curl -fsS https://staging.mzcampuspulse.com/api/routes/2/estimate
    curl -fsS https://staging.mzcampuspulse.com/api/routes/2/estimate
    bash scripts/test.sh

The captured responses in responses.json show 2047 meters and 181.464 seconds. Subsequent runs within the one-hour freshness window can return cached on both requests. The first staging probe assumed ID 1 and repeated failures hit the application rate limit. Using the catalog revealed a separate HTTP 500: PostgreSQL could not evaluate ST_Y on geography. The controller now reads the ordered geography points before extracting their ordinates. No schema change was needed.

RoutingOutageTests runs against a disposable PostGIS database and a stalled HTTP provider. The predicted behavior was two bounded attempts, an unavailable estimate, a successful public schedule read and an authorized trip creation. The test passed. The timeout was induced locally. The live provider was contacted only for the normal route check.

Codex performed these checks and corrected its assumption about the route ID. The published provider responses contain reference measurements and timestamps only. Credentials are excluded.

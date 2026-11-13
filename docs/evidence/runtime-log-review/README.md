# Runtime log credential review

Repository reference: `51f2d74`. Collection used the running staging and production API and Caddy containers. The [observations](results.txt) record the first and last UTC timestamp for each retained stream. The short web logs contain service messages and do not provide a complete request history.

The predicted outcome was a successful routing estimate and an HTTP 401 for a fabricated bearer value, with neither the provider query key nor the bearer value appearing in the collected logs. [Public probe results](probes.txt) record both environments. Each returned a fresh estimate. A POST to /api/trips with the deliberately invalid value was refused in each environment. No real token was supplied. A real login callback was not exercised in this run.

To reproduce, use an owner-only temporary directory. For each environment, GET /api/catalog, GET /api/routes/{routeId}/estimate for a listed route, then POST an empty JSON object to /api/trips with Authorization set to Bearer RouteBoardInvalidLogProbe2026. On each host use docker logs --timestamps for each running RouteBoard API and web container, merging stderr into stdout. Save as staging-api.log, staging-web.log, production-api.log and production-web.log in that directory. With Python 3 and Gitleaks 8.30.1, run:

```sh
python3 docs/evidence/runtime-log-review/check.py "$private_log_directory"
```

AI inspected the retained Docker streams using Gitleaks and the listed patterns. No pattern or probe value matched. Only aggregate counts, dates and public response outcomes are published. Deleted containers, external logging services, older rotated logs, database logs and browser storage are outside this capture.

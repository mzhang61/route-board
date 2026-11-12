# Staging CDN check

The operator purged the exact asset URL recorded in purge.txt in Cloudflare. Before the purge, the same asset had returned HIT. The first subsequent GET from the Chicago edge was expected to miss; the second should hit. The response headers show both results. Repeat with a single-URL purge followed by two curl -sS -D - -o /dev/null requests to the asset. Use its current ETag in an If-None-Match header to reproduce the conditional response.

A synthetic viewer signed in through the staging browser, then made two GET /api/trips requests with its valid bearer token. Both responses prohibited storage and the edge reported DYNAMIC. This checks authenticated requests on a public read route. Only cache metadata was recorded. No bearer tokens, cookies or response bodies are included.

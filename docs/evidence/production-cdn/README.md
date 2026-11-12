# Production CDN verification

After enabling the production proxy, Codex checked the public site using normal HTTPS certificate validation. The health endpoint returned Healthy. The HTML and anonymous trip list returned DYNAMIC. Two GET requests for the same hashed JavaScript asset at the Chicago edge returned MISS then HIT. A request with its ETag returned 304. Repeat with curl -sS -D - -o /dev/null against the paths in headers.json; an already warm asset can return HIT on the first request.

A synthetic viewer signed in through production Auth0 and requested /api/trips twice with a valid bearer token. Both responses contained no-store and DYNAMIC. This check covers the public trip-list route with a signed-in session. The single-URL purge drill was run on staging. Only public headers and cache metadata are retained.

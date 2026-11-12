# Production login gate verification

Promoted API revision c91cf3caf0e9be08b1eb09f049d09e20fb2fbd58 after its staging tests and health gate passed (Actions run 43171227250). The production host verified the release bundle SHA-256 from GitHub before loading it. The web image was built from a git archive of that same revision with production public OIDC settings. Public /release.txt returned the revision and /healthz returned Healthy. The active configuration uses these image IDs:

    API sha256:d4f837302e6471f5507abc753530ac2709caaebbf04b13198682ee39a4aa6000
    Web sha256:de893418414cc836c4fb7bdcfe73033a8f24cf3abd3b78a1fc005938b96f0d90

The two synthetic identities signed in through the application's 302 redirect with PKCE S256, state and nonce present. Each submitted an intentionally invalid trip. The viewer was refused before validation (403); the dispatcher reached input validation (400). No trip was created. An unauthenticated write returned 401.

To reproduce the read-only limiter probe, use a single Node fetch client, issue sequential GETs to /api/auth/authorize with redirect set to manual and consume each body. Stop at the first 429 or after eleven requests. Check GET /api/catalog afterwards. Existing requests in the window can move the first 429 earlier. The first probe with separate curl processes failed a combined assertion; it did not retain the individual codes, so its cause is unknown. A subsequent single-process probe observed nine redirects followed by 429, with public reads still returning 200. The limiter groups by the API-observed address, which can be shared at CDN ingress. This drill demonstrates application responses, not per-person isolation or throttling at Auth0.

Only statuses, presence of public protocol fields and image identifiers are retained. Redirect URLs, authorization values, passwords and tokens are excluded. Production promotion was operator-triggered; the staging workflow remains automatic.

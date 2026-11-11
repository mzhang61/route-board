# Staging identity verification

The staging public SPA uses Authorization Code with PKCE. Its callback is https://staging.mzcampuspulse.com/auth/callback. The issuer is https://dev-okl8jqszwoasg244.us.auth0.com/ and the API audience is https://staging.mzcampuspulse.com/api. The SPA requests openid and trips.write. Auth0 enforces its API permission policy. A post-login Action copies the assigned viewer and dispatcher roles to the signed access-token claim https://staging.mzcampuspulse.com/roles for this API audience only.

The viewer role has the three MCP read permissions. The dispatcher role also has trips.write, mcp.preview_trip_update and mcp.record_trip_update. The API checks the signature, issuer, audience, expiry, roles and scopes. Dashboard management access and RouteBoard dispatcher access require separate assignments.

## Reproduce the browser check

1. Apply deploy/seed-staging-catalog.sql as migration owner on staging to create the fixed synthetic route, stops and vehicle. Reapplying the transaction preserves existing rows.
2. Use separate browser sessions for synthetic Auth0 users assigned viewer and dispatcher respectively. Sign in on the staging board. Inspect the authorization request for response_type=code, code_challenge_method=S256, state, nonce and the configured audience. Keep values of state, nonce, codes, tokens and credentials out of evidence.
3. In each session submit a trip creation for the synthetic route and vehicle. Predict 403 for the viewer and 201 for the dispatcher before submission. The browser-verification.txt output records the outcomes of actual same-origin POST requests made with each browser session's access token. The dispatcher check creates a real synthetic trip on staging.

The deployed artifacts used the working tree based on 5dd3b3b014d9d6e0e14a9ec6c15c957fbdb74529. Local test execution initially timed out during the database permissions test. The provider check recorded selected request properties and response codes only. A full redacted HAR and a live MCP-host recording remain outstanding.

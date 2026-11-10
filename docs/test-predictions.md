# Hand-predicted checks

These expectations were specified before the corresponding tests were run.

- ETA: 10:00 + 8 travel + 3 dwell + 12 travel = 10:23; total delay 4 gives 10:27.
- Anonymous write: 401. Valid viewer or missing write scope: 403.
- Wrong signature, issuer, audience, or expired token: 401.
- Two writes using the same version: one success and one 409; only one new state event and audit.
- Arrival before departure or skipping the next stop: reject without a success audit/event.
- Updating an audit row: PostgreSQL rejects it.
- SSE after cursor N: first replayed ID is greater than N; its snapshot has delay 4, not 8, and excludes actor identity.
- Provider 503: at most two calls then unavailable; 429: one call; negative route duration: unavailable; successful response: next lookup uses cache.
- MCP read-only scope cannot execute writes or another ungranted read tool. False confirmation, another actor, and a reused old-version token fail.
- Synthetic interpolation: 3 minutes dwell + half of a 12-minute leg reaches the midpoint at minute 9. A time older than the last persisted tick does not change sequence or coordinates.
- Migration Up/Down/Up on an isolated database leaves all migrations applied and no application rows.
- Browser empty result has visible no-results text. A stalled call displays loading; a failed call displays an error and retry action.

The fixture tokens, provider bodies, and operational records are synthetic. Real login/provider/host/deployment tests are separate work.

## Restricted runtime database identity

Before execution: the runtime role can read the public catalog and append an audit event. Updating/deleting/truncating audit history, disabling its trigger, creating tables, and deleting replay events must each fail with PostgreSQL insufficient_privilege (42501). This uses a disposable database and a unique temporary role; it does not modify the development role.

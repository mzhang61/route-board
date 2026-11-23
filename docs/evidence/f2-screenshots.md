# F2 — Remote-call loading, empty, and error states

This page gathers the F2 browser screenshots in one place. The PNG files are stored with the evidence in this project, and the relative image embeds below display them in Markdown viewers that support inline images.

The public-page screenshots use synthetic responses intercepted by Playwright. They show loading, forced HTTP 503 failures, recovery, and an empty result. The operator screenshots show sign-in, dispatch-write, and live-update states. These are local browser checks; the trips, identities, and responses are synthetic.

Test sources: [`public-recovery.spec.ts`](../../frontend/tests/public-recovery.spec.ts) and [`operator-states.spec.ts`](../../frontend/tests/operator-states.spec.ts). Evidence notes: [`public-board-states`](public-board-states/README.md) and [`operator-states`](operator-states/README.md).

## Public board and route reference

### Loading

![Loading state for route catalog, trip board, and synthetic positions](public-board-states/loading.png)

### Forced failures

![Forced HTTP 503 errors for remote calls](public-board-states/provider-failures.png)

### Empty state after recovery

![Empty route catalog, trip board, and synthetic positions after retry](public-board-states/recovered-empty.png)

### Route reference loading

![Loading state for external route reference](public-board-states/estimate-loading.png)

### Route reference failure

![Error state for failed external route reference while scheduled timetable remains visible](public-board-states/estimate-failure.png)

### Route reference recovery

![Recovered cached route reference](public-board-states/estimate-recovered.png)

## Sign-in, dispatch, and live connection

### Sign-in progress

![Sign-in is opening](operator-states/sign-in-opening.png)

### Sign-in error and retry

![Sign-in provider failure with retry option](operator-states/sign-in-failed.png)

### Dispatch write in progress

![Dispatch save is in progress](operator-states/dispatch-saving.png)

### Dispatch write denied

![Dispatch write denied because the account lacks permission](operator-states/dispatch-forbidden.png)

### Stale update conflict

![Conflict state after the trip changed](operator-states/dispatch-conflict.png)

### Live updates disconnected

![Live updates disconnected and reconnecting](operator-states/live-disconnected.png)

### Live updates reconnected

![Live updates connected again](operator-states/live-reconnected.png)

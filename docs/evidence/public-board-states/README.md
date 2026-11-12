# Public board failure and recovery

Tested commit `5c5a1f6`. From the repository root run `bash scripts/test.sh` after local setup. Playwright writes the original PNGs under `frontend/test-results/public-recovery-*`. The [test summary](test-summary.txt) contains selected lines from the full run.

The browser ran the application locally with synthetic responses intercepted by Playwright. The test predicted loading indicators while requests were pending, three independent errors on HTTP 503, and empty results after recovery. The user retried the catalog and board separately. The position panel recovered on its next automatic poll.

[Loading](loading.png) · [Forced 503s](provider-failures.png) · [Recovered empty results](recovered-empty.png)

A second test opened a synthetic trip with a fixed timetable. During a route estimate failure, the scheduled stop remained visible. Reopening the trip after restoring the endpoint displayed a ten-minute cached reference with its retrieval time.

[Estimate loading](estimate-loading.png) · [Estimate outage](estimate-failure.png) · [Recovered reference](estimate-recovered.png)

AI wrote the tests and collected the original browser screenshots. The first strict build rejected Node-specific file handling in the browser test project. The final tests use Playwright's output directory. The screenshots use synthetic schedules without accounts, tokens or external API keys.

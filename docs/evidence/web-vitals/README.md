# Public board Web Vitals experiment

Prediction: delivering the static heading and synthetic-data notice in the initial HTML should reduce largest contentful paint (LCP) on a cold browser visit. React still manages live status, sign-in and the board. The title and notice have one source in the HTML.

| Metric (median of five visits per revision) | Before | After |
| --- | ---: | ---: |
| LCP | 1368 ms | 568 ms |
| Interaction to next paint (INP) | 64 ms | 104 ms |
| Cumulative layout shift (CLS) | 0.0597 | 0.0597 |

LCP decreased by 800 ms (58.5%). Interaction latency increased by 40 ms in this sample. The experiment identifies no cause for that increase. Layout shift was unchanged.

## Reproduction

Run npm ci in frontend and install Playwright Chromium. From the repository root run:

    node frontend/tests/measure-vitals.mjs

For the before measurement the deployed staging SHA was a3c4d7334007da00a6c3a3d1815e376d71cb65d3. For after it was 92e1ad9fba84116eaa1ff9971e0bbb5c864b6869. The script prints the actual release per visit. To repeat the full comparison, use the same measurement script with each revision deployed. The two JSON files preserve every observed sample. Use the middle of five sorted values for each metric.

The lab driver used Chromium 158.0.8127.18, a 390 by 844 viewport, fourfold CPU throttling, 150 ms latency, 200000 bytes/s download and 90000 bytes/s upload. Each visit used a fresh unauthenticated browser context and disabled browser cache. The same public board search, open and close interactions were exercised against live staging. CDN cache and network conditions outside the browser were uncontrolled. These samples characterize this lab scenario; a field distribution across devices requires real-user monitoring.

Codex measured the baseline, changed the initial document, ran the production build and browser tests, and repeated the lab run. The check challenged the assumption that faster paint implies faster interaction. The retained output contains only public deployment revisions, browser version and metrics. Recovery and deliberate failure evidence for this deployment pipeline is documented in the staging rollback drill.

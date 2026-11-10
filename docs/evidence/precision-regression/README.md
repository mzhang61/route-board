# Timestamp precision regression

The first remote CI run failed: https://github.com/mzhang61/route-board/actions/runs/42756859693 . Its tested head was `0d7db353b0bd9c00322cd920145e6dbe094fda75`. Raw failed-step output is preserved in remote-failure.txt. The simulation persistence assertion differed by seven .NET ticks, below PostgreSQL timestamp precision.

The fix normalizes synthetic observation time to whole microseconds before ordering and persistence. The test now intentionally supplies seven sub-microsecond ticks and replays one tick later in the same microsecond. Both samples must yield the same stored timestamp and sequence. It still rejects older updates.

`bash scripts/test.sh` passed 36 backend and 2 browser tests on the local working-tree fix; local-tests.txt and source-manifest.txt identify that run. This is not a claim that remote CI has passed yet. The next GitHub workflow retains its tested commit SHA and raw output in the verification artifact.

Logs were inspected for the known local database password and token/private-key patterns before committing. They contain synthetic test actors and failed assertion values, not production credentials. No failure output was rewritten to resemble success.

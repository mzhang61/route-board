# Trip persistence across a staging deployment

The public staging API served synthetic trip 4 before and after a deployment. The prediction was that the release identifier would change while the complete trip response, including version 2 and its four-minute delay, would remain identical. The SHA files identify the two builds. The JSON files contain the original public response bodies. The comparison used an existing trip so there was no need to write to the shared board. AI collected the public responses and checked that the evidence fields contain synthetic trip data and public build identifiers.

From the evidence directory, reproduce the check around a new staging deployment:

```sh
site=https://staging.mzcampuspulse.com
curl -fsS "$site/release.txt" > before.sha
curl -fsS "$site/api/trips/4" > before.json
# After the new candidate passes staging verification:
curl -fsS "$site/release.txt" > after.sha
test "$(cat before.sha)" != "$(cat after.sha)"
curl -fsS "$site/api/trips/4?verification=$(cat after.sha)" > after.json
cmp before.json after.json
shasum -a 256 before.json after.json
```

Both checksums matched. During verification the first attempt still returned the previous SHA and failed the version check. An unchanged release cannot establish redeployment. A staging-host query for the new release tag initially returned 404 while a fresh query returned the published tag. The pull service now requests fresh metadata at each poll. [Successful workflow and public health gate](https://github.com/mzhang61/route-board/actions/runs/43268938964).

# Secret review history and current scope

See [current local working-tree and Git-history scan](current-local-scan.txt) and [current cloud and remote-history review](current-cloud-review.md) for the latest scoped scans. The broader review below is an earlier, revision-bound snapshot. The current cloud review extends coverage to the remote source history, recent CI logs, deployed API/web images, and 30-day Azure runtime logs. The separate [media review](../media-review/README.md) covers project screenshots, recordings, and the redacted HAR.

Gitleaks 8.30.1 scanned the repository at `2b90293e470aeca85989b56d1c53389f8ea9cbe6`, all then-reachable local refs (46 commits), ten completed GitHub Actions logs, and the configuration and individual saved layers of both deployed API and web images. Each image was identified from the running container. No container mounts, database contents or vault values were exported. [Sanitized scanner results and image IDs](results.txt).

For that earlier snapshot, all ten Git hits were reviewed: one image digest, one source revision and eight file checksums in the source manifest. All 44 workflow hits were lines containing commit-based Docker tags. No live credential was identified in those findings. The web images had zero detections. Both API images had zero detections and 17 archive-read diagnostics each. Follow-up extraction encountered unresolved documentary changelog links in the base layers. These entries remain a coverage limitation.

To repeat the earlier local review, create a private temporary directory, then run the following from a fresh checkout. Replace RUN with a listed run ID. For each image, use docker inspect on the running container to identify its image ID, docker save that ID, and use the archive's manifest.json to copy its Config and Layers entries into a private image directory. Current cloud scope and results are in `current-cloud-review.md`.

```sh
umask 077
scan=$(mktemp -d)
mkdir "$scan/logs"
gitleaks git --log-opts='--all --full-history' --redact=100 --report-format=json --report-path="$scan/git.json" .
gh run view RUN --repo mzhang61/route-board --log > "$scan/logs/RUN.txt"
gitleaks dir --redact=100 "$scan/logs"
gitleaks dir --max-archive-depth=3 --redact=100 "$scan/image-directory"
```

A scanner exit status of 1 indicates findings requiring review. Only redacted metadata is published. Detection rules have limited coverage of opaque credentials and binary formats. The review below is historical and retains its original scope; see the current report for the later scans and remaining limits.

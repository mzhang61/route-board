# Pre-submission secret review

Gitleaks 8.30.1 scanned the repository at `2b90293e470aeca85989b56d1c53389f8ea9cbe6`, all local refs (46 reachable commits), ten recent completed GitHub Actions logs, and the configuration and individual saved layers of both deployed API and web images. Each image was identified from the running container. No container mounts, database contents or vault values were exported. [Sanitized scanner results and image IDs](results.txt).

All ten Git hits were reviewed: one image digest, one source revision and eight file checksums in the source manifest. All 44 workflow hits were lines containing commit-based Docker tags. No live credential was identified in these findings. The web images had zero detections. Both API images had zero detections and 17 archive-read diagnostics each. Follow-up extraction encountered unresolved documentary changelog links in the base layers. These entries remain a coverage limitation.

To repeat, create a private temporary directory, then run the following from a fresh checkout. Replace RUN with a listed run ID. For each image, use docker inspect on the running container to identify its image ID, docker save that ID, and use the archive's manifest.json to copy its Config and Layers entries into a private image directory.

```sh
umask 077
scan=$(mktemp -d)
mkdir "$scan/logs"
gitleaks git --log-opts='--all --full-history' --redact=100 --report-format=json --report-path="$scan/git.json" .
gh run view RUN --repo mzhang61/route-board --log > "$scan/logs/RUN.txt"
gitleaks dir --redact=100 "$scan/logs"
gitleaks dir --max-archive-depth=3 --redact=100 "$scan/image-directory"
```

A scanner exit status of 1 indicates findings requiring review. AI collected the inputs and classified the reported locations. Only metadata is published. Detection rules have limited coverage of opaque credentials and binary formats. Screenshots, video, cloud application logs, older workflow logs and the database image remain outside this review. Repeat on the final submission revision.

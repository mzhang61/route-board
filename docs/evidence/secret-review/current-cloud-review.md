# Current cloud and remote-history secret review

Date: 2026-11-19 UTC  
Scanner: Gitleaks 8.30.1  
Purpose: extend the local review to the currently deployed images, bounded cloud log windows, recent GitHub Actions archives, and the GitHub-hosted source history. This is a scoped review, not a guarantee about arbitrary external systems or every possible encoded secret.

## Scope

- Public GitHub source history cloned from the production VM: 55 commits, including the commits added through GitHub's web editor that were absent from the local Git database.
- GitHub Actions archives for runs 43, 44, and 45, plus the earlier ten-run set recorded in [the historical review](README.md).
- The currently running API and web images in staging (`db7ef58`) and production (`c91cf3c`), their archive metadata/layers, and each API/web container's last 30 days of logs.
- Staging and production host journals, last 30 days.
- Staging and production database container logs, last 30 days.
- Existing database image review and the separate [media review](../media-review/README.md).

Cloud checks used Azure VM Run Command from the signed-in portal session. The review did not export database contents, mounted volumes, or Key Vault values, and did not restart or reconfigure application services. Temporary scan directories and the Gitleaks scanner image were removed after each run. Disk use returned to its pre-scan level.

## Results

| Input | Scanner result | Review |
|---|---:|---|
| Remote source history, 55 commits | 10 pattern matches | Eight source-manifest file checksums, one public API image digest, and one public Git commit ID. |
| Recent Actions archives, runs 43, 44, 45 | 16 pattern matches total | All matched public commit-based image tags or image digests. |
| Staging API/web images, their metadata/layers, host journal, and 30-day API/web logs | 65 pattern matches | 63 public image revision/digest identifiers and two public certificate fingerprints. No API/web application-log matches. |
| Production API/web images, host journal, and 30-day API/web logs | 4 pattern matches | Two public image revision/digest identifiers and two public certificate fingerprints. No API/web application-log matches. The unmatched-looking journal values were confirmed from sanitized context to be kernel X.509 certificate fingerprints, not private keys. |
| Staging database container logs, last 30 days | 0 | No pattern matches. |
| Production database container logs, last 30 days | 0 | No pattern matches. |
| Database image | 0 in the existing review | Current running image ID matches the previously reviewed image; see [database image review](../database-image-review/README.md). |
| Screenshots, recordings, redacted HAR | 0 in the existing review | See [media review](../media-review/README.md). |

Gitleaks exits with status 1 when a pattern match is found. The matches above were manually compared to public commit IDs, image tags/digests, source-manifest checksums, or public certificate fingerprints. No credential value was printed or copied into this report. Raw cloud scan outputs were kept in temporary private locations and removed after classification; the report preserves only redacted counts and classifications. Therefore, this report is a sanitized result summary, not the raw scanner transcript required for a complete Q3 dossier. To close that evidence detail, retain a safely redacted scanner report from a repeat run and attach its commit/revision, command, and interpretation.

The three downloaded GitHub run archives remain in the user's Downloads folder because the local filesystem denied deletion. They are public workflow logs, not secret-bearing exports; their only scanner matches were reviewed public identifiers. They are not part of the repository evidence bundle.

## Reproduction outline

1. In Azure Portal, use Run Command on each currently deployed VM. Record running API/web image references and scan `docker save` archives and the corresponding container logs without printing matched lines.
2. Scan host journal entries limited to the prior 30 days. Classify hits by comparing them privately to known image references and by inspecting only sanitized surrounding labels.
3. Scan database container logs with the same 30-day limit. Do not export database volumes or query application records for this review.
4. Download the GitHub Actions archives for runs 43–45 from the repository's Actions page and scan archives at bounded depth. Scan the full public Git history separately.
5. Record counts and classifications only; remove temporary scan directories and scanner image, then confirm VM health and disk use returned to baseline.

## Interpretation and limits

Within these inputs and time windows, every detected pattern was reviewed as a public integrity identifier or certificate fingerprint; the scans found no live credential. Together with [the current local scan](current-local-scan.txt) and [media review](../media-review/README.md), this closes the previously listed scans of the remote Git history, recent CI logs, deployed API/web images, and bounded Azure runtime logs.

Gitleaks is a pattern scanner; it cannot prove the absence of every opaque, encrypted, encoded, or unknown credential. The cloud logs cover the stated 30-day window, and the recent Actions archives are runs 43–45 plus the earlier ten-run set, not every workflow ever retained by GitHub. A human still needs to check the exact final submission bundle and avoid exposing live secrets during the oral defense. Do not describe this as a mathematical 100% guarantee.

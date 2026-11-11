#!/usr/bin/env bash
# Caller serializes deployments. Keep the active environment until the candidate passes.
set -euo pipefail
umask 077
if [[ $# != 3 ]]; then
  echo "Usage: apply-staging.sh deployment-directory active.env candidate.env" >&2
  exit 2
fi
release="$1/release.sh"
active="$2"
candidate="$3"
verify_candidate() {
  bash "$release" staging "$candidate" || return 1
  if [[ -n "${EXPECTED_RELEASE:-}" ]]; then
    local observed
    observed=$(curl -fsS --max-time 10 https://staging.mzcampuspulse.com/release.txt) || return 1
    [[ "$observed" == "$EXPECTED_RELEASE" ]] || return 1
  fi
}

if verify_candidate; then
  mv "$candidate" "$active"
  echo "Staging candidate accepted."
  exit 0
fi
echo "Candidate failed. Restoring the previous release." >&2
bash "$release" staging "$active"
echo "Previous staging release recovered." >&2
exit 1

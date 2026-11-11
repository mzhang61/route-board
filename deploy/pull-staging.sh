#!/usr/bin/env bash
# A root-owned timer fetches artifacts published for the current main commit.
set -euo pipefail
umask 077
settings=/etc/routeboard/staging
deployment=/opt/routeboard/releases/staging-oidc/deploy
repository=https://api.github.com/repos/mzhang61/route-board
exec 9>"$settings/deploy.lock"
flock -n 9 || exit 0
sha=$(curl --fail --silent --show-error "$repository/git/ref/heads/main" | jq -er '.object.sha')
[[ "$sha" =~ ^[0-9a-f]{40}$ ]]
if [[ -f "$settings/deployed-sha" && $(cat "$settings/deployed-sha") == "$sha" ]]; then exit 0; fi
temporary=$(mktemp -d "$settings/pull.XXXXXXXX")
trap 'rm -rf -- "$temporary"' EXIT
status=$(curl --silent --show-error -o "$temporary/release.json" -w '%{http_code}' "$repository/releases/tags/staging-$sha")
if [[ "$status" == 404 ]]; then exit 0; fi
[[ "$status" == 200 ]]
asset=$(jq -er '.assets[] | select(.name == "images.tar.gz")' "$temporary/release.json")
digest=$(jq -er '.digest' <<<"$asset")
[[ "$digest" =~ ^sha256:[a-f0-9]{64}$ ]]
url="https://github.com/mzhang61/route-board/releases/download/staging-$sha/images.tar.gz"
curl --fail --silent --show-error --location --retry 2 "$url" -o "$temporary/images.tar.gz"
echo "${digest#sha256:}  $temporary/images.tar.gz" | sha256sum --check --status
docker load -i "$temporary/images.tar.gz"
candidate="$temporary/candidate.env"
cp "$settings/release.env" "$candidate"
for service in api web; do
  image=$(docker image inspect --format '{{.Id}}' "routeboard-$service:$sha")
  [[ "$image" =~ ^sha256:[a-f0-9]{64}$ ]]
  sed -i "s/^${service^^}_IMAGE=.*/${service^^}_IMAGE=$image/" "$candidate"
  echo "$service $image"
done
EXPECTED_RELEASE="$sha" bash "$deployment/apply-staging.sh" "$deployment" "$settings/release.env" "$candidate"
echo "$sha" > "$settings/deployed-sha"
echo "Deployed $sha"

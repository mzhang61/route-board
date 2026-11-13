# Deployed database image review

Repository at collection: `fa3f1b6`. Staging and production reported the same Docker distribution manifest digest. Both had POSTGRES_PASSWORD_FILE set to /run/secrets/database_password, an empty or unset POSTGRES_PASSWORD, and a separate runtime password mount. The predicted result was four distinct current password fingerprints and zero exact matches in the saved image.

From each environment, inspect the running database container and save the output below in a private directory as staging.meta or production.meta. Run the second command twice per environment, substituting database_password and runtime_password for FILE. Save only the first output column in the matching staging.hashes or production.hashes file, one fingerprint per line.

```sh
docker inspect CONTAINER --format '{{.Image}} {{range .Mounts}}{{.Destination}} {{end}}'
docker exec CONTAINER sh -ec 'v=$(cat /run/secrets/FILE); test "${#v}" = 64; printf %s "$v" | sha256sum'
```

Export the shared image once using docker save IMAGE_ID piped through gzip into image.tar.gz in that private directory. With Python 3.11+ and Gitleaks 8.30.1:

```sh
python3 docs/evidence/database-image-review/check.py "$private_directory"
gitleaks dir --max-archive-depth=3 --redact=100 "$private_directory/layers"
```

[Results](results.txt) cover the verified config and all 15 build layers, including superseded files. The exact comparison checked lowercase 64-hex candidates in config bytes, layer entry metadata and regular file contents after outer layer decompression. Gitleaks supplied an independent rule scan. Neither test establishes absence of encoded, encrypted or historical passwords. The running database volume was excluded. No database records were accessed.

AI collected the evidence and corrected an initial failed assumption that the running image identifier would equal its config digest. The verifier checks the distribution manifest against both running IDs, then checks the config and layer digests against that manifest. Only public image identifiers, mount destinations and aggregate results are published.

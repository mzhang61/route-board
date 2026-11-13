"""Compare Docker image content with private SHA-256 fingerprints of 64-hex secrets."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import sys
import tarfile

root = Path(sys.argv[1])
fingerprints = set()
for environment in ("staging", "production"):
    values = (root / f"{environment}.hashes").read_text().splitlines()
    assert len(values) == 2 and all(re.fullmatch("[0-9a-f]{64}", v) for v in values)
    fingerprints.update(values)
    print(environment, (root / f"{environment}.meta").read_text().strip())

candidate = re.compile(rb"(?=([0-9a-f]{64}))")
matches = 0


def check(content):
    global matches
    for match in candidate.finditer(content):
        matches += hashlib.sha256(match.group(1)).hexdigest() in fingerprints


directory = root / "layers"
directory.mkdir(exist_ok=True)
with tarfile.open(root / "image.tar.gz") as image:
    manifest, = json.load(image.extractfile("manifest.json"))
    config = image.extractfile(manifest["Config"]).read()
    digest = "sha256:" + hashlib.sha256(config).hexdigest()
    descriptor, = json.load(image.extractfile("index.json"))["manifests"]
    manifest_digest = descriptor["digest"]
    raw = image.extractfile("blobs/" + manifest_digest.replace(":", "/")).read()
    assert "sha256:" + hashlib.sha256(raw).hexdigest() == manifest_digest
    verified = json.loads(raw)
    assert digest == verified["config"]["digest"]
    assert len(manifest["Layers"]) == len(verified["layers"])
    for environment in ("staging", "production"):
        assert manifest_digest == (root / f"{environment}.meta").read_text().split()[0]
    (directory / "config.json").write_bytes(config)
    check(config)
    files = 0
    for index, member in enumerate(manifest["Layers"]):
        path = directory / f"layer-{index}.tar"
        with path.open("wb") as destination:
            shutil.copyfileobj(image.extractfile(member), destination)
        with path.open("rb") as source:
            assert "sha256:" + hashlib.file_digest(source, "sha256").hexdigest() == verified["layers"][index]["digest"]
        with tarfile.open(path, "r|*") as layer:
            for item in layer:
                check(str(item.pax_headers).encode())
                check(item.name.encode())
                check(item.linkname.encode())
                if item.isfile():
                    check(layer.extractfile(item).read())
                    files += 1
print("Distinct current password fingerprints:", len(fingerprints))
print("Layers:", len(manifest["Layers"]), "regular file entries:", files)
print("Current password matches:", matches)
assert matches == 0

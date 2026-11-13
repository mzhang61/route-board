"""Inspect tracked submission media locally. Print locations and counts only."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import hashlib
import json
import os
import re
import subprocess
import tempfile
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[3]
os.chdir(ROOT)
patterns = {
    "email": r"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}",
    "JWT": r"eyJ[\w-]{10,}\.[\w-]{10,}\.[\w-]+",
    "credential": r"(?:Bearer\s+[\w.-]{12,}|(?:api[_ -]?key|password|access_token|refresh_token|client_secret|set-cookie)\s*[:=]\s*[^\s]{6,})",
}
files = [Path(name) for name in subprocess.check_output(["git", "ls-files", "docs/evidence"], text=True).splitlines()]
alerts = []


def inspect(label, text):
    for category, expression in patterns.items():
        if re.search(expression, text, re.I):
            alerts.append((label, category))


def ocr(path):
    return subprocess.check_output(["tesseract", str(path), "stdout"], stderr=subprocess.DEVNULL, text=True)


for path in files:
    if path.suffix == ".har":
        entries = json.loads(path.read_text())["log"]["entries"]
        allowed = {"response_type": "code", "scope": "openid trips.write", "code_challenge_method": "S256",
                   "audience": "https://staging.mzcampuspulse.com/api"}
        for entry in entries:
            req, res = entry["request"], entry["response"]
            url = urlsplit(req["url"])
            assert not (url.query or url.fragment or url.username or url.password)
            assert not any(obj[key] for obj in (req, res) for key in ("headers", "cookies"))
            assert "postData" not in req and "text" not in res["content"] and not res["redirectURL"]
            assert all(q["value"] in ("[redacted]", allowed.get(q["name"])) for q in req["queryString"])
        inspect(str(path), path.read_text())
        print(path, len(entries), "HAR entries checked")
    elif path.suffix == ".png":
        text = ocr(path)
        assert text.strip(), path
        inspect(str(path), text)
        print(path, "OCR checked")
    elif path.suffix == ".webm":
        with tempfile.TemporaryDirectory() as directory:
            subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-fps_mode", "passthrough",
                            str(Path(directory) / "%06d.png")], check=True)
            frames = sorted(Path(directory).glob("*.png"))
            assert frames
            unique = {}
            for frame in frames:
                unique.setdefault(hashlib.sha256(frame.read_bytes()).digest(), frame)
            with ThreadPoolExecutor(max_workers=4) as pool:
                texts = list(pool.map(ocr, unique.values()))
            assert any(text.strip() for text in texts), path
            for frame, text in zip(unique.values(), texts):
                inspect(f"{path} frame {frame.stem}", text)
            print(path, len(frames), "frames,", len(unique), "distinct images checked")
    elif path.suffix in (".txt", ".md", ".json") and any(s in path.parts for s in ("production-login", "staging-oidc", "staging-replay")):
        inspect(str(path), path.read_text())
        print(path, "text checked")

for location, category in alerts:
    print("Review:", location, category)
print("Pattern hits:", len(alerts))
raise SystemExit(bool(alerts))

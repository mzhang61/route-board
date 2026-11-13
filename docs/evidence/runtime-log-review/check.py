"""Check private Docker log captures. Emit only counts, timestamps and rule names."""
from pathlib import Path
import json
import re
import subprocess
import sys
import tempfile

root = Path(sys.argv[1])
rules = {
    "provider query credential": r"[?&](?:apikey|api_key|access_token)=[^&\s]+",
    "callback code": r"[?&]code=[^&\s]+",
    "JWT": r"eyJ[\w-]{8,}\.[\w-]+\.[\w-]+",
    "bearer credential": r"Bearer\s+[A-Za-z0-9_.-]{10,}",
    "invalid probe value": "RouteBoardInvalidLogProbe2026",
}
failed = False
for env in ("staging", "production"):
    for service in ("api", "web"):
        label = f"{env}-{service}"
        path = root / (label + ".log")
        lines = path.read_text().splitlines()
        if not lines:
            raise ValueError(f"Missing log coverage for {label}")
        print(label, "lines", len(lines), "from", lines[0].split()[0], "to", lines[-1].split()[0])
        for name, pattern in rules.items():
            hits = sum(bool(re.search(pattern, line, re.I)) for line in lines)
            print(name, hits)
            failed |= hits > 0
        with tempfile.TemporaryDirectory() as temp:
            report = Path(temp) / "result.json"
            result = subprocess.run(["gitleaks", "stdin", "--redact=100", "--report-format=json",
                                     "--report-path", str(report)], stdin=path.open("rb"),
                                    stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            if result.returncode not in (0, 1):
                raise RuntimeError(f"Scanner failed for {label}")
            hits = len(json.loads(report.read_text()))
            print("Gitleaks findings", hits)
            failed |= bool(hits) or result.returncode == 1
raise SystemExit(failed)

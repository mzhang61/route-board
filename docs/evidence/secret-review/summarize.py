"""Publish scanner metadata without including detected values or log contents."""
import json
import re
import sys
from collections import Counter
from pathlib import Path

root = Path(sys.argv[1])
for target in ["git", "logs", "staging-api", "staging-web", "production-api", "production-web"]:
    findings = json.loads((root / f"{target}.json").read_text())
    warnings = (root / f"{target}-summary").read_text()
    print(target, "rule hits:", len(findings), "error lines:", sum("ERR " in line for line in warnings.splitlines()))
    if (root / f"{target}.id").exists():
        image = (root / f"{target}.id").read_text().strip()
        assert re.fullmatch(r"sha256:[0-9a-f]{64}", image)
        print("Image:", image, "layers:", len(list((root / target).glob("layer-*.tar"))))
    if target == "git":
        for row in findings:
            print(row["Commit"][:12], row["File"], row["StartLine"], row["RuleID"])
    if target == "logs":
        counts = Counter()
        for row in findings:
            name = Path(row["File"]).name
            line = (root / "logs" / name).read_text().splitlines()[row["StartLine"] - 1]
            category = "image tag with commit SHA" if re.search(r"routeboard-(api|web):[0-9a-f]{40}", line) else "needs review"
            counts[category] += 1
        print(dict(counts))
        print("Workflow runs:", ", ".join((root / "run-ids").read_text().split()))

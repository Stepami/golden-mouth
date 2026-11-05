"""Collect toxicity and emotion scores on synthetic unsubscribe cases."""

import json
import urllib.request
from pathlib import Path


folder = Path(__file__).parent
cases = json.loads((folder / "issue-244-cause-horizon.json").read_text(encoding="utf-8"))
inputs = [case["text"] for case in cases]
for model in ("toxicity", "emotion"):
    request = urllib.request.Request(
        "http://127.0.0.1:7998/classify",
        json.dumps({"model": model, "input": inputs}, ensure_ascii=False).encode("utf-8"),
        {"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(request, timeout=120) as response:
        rows = json.load(response)["data"]
    for case, row in zip(cases, rows):
        case[model] = {item["label"]: item["score"] for item in row}

path = folder / "issue-244-four-model-classifiers.json"
path.write_text(json.dumps(cases, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(path)
for case in cases:
    toxic = case["toxicity"]
    emotion = case["emotion"]
    print(
        f"{case['id']:<15} {case['kind']:<6} "
        f"insult={toxic['insult']:.3f} threat={toxic['threat']:.3f} "
        f"sadness={emotion['sadness']:.3f} anger={emotion['anger']:.3f} "
        f"joy={emotion['joy']:.3f}"
    )

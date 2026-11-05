"""Short local CPU load probe using synthetic texts; not production capacity sizing."""

import argparse
import json
import statistics
import time
import urllib.request
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path


parser = argparse.ArgumentParser()
parser.add_argument("--base", required=True)
parser.add_argument("--model", required=True)
parser.add_argument("--separator", required=True)
parser.add_argument("--count", type=int, default=48)
args = parser.parse_args()
posts = json.loads((Path(__file__).parent / "issue-244-corpus.json").read_text(encoding="utf-8"))["posts"]
hypotheses = [
    "The audience's opinion does not matter.",
    "The author promises a result without showing a plan or evidence.",
    "This post is a joke.",
    "The author openly criticizes a decision and proposes a solution.",
]


def send(index):
    text = posts[index % len(posts)]["text"]
    inputs = [text + args.separator + hypothesis for hypothesis in hypotheses]
    request = urllib.request.Request(
        args.base + "/classify",
        json.dumps({"model": args.model, "input": inputs}, ensure_ascii=False).encode("utf-8"),
        {"Content-Type": "application/json"},
    )
    start = time.monotonic()
    with urllib.request.urlopen(request, timeout=120) as response:
        json.load(response)
    return (time.monotonic() - start) * 1000


send(0)
for concurrency in (1, 4, 8):
    started = time.monotonic()
    with ThreadPoolExecutor(max_workers=concurrency) as pool:
        times = sorted(pool.map(send, range(args.count)))
    elapsed = time.monotonic() - started
    print(
        f"concurrency={concurrency} requests={args.count} "
        f"rps={args.count / elapsed:.1f} "
        f"p50_ms={statistics.median(times):.0f} "
        f"p95_ms={times[int(len(times) * 0.95) - 1]:.0f} "
        f"max_ms={times[-1]:.0f}"
    )

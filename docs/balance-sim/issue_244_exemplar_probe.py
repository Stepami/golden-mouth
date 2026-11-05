"""Inspect leave-one-out exemplar similarities; diagnostic only, not game rules."""

import json
import urllib.request
from pathlib import Path


posts = json.loads((Path(__file__).parent / "issue-244-corpus.json").read_text(encoding="utf-8"))["posts"]
request = urllib.request.Request(
    "http://127.0.0.1:7997/embeddings",
    json.dumps({"model": "frida", "input": ["categorize: " + post["text"] for post in posts]}, ensure_ascii=False).encode("utf-8"),
    {"Content-Type": "application/json"},
)
with urllib.request.urlopen(request, timeout=120) as response:
    vectors = [item["embedding"] for item in json.load(response)["data"]]


def dot(first, second):
    return sum(a * b for a, b in zip(first, second))


for category in ("against_audience", "empty_promise", "humor", "off_topic"):
    positives = [index for index, post in enumerate(posts) if post["case"] == category]
    print(category)
    for index, post in enumerate(posts):
        others = [positive for positive in positives if positive != index]
        if not others:
            continue
        similarity = max(dot(vectors[index], vectors[positive]) for positive in others)
        print(f"  {post['id']:<3} {post['case']:<17} {similarity:.3f}")

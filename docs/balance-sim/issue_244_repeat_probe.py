"""Compare near-repeat pairs with other same-world post pairs."""

import argparse
import itertools
import json
import urllib.request
from pathlib import Path


parser = argparse.ArgumentParser()
parser.add_argument("--base", required=True)
parser.add_argument("--model", required=True)
parser.add_argument("--prefix", required=True)
args = parser.parse_args()
posts = json.loads((Path(__file__).parent / "issue-244-corpus.json").read_text(encoding="utf-8"))["posts"]
request = urllib.request.Request(
    args.base + "/embeddings",
    json.dumps({"model": args.model, "input": [args.prefix + post["text"] for post in posts]}, ensure_ascii=False).encode("utf-8"),
    {"Content-Type": "application/json"},
)
with urllib.request.urlopen(request, timeout=120) as response:
    vectors = [item["embedding"] for item in json.load(response)["data"]]


def dot(first, second):
    return sum(a * b for a, b in zip(first, second))


positives = {frozenset((post["id"], post["repeat_of"])) for post in posts if "repeat_of" in post}
same_world = [
    (frozenset((posts[i]["id"], posts[j]["id"])), dot(vectors[i], vectors[j]))
    for i, j in itertools.combinations(range(len(posts)), 2)
    if posts[i]["world"] == posts[j]["world"]
]
positive_scores = sorted(score for pair, score in same_world if pair in positives)
negative_scores = sorted((score for pair, score in same_world if pair not in positives), reverse=True)
print("positive", [round(score, 3) for score in positive_scores])
print("top_other", [round(score, 3) for score in negative_scores[:10]])

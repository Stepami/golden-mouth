"""Collect real Infinity outputs for the synthetic issue #244 corpus."""

import argparse
import json
import urllib.request
from pathlib import Path


parser = argparse.ArgumentParser()
parser.add_argument("--nli-model", default="minilm")
parser.add_argument("--base", default="http://127.0.0.1:7997")
parser.add_argument("--separator", default=" </s></s> ")
parser.add_argument("--output", default="issue-244-model-outputs.json")
args = parser.parse_args()

base = args.base
folder = Path(__file__).parent
corpus = json.loads((folder / "issue-244-corpus.json").read_text(encoding="utf-8"))


def post(path, body):
    request = urllib.request.Request(
        base + path,
        json.dumps(body, ensure_ascii=False).encode("utf-8"),
        {"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(request, timeout=120) as response:
        return json.load(response)


def dot(first, second):
    return sum(a * b for a, b in zip(first, second))


posts = corpus["posts"]
contexts = corpus["worlds"]
inputs = ["categorize: " + text for text in contexts.values()]
inputs += ["categorize: " + item["text"] for item in posts]
embeddings = [item["embedding"] for item in post("/embeddings", {"model": "frida", "input": inputs})["data"]]
context_vectors = dict(zip(contexts, embeddings[: len(contexts)]))
post_vectors = {item["id"]: vector for item, vector in zip(posts, embeddings[len(contexts) :])}

output = []
for item in posts:
    world = item["world"]
    hypotheses = {
        "against_audience": "The audience's opinion does not matter.",
        "empty_promise": "The author promises a result without showing a plan or evidence.",
        "topic": "Пост посвящён теме мира: " + contexts[world],
        "humor": "This post is a joke.",
        "brave": "The author openly criticizes a decision and proposes a solution.",
    }
    premise = item["text"]
    nli_input = [premise + args.separator + hypothesis for hypothesis in hypotheses.values()]
    rows = post("/classify", {"model": args.nli_model, "input": nli_input})["data"]
    scores = {
        key: {score["label"]: score["score"] for score in row}
        for key, row in zip(hypotheses, rows)
    }
    output.append(
        {
            "id": item["id"],
            "world": world,
            "case": item["case"],
            "topic_similarity": dot(post_vectors[item["id"]], context_vectors[world]),
            "repeat_similarity": (
                dot(post_vectors[item["id"]], post_vectors[item["repeat_of"]])
                if "repeat_of" in item
                else None
            ),
            "nli_scores": scores,
        }
    )

path = folder / args.output
path.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(path)

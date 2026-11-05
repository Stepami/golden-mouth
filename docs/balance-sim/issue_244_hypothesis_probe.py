"""Compare fixed NLI hypothesis phrasings on the synthetic corpus only."""

import json
import urllib.request
from pathlib import Path


posts = json.loads((Path(__file__).parent / "issue-244-corpus.json").read_text(encoding="utf-8"))["posts"]
hypotheses = {
    "empty_promise": [
        "The author makes an empty promise.",
        "The author promises something without evidence.",
        "The author guarantees success without a plan.",
        "The author promises to solve everything without explaining how.",
        "This text is an unsupported promise.",
        "Автор обещает результат, но не объясняет как.",
        "Это пустое обещание.",
    ],
    "humor": [
        "This is humorous.",
        "This text is funny.",
        "The author is joking.",
        "The post contains a joke.",
        "This is satire.",
        "Это шутка.",
        "Автор шутит.",
    ],
    "against_audience": [
        "The audience's opinion does not matter.",
        "The author rejects the audience's opinion.",
        "The author criticizes the audience rather than an idea.",
        "Мнение аудитории не имеет значения.",
    ],
}

for category, variants in hypotheses.items():
    inputs = [post["text"] + "<eos>" + variant for variant in variants for post in posts]
    request = urllib.request.Request(
        "http://127.0.0.1:7998/classify",
        json.dumps({"model": "horizon", "input": inputs}, ensure_ascii=False).encode("utf-8"),
        {"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(request, timeout=120) as response:
        scores = [next(item["score"] for item in row if item["label"] == "entailment") for row in json.load(response)["data"]]
    print(category)
    for index, variant in enumerate(variants):
        row = scores[index * len(posts) : (index + 1) * len(posts)]
        positive = [(post["id"], score) for post, score in zip(posts, row) if post["case"] == category]
        negative = sorted(
            [(post["id"], score) for post, score in zip(posts, row) if post["case"] != category],
            key=lambda item: item[1],
            reverse=True,
        )[:3]
        print(f"  {variant}: positive={positive} top_negative={negative}")

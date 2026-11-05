"""Run RuBERT NLI with real premise/hypothesis token type IDs, not Infinity strings."""

import json
from pathlib import Path

import torch
from optimum.onnxruntime import ORTModelForSequenceClassification
from transformers import AutoTokenizer


folder = Path(__file__).parent
model_path = "/models/rubert-nli"
tokenizer = AutoTokenizer.from_pretrained(model_path)
model = ORTModelForSequenceClassification.from_pretrained(
    model_path, file_name="onnx/model.onnx", provider="CPUExecutionProvider"
)


def score(pairs):
    scores = []
    for offset in range(0, len(pairs), 4):
        batch = pairs[offset : offset + 4]
        tokens = tokenizer(
            [premise for premise, _ in batch],
            [hypothesis for _, hypothesis in batch],
            padding=True,
            truncation=True,
            max_length=512,
            return_tensors="pt",
        )
        probabilities = torch.softmax(model(**tokens).logits, dim=-1).tolist()
        scores.extend(probability[0] for probability in probabilities)
    return scores


corpus = json.loads((folder / "issue-244-corpus.json").read_text(encoding="utf-8"))
posts = corpus["posts"]
hypotheses = {
    "against_audience": "The audience's opinion does not matter.",
    "empty_promise": "The author promises a result without showing a plan or evidence.",
    "humor": "This post is a joke.",
    "brave": "The author openly criticizes a decision and proposes a solution.",
}
pairs = [(post["text"], hypothesis) for post in posts for hypothesis in hypotheses.values()]
values = score(pairs)
results = []
for index, post in enumerate(posts):
    result = {
        "id": post["id"],
        "world": post["world"],
        "case": post["case"],
        "entailment": dict(zip(hypotheses, values[index * len(hypotheses) : (index + 1) * len(hypotheses)])),
    }
    results.append(result)

(folder / "issue-244-rubert-paired-outputs.json").write_text(
    json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
)
for result in results:
    print(result["id"], result["case"], {key: round(value, 3) for key, value in result["entailment"].items()})

causes = json.loads((folder / "issue-244-cause-horizon.json").read_text(encoding="utf-8"))
cause_hypotheses = {
    "call_to_action": "The author urges the audience to buy, join, or act now.",
    "vague_benefit": "The author promises a big benefit without explaining how it will happen.",
    "self_pity": "The author complains about their own problems and feels sorry for themselves.",
    "constructive": "The author offers a concrete next step or asks for help.",
}
pairs = [(case["text"], hypothesis) for case in causes for hypothesis in cause_hypotheses.values()]
values = score(pairs)
cause_results = []
for index, case in enumerate(causes):
    result = {
        "id": case["id"],
        "kind": case["kind"],
        "entailment": dict(zip(cause_hypotheses, values[index * len(cause_hypotheses) : (index + 1) * len(cause_hypotheses)])),
    }
    cause_results.append(result)

(folder / "issue-244-rubert-paired-causes.json").write_text(
    json.dumps(cause_results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
)
for result in cause_results:
    print(result["id"], result["kind"], {key: round(value, 3) for key, value in result["entailment"].items()})

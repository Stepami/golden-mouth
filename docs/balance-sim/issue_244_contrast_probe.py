"""Offline CPU pilot for the selected four models; not a runtime rule.

Calibrate only on dev, then evaluate the selected candidate once on holdout.
Run in the pinned Infinity image with /models read-only and this folder /work.
"""

import argparse
import json
import re
import time
from pathlib import Path

import numpy as np
import onnxruntime as ort
import torch
from transformers import AutoModel, AutoModelForSequenceClassification, AutoTokenizer

from issue_244_contrast_corpus import TOPICS, cases
from issue_244_selected_causes import OTHER_TOPICS, cases as challenge_cases

torch.set_num_threads(2)
torch.set_num_interop_threads(1)
folder = Path(__file__).parent
parser = argparse.ArgumentParser()
parser.add_argument("--challenge", action="store_true")
args = parser.parse_args()
reference_rows = cases()
rows = challenge_cases() if args.challenge else reference_rows


def own_text(text):
    # Explicit quotations only. Unmarked reported speech is not resolved.
    return re.sub(r'«[^»]*»|"[^"\n]*"', ' ', text) if args.challenge else text
start = time.perf_counter()
hypotheses = {
    "insult": ("Автор оскорбляет других людей.", "Автор обсуждает проблему без оскорблений."),
    "promotion": ("Автор предлагает купить свой товар или платную услугу.", "Автор рассказывает о товаре, но не предлагает его купить."),
    "complaint": ("Автор жалуется на свою жизнь и жалеет себя.", "Автор сообщает о проблеме и предлагает решение."),
}


def classify(name):
    path = f"/models/{name}"
    tokenizer = AutoTokenizer.from_pretrained(path, local_files_only=True)
    model = AutoModelForSequenceClassification.from_pretrained(path, local_files_only=True).eval()
    with torch.inference_mode():
        for offset in range(0, len(rows), 4):
            batch = rows[offset:offset + 4]
            tokens = tokenizer([own_text(row["text"]) for row in batch], padding=True,
                               truncation=True, max_length=512, return_tensors="pt")
            probs = model(**tokens).logits.sigmoid().tolist()
            for row, values in zip(batch, probs):
                row[name] = {model.config.id2label[i]: p for i, p in enumerate(values)}


classify("toxicity")
classify("emotion")
tokenizer = AutoTokenizer.from_pretrained("/models/embedding", local_files_only=True)
model = AutoModel.from_pretrained("/models/embedding", local_files_only=True).eval()
texts = list(dict.fromkeys([r["text"] for r in rows] + [r["previous"] for r in rows if r["previous"]]
                          + [own_text(r["text"]) for r in rows + reference_rows]
                          + [t for ts in TOPICS.values() for t in ts] + OTHER_TOPICS))
vectors = {}
with torch.inference_mode():
    for offset in range(0, len(texts), 4):
        batch = texts[offset:offset + 4]
        tokens = tokenizer(batch, padding=True, truncation=True, max_length=512, return_tensors="pt")
        embeddings = torch.nn.functional.normalize(model(**tokens).last_hidden_state[:, 0], dim=-1)
        vectors.update(zip(batch, embeddings.numpy()))
del model

tokenizer = AutoTokenizer.from_pretrained("/models/nli", local_files_only=True)
options = ort.SessionOptions()
options.intra_op_num_threads = 1
options.inter_op_num_threads = 1
session = ort.InferenceSession("/models/nli/onnx/model.onnx", sess_options=options,
                              providers=["CPUExecutionProvider"])
nli_config = json.loads(Path("/models/nli/config.json").read_text())
entail = next(int(k) for k, v in nli_config["id2label"].items() if v == "entailment")


def nli(pairs):
    result = []
    for offset in range(0, len(pairs), 4):
        batch = pairs[offset:offset + 4]
        tokens = tokenizer([x[0] for x in batch], [x[1] for x in batch], padding=True,
                           truncation=True, max_length=512, return_tensors="np")
        logits = session.run(None, dict(tokens))[0]
        probabilities = np.exp(logits - logits.max(axis=-1, keepdims=True))
        probabilities /= probabilities.sum(axis=-1, keepdims=True)
        result.extend(float(v) for v in probabilities[:, entail])
    return result


for category, pair in hypotheses.items():
    scores = nli([(r["text"], h) for r in rows for h in pair])
    for i, row in enumerate(rows):
        row.setdefault("nli", {})[category] = dict(positive=scores[2*i], negative=scores[2*i+1])

for row in rows:
    vector = vectors[row["text"]]
    row["topic_similarity"] = max(float(vector @ vectors[t]) for t in TOPICS[row["world"]])
    row["other_topic_similarity"] = max(float(vector @ vectors[t]) for t in OTHER_TOPICS)
    row["topic_entailment"] = max(nli([(row["text"], t) for t in TOPICS[row["world"]]]))
    if row["previous"]:
        row["repeat_similarity"] = float(vector @ vectors[row["previous"]])
        row["repeat_entailment"] = min(nli([(row["text"], row["previous"]), (row["previous"], row["text"])]))
    for category in hypotheses:
        reference = [r for r in reference_rows if r["split"] == "dev" and r["category"] == category and r["family"] != row["family"]]
        own_vector = vectors[own_text(row["text"])]
        positive = max(float(own_vector @ vectors[own_text(r["text"])]) for r in reference if r["expected"])
        negative = max(float(own_vector @ vectors[own_text(r["text"])]) for r in reference if not r["expected"])
        row.setdefault("example_margin", {})[category] = positive - negative

if args.challenge:
    report = dict(protocol="32 new synthetic challenge texts; fixed rules before inference; old 80 texts are exploratory",
                  hypotheses=hypotheses, topics=TOPICS, other_topics=OTHER_TOPICS, rules={
                      "angry_abuse": "min(insult, anger) >= 0.5 on text without explicit quotations",
                      "promotion": "positive-negative example cosine margin >= 0.1 on text without explicit quotations",
                      "near_copy": "cosine >= 0.9 AND mutual NLI >= 0.8 AND equal ordered numeric tokens",
                      "topic_switch": "max(other topic cosine) - max(world topic cosine) >= 0.15"}, metrics={})
    for row in rows:
        row["decisions"] = dict(
            angry_abuse=min(row["toxicity"]["insult"], row["emotion"]["anger"]) >= 0.5,
            promotion=row["example_margin"]["promotion"] >= 0.1,
            near_copy=bool(row["previous"] and row["repeat_similarity"] >= 0.9
                           and row["repeat_entailment"] >= 0.8
                           and re.findall(r"\d+", row["text"]) == re.findall(r"\d+", row["previous"])),
            topic_switch=row["other_topic_similarity"] - row["topic_similarity"] >= 0.15)
    for category in report["rules"]:
        # All 32 cases have a known world; unrelated-to-this-cause cases are negatives.
        # Repeat is conditional on the supplied previous post (8 cases).
        sample = [r for r in rows if r["previous"]] if category == "near_copy" else rows
        tp = fp = fn = tn = 0
        mistakes = []
        for row in sample:
            label = row["category"] == category and row["expected"]
            predicted = row["decisions"][category]
            tp += int(predicted and label)
            fp += int(predicted and not label)
            fn += int(not predicted and label)
            tn += int(not predicted and not label)
            if label != predicted:
                mistakes.append(row["id"])
        report["metrics"][category] = dict(tp=tp, fp=fp, fn=fn, tn=tn, mistakes=mistakes)
        print(category, report["metrics"][category], flush=True)
    report["elapsed_seconds"] = time.perf_counter() - start
    (folder / "issue-244-selected-causes-results.json").write_text(json.dumps(dict(report=report, cases=rows), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("elapsed_seconds", round(report["elapsed_seconds"], 2), flush=True)
    raise SystemExit()


def features(row, category):
    if category == "repeat":
        return {"cosine": row["repeat_similarity"], "mutual_nli": row["repeat_entailment"],
                "cosine_x_nli": row["repeat_similarity"] * row["repeat_entailment"]}
    if category == "off_topic":
        return {"negative_cosine": -row["topic_similarity"], "negative_nli": -row["topic_entailment"],
                "negative_max": -max(row["topic_similarity"], row["topic_entailment"])}
    p = row["nli"][category]
    scores = {"nli": p["positive"], "nli_margin": p["positive"] - p["negative"],
              "example_margin": row["example_margin"][category]}
    if category == "insult":
        scores.update(insult=row["toxicity"]["insult"],
                      insult_x_nli=row["toxicity"]["insult"] * p["positive"])
    if category == "complaint":
        scores.update(sadness=row["emotion"]["sadness"],
                      sadness_x_nli=row["emotion"]["sadness"] * p["positive"])
    return scores


def metrics(sample, category, feature, threshold):
    tp = fp = fn = tn = 0
    mistakes = []
    for row in sample:
        label = row["category"] == category and row["expected"]
        predicted = features(row, category)[feature] >= threshold
        tp += int(predicted and label)
        fp += int(predicted and not label)
        fn += int(not predicted and label)
        tn += int(not predicted and not label)
        if label != predicted:
            mistakes.append(row["id"])
    return dict(tp=tp, fp=fp, fn=fn, tn=tn, mistakes=mistakes)


report = dict(protocol="80 synthetic texts, dev/holdout split by contrast family; zero-FP threshold on dev; no holdout tuning",
              hypotheses=hypotheses, topics=TOPICS, models={
                  name: json.loads(Path(f"/models/{name}/config.json").read_text()).get("_name_or_path")
                  for name in ("embedding", "nli", "toxicity", "emotion")}, categories={})
for category in (*hypotheses, "repeat", "off_topic"):
    # Topic labels exist only for topic pairs, repeat requires a supplied history.
    sample = rows if category in hypotheses else [r for r in rows if r["category"] == category]
    dev = [r for r in sample if r["split"] == "dev"]
    holdout = [r for r in sample if r["split"] == "holdout"]
    candidates = []
    for feature in features(dev[0], category):
        negatives = [features(r, category)[feature] for r in dev if not (r["category"] == category and r["expected"])]
        threshold = max(negatives) + 1e-6
        candidates.append(dict(feature=feature, threshold=threshold,
                               dev=metrics(dev, category, feature, threshold)))
    # Stable tie order: prefer simpler/native formula listed first. Never use holdout to select.
    winner = max(candidates, key=lambda c: c["dev"]["tp"])
    winner["holdout"] = metrics(holdout, category, winner["feature"], winner["threshold"])
    report["categories"][category] = dict(selected=winner, dev_candidates=candidates)
    print(category, json.dumps(winner, ensure_ascii=False), flush=True)

report["elapsed_seconds"] = time.perf_counter() - start
(folder / "issue-244-contrast-results.json").write_text(json.dumps(dict(report=report, cases=rows), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("elapsed_seconds", round(report["elapsed_seconds"], 2), flush=True)

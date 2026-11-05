"""Emotion-driven audience reactions: select threshold on dev only, then holdout.

Sad support requests and frightening stories can be positives by product choice.
This does not detect whining, manipulation, pessimism, or truth of a statement.
"""
import json
from pathlib import Path

import torch
from transformers import AutoModelForSequenceClassification, AutoTokenizer
from issue_244_sadness_corpus import CASES as SADNESS
from issue_244_fear_corpus import CASES as FEAR

torch.set_num_threads(2)
torch.set_num_interop_threads(1)
rows = SADNESS + FEAR
path = '/models/emotion'
tokenizer = AutoTokenizer.from_pretrained(path, local_files_only=True)
model = AutoModelForSequenceClassification.from_pretrained(path, local_files_only=True).eval()
with torch.inference_mode():
    for offset in range(0, len(rows), 4):
        batch = rows[offset:offset + 4]
        tokens = tokenizer([r['text'] for r in batch], padding=True, truncation=True,
                           max_length=512, return_tensors='pt')
        probabilities = model(**tokens).logits.sigmoid().tolist()
        for row, p in zip(batch, probabilities):
            row['emotion'] = {model.config.id2label[i]: value for i, value in enumerate(p)}

report = dict(protocol='Independent author-written synthetic pairs; threshold chosen only on dev; each emotion evaluated on its labelled cases, not treating other emotions as negatives.',
              threshold_policy='max(0.5, largest negative dev score + 0.001)', categories={})
for cause in ('sadness', 'fear'):
    sample = [r for r in rows if r['cause'] == cause]
    threshold = max(.5, max(r['emotion'][cause] for r in sample if r['split'] == 'dev' and not r['expected']) + .001)
    stats = dict(threshold=threshold)
    for split in ('dev', 'challenge'):
        tp = fp = fn = tn = 0
        mistakes = []
        for row in (r for r in sample if r['split'] == split):
            pred = row['emotion'][cause] >= threshold
            label = row['expected']
            tp += int(pred and label)
            fp += int(pred and not label)
            fn += int(not pred and label)
            tn += int(not pred and not label)
            if pred != label:
                mistakes.append(row['id'])
        stats[split] = dict(tp=tp, fp=fp, fn=fn, tn=tn, mistakes=mistakes)
    report['categories'][cause] = stats
    print(cause, stats)
folder = Path(__file__).parent
(folder / 'issue-244-emotion-causes-results.json').write_text(json.dumps(dict(report=report, cases=rows), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
for row in rows:
    print(row['id'], row['split'], row['expected'], round(row['emotion'][row['cause']], 3))

"""Fixed-rule exploratory pilot, not calibrated gameplay thresholds."""
import argparse
import json
import re
from pathlib import Path

import numpy as np
import onnxruntime as ort
import torch
from transformers import AutoModelForSequenceClassification, AutoTokenizer

from issue_244_more_causes_corpus import cases
from issue_244_obscenity_corpus import CASES

torch.set_num_threads(2)
parser = argparse.ArgumentParser()
parser.add_argument('--literal', action='store_true')
args = parser.parse_args()
folder = Path(__file__).parent
rows = cases + CASES
# Prior challenge remains a regression set, never an independent new holdout.
old = json.loads((folder / 'issue-244-selected-causes-results.json').read_text(encoding='utf-8'))['cases']
# Only closure is labelled absent in old cases. Old profanity is not a negative.
rows += [dict(id='regression_' + r['id'], cause='regression', expected=False, text=r['text']) for r in old]

def own(text):
    return re.sub(r'«[^»]*»|“[^”]*”|"[^"\n]*"', ' ', text)

tokenizer = AutoTokenizer.from_pretrained('/models/toxicity', local_files_only=True)
model = AutoModelForSequenceClassification.from_pretrained('/models/toxicity', local_files_only=True).eval()
with torch.inference_mode():
    for row in rows:
        tokens = tokenizer(own(row['text']), return_tensors='pt', truncation=True, max_length=512)
        p = model(**tokens).logits.sigmoid()[0].tolist()
        row['toxicity'] = {model.config.id2label[i]: value for i, value in enumerate(p)}
del model

tokenizer = AutoTokenizer.from_pretrained('/models/nli', local_files_only=True)
options = ort.SessionOptions()
options.intra_op_num_threads = 1
options.inter_op_num_threads = 1
session = ort.InferenceSession('/models/nli/onnx/model.onnx', sess_options=options, providers=['CPUExecutionProvider'])
config = json.loads(Path('/models/nli/config.json').read_text())
index = next(int(k) for k, v in config['id2label'].items() if v == 'entailment')
hypotheses = ['Автор больше не будет публиковать посты в этом канале.',
              'Автор окончательно прекращает вести этот канал.',
              'Автор продолжит публиковать посты в этом канале.']
if args.literal:
    hypotheses = ['В этом канале больше не будет новых постов.',
                  'Я больше не буду вести этот канал.',
                  'Автор продолжит публиковать посты в этом канале.']
for row in rows:
    tokens = tokenizer([own(row['text'])] * 3, hypotheses, return_tensors='np', padding=True, truncation=True, max_length=512)
    logits = session.run(None, dict(tokens))[0]
    probs = np.exp(logits - logits.max(axis=-1, keepdims=True))
    probs /= probs.sum(axis=-1, keepdims=True)
    row['nli'] = probs[:, index].tolist()
    row['closure'] = min(row['nli'][:2]) >= .75 and min(row['nli'][:2]) - row['nli'][2] >= .5
    row['obscenity'] = row['toxicity']['obscenity'] >= .8

report = dict(protocol='Exploratory only; formulas fixed before inference; no threshold tuning',
              hypotheses=hypotheses, rules=dict(channel_closure='min(two positives)>=0.75 and margin over continuation>=0.5',
                                              obscenity='obscenity>=0.8 on own text'), metrics={})
for cause, decision in [('channel_closure', 'closure'), ('obscenity', 'obscenity')]:
    sample = rows if cause == 'channel_closure' else [r for r in rows if r['cause'] == cause]
    tp = fp = fn = tn = 0
    mistakes = []
    for row in sample:
        label = row['cause'] == cause and row['expected']
        pred = row[decision]
        tp += int(pred and label)
        fp += int(pred and not label)
        fn += int(not pred and label)
        tn += int(not pred and not label)
        if label != pred:
            mistakes.append(row['id'])
    report['metrics'][cause] = dict(tp=tp, fp=fp, fn=fn, tn=tn, mistakes=mistakes)
    print(cause, report['metrics'][cause])
(folder / ('issue-244-more-causes-literal-results.json' if args.literal else 'issue-244-more-causes-results.json')).write_text(json.dumps(dict(report=report, cases=rows), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
for row in rows[:24]:
    print(row['id'], [round(x, 3) for x in row['nli']], {k: round(v, 3) for k, v in row['toxicity'].items()})

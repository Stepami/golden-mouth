"""Export the pinned tiny BERT NLI model with token_type_ids for .NET ONNX Runtime."""

from pathlib import Path

import numpy as np
import onnxruntime as ort
import torch
from transformers import AutoModelForSequenceClassification, AutoTokenizer


root = Path("/models/nli")
model = AutoModelForSequenceClassification.from_pretrained(root).eval()
tokenizer = AutoTokenizer.from_pretrained(root)


class Export(torch.nn.Module):
    def __init__(self, wrapped):
        super().__init__()
        self.wrapped = wrapped

    def forward(self, input_ids, attention_mask, token_type_ids):
        return self.wrapped(
            input_ids=input_ids,
            attention_mask=attention_mask,
            token_type_ids=token_type_ids,
        ).logits


sample = tokenizer("Сегодня идёт дождь", "Сегодня нет дождя", return_tensors="pt")
output = root / "onnx" / "model.onnx"
output.parent.mkdir(exist_ok=True)
torch.onnx.export(
    Export(model),
    (sample["input_ids"], sample["attention_mask"], sample["token_type_ids"]),
    str(output),
    input_names=["input_ids", "attention_mask", "token_type_ids"],
    output_names=["logits"],
    dynamic_axes={
        "input_ids": {0: "batch", 1: "sequence"},
        "attention_mask": {0: "batch", 1: "sequence"},
        "token_type_ids": {0: "batch", 1: "sequence"},
        "logits": {0: "batch"},
    },
    opset_version=17,
    dynamo=False,
)

reference = AutoModelForSequenceClassification.from_pretrained(root).eval()(**sample).logits.detach().numpy()
actual = ort.InferenceSession(str(output), providers=["CPUExecutionProvider"]).run(
    None, {key: value.numpy() for key, value in sample.items()}
)[0]
if np.max(np.abs(reference - actual)) > 1e-4:
    raise RuntimeError("The exported NLI model differs from the source checkpoint.")

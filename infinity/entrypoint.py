"""Prepare the backend NLI artifact in the shared volume, then start Infinity."""
import os
import sys
from pathlib import Path

root = Path(os.environ["HF_HOME"]) / "nli"
revision = "3907306f5e5d65e281a524c536883b766c89ded2"
output = root / "onnx" / "model.onnx"
marker = output.with_suffix(".revision")

if not output.exists() or not (root / "vocab.txt").exists() or not marker.exists() or marker.read_text() != revision:
    from huggingface_hub import snapshot_download

    snapshot_download("cointegrated/rubert-tiny-bilingual-nli", revision=revision,
                      local_dir=root, allow_patterns=["*.json", "*.txt", "*.safetensors"])
    import numpy as np
    import onnxruntime as ort
    import torch
    from transformers import AutoModelForSequenceClassification, AutoTokenizer


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
    output.parent.mkdir(parents=True, exist_ok=True)
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
    marker.write_text(revision)
    print("NLI ONNX prepared in shared volume", flush=True)
else:
    print("Using cached NLI ONNX", flush=True)

os.execvp("infinity_emb", ["infinity_emb", *sys.argv[1:]])

"""Prepare only the paired NLI checkpoint for the backend; Infinity downloads its own models."""

import subprocess
from pathlib import Path

from huggingface_hub import snapshot_download


root = Path(__file__).parent
models = root / "models"
nli_revision = "3907306f5e5d65e281a524c536883b766c89ded2"

snapshot_download(
    "cointegrated/rubert-tiny-bilingual-nli",
    revision=nli_revision,
    local_dir=models / "nli",
    allow_patterns=["*.json", "*.txt", "*.safetensors"],
)
onnx = models / "nli" / "onnx" / "model.onnx"
marker = onnx.with_suffix(".revision")
if not onnx.exists() or not marker.exists() or marker.read_text(encoding="utf-8") != nli_revision:
    subprocess.run(
        [
            "docker", "run", "--rm", "--network", "none",
            "--volume", f"{models.resolve()}:/models",
            "--volume", f"{(root / 'export_nli.py').resolve()}:/export_nli.py:ro",
            "--entrypoint", "python", "michaelf34/infinity:0.0.78-cpu", "/export_nli.py",
        ],
        check=True,
    )
    marker.write_text(nli_revision, encoding="utf-8")

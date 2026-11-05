"""Download pinned public checkpoints and export paired NLI ONNX before Compose."""

import subprocess
from pathlib import Path

from huggingface_hub import snapshot_download


root = Path(__file__).parent
models = root / "models"
nli_revision = "3907306f5e5d65e281a524c536883b766c89ded2"

snapshot_download(
    "cointegrated/rubert-tiny2",
    revision="e8ed3b0c8bbf4fb6984c3de043bf7d2f4e5969ae",
    local_dir=models / "embedding",
    allow_patterns=["*.json", "*.txt", "*.safetensors"],
)

snapshot_download(
    "cointegrated/rubert-tiny-bilingual-nli",
    revision=nli_revision,
    local_dir=models / "nli",
    allow_patterns=["*.json", "*.txt", "*.safetensors"],
)
snapshot_download(
    "cointegrated/rubert-tiny-toxicity",
    revision="5d37eff844868e243467e4c38898bad46c271af2",
    local_dir=models / "toxicity",
    allow_patterns=["*.json", "*.txt", "*.safetensors"],
)
snapshot_download(
    "cointegrated/rubert-tiny2-cedr-emotion-detection",
    revision="453ae93ca895c98cda29522c72b6fbc5a08067b9",
    local_dir=models / "emotion",
    allow_patterns=["*.json", "*.txt", "*.safetensors"],
)

onnx = models / "nli" / "onnx" / "model.onnx"
marker = onnx.with_suffix(".revision")
if not onnx.exists() or not marker.exists() or marker.read_text(encoding="utf-8") != nli_revision:
    subprocess.run(
        [
            "docker",
            "run",
            "--rm",
            "--network",
            "none",
            "--volume",
            f"{models.resolve()}:/models",
            "--volume",
            f"{(root / 'export_nli.py').resolve()}:/export_nli.py:ro",
            "--entrypoint",
            "python",
            "michaelf34/infinity:0.0.78-cpu",
            "/export_nli.py",
        ],
        check=True,
    )
    marker.write_text(nli_revision, encoding="utf-8")

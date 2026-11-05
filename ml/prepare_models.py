"""Download the two public, pinned model artifacts before starting Compose."""

from pathlib import Path

from huggingface_hub import snapshot_download


models = Path(__file__).parent / "models"

snapshot_download(
    "sergeyzh/rubert-mini-frida",
    revision="32a82388a9f99c99eca74494731f7ee6e9a9fe3a",
    local_dir=models / "frida",
    allow_patterns=["*.json", "*.txt", "*.safetensors"],
)

snapshot_download(
    "Horizon-Labs/multilingual-zeroshot-small",
    revision="17af5f8a728d101ce33a9bffad227b0cd005e95e",
    local_dir=models / "horizon",
    allow_patterns=[
        "config.json",
        "special_tokens_map.json",
        "tokenizer.json",
        "tokenizer_config.json",
        "onnx/model_quantized.onnx",
    ],
)

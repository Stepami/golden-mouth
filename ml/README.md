# Локальные модели для Infinity v2

Обе модели публичные, токен Hugging Face для загрузки не нужен. До запуска Compose установите `huggingface_hub==0.36.0` и выполните `python ml/prepare_models.py` из корня репозитория. Скрипт скачивает файлы в игнорируемую Git папку `ml/models/`. Infinity запускается с `HF_HUB_OFFLINE=1` и использует только эти локальные файлы.

- `frida`: исходная [mini-frida](https://huggingface.co/sergeyzh/rubert-mini-frida) (MIT), Safetensors, mean pooling и L2-нормализация. На вход передаётся `categorize: ` ровно один раз. Infinity вызывает токенизатор напрямую и не применяет prompt из `SentenceTransformer.encode()` автоматически.
- `horizon`: [Horizon-Labs/multilingual-zeroshot-small](https://huggingface.co/Horizon-Labs/multilingual-zeroshot-small) (Apache-2.0), INT8 ONNX. Для NLI вход `/classify` — строка `premise + "<eos>" + hypothesis`; на проверенных парах она дала те же токены, что `tokenizer(premise, hypothesis)`. Ответ содержит `entailment` и `not_entailment`. Проверенные игровые гипотезы приведены в [исследовании](../docs/ml-model-research-244.md); новые пороги отписок пока не включены.

Перед использованием для игрового баланса запустите сравнение с эталонными моделями на синтетических постах и измерьте задержку на целевой машине. Локальная проверка 05.11.2026 описана в [протоколе](../docs/balance-sim/issue-244-preflight.md).

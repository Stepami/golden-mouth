# Локальные модели для Infinity v2

Все четыре модели публичные, токен Hugging Face для загрузки не нужен. До запуска Compose установите `huggingface_hub==0.36.0` и выполните `python ml/prepare_models.py` из корня репозитория. Скрипт скачивает закреплённые Safetensors в игнорируемую Git папку `ml/models/` и офлайн экспортирует парную NLI-модель в ONNX через закреплённый Docker-образ Infinity. Игровой запрос ничего не скачивает.

- `embedding`: [cointegrated/rubert-tiny2](https://huggingface.co/cointegrated/rubert-tiny2) (MIT), исходный Safetensors, CLS pooling, L2-нормализация, 312 измерений. Префикс не нужен. Infinity обслуживает `/embeddings`.
- `nli`: [cointegrated/rubert-tiny-bilingual-nli](https://huggingface.co/cointegrated/rubert-tiny-bilingual-nli), две метки. Внутри backend `Microsoft.ML.Tokenizers` строит `input_ids` **и** `token_type_ids` для пары, `Microsoft.ML.OnnxRuntime` исполняет локальный `onnx/model.onnx`. Строковый Infinity `/classify` для этого checkpoint не годится: он выдаёт нулевой сегмент гипотезе.
- `toxicity`: [cointegrated/rubert-tiny-toxicity](https://huggingface.co/cointegrated/rubert-tiny-toxicity) (MIT). Infinity `/classify` возвращает независимые метки `non-toxic`, `insult`, `obscenity`, `threat`, `dangerous`.
- `emotion`: [cointegrated/rubert-tiny2-cedr-emotion-detection](https://huggingface.co/cointegrated/rubert-tiny2-cedr-emotion-detection). Infinity `/classify` возвращает независимые метки `no_emotion`, `joy`, `sadness`, `surprise`, `fear`, `anger`.

В карточках `nli` и `emotion` лицензия не указана; выбранный набор внедрён по прямому решению владельца проекта, условия использования для выпуска нужно уточнить. Никакая из меток пока не является самостоятельным правилом отписки: [исследование](../docs/ml-model-research-244.md) показывает пересечения на игровых примерах. Текущий `balance.json` не менялся.

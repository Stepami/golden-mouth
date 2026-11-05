# Локальные модели

Установить `huggingface_hub==0.36.0`, выполнить `python ml/prepare_models.py` из корня репозитория. Нужен работающий Docker: скрипт скачивает четыре публичных checkpoint и экспортирует NLI в ONNX. Токен Hugging Face не требуется. Веса остаются в игнорируемом `ml/models/`.

Один Infinity v2 обслуживает rubert-tiny2 (`/embeddings`, модель `embedding`), toxicity и CEDR emotion (`/classify`, модели `toxicity`/`emotion`). NLI исполняется в существующем backend: строковый `/classify` Infinity не передаёт необходимые парные token_type_ids. Отдельного Python-сервиса нет.

Для Compose дополнительно задать `MONGO_SEARCH_PASSWORD` в окружении. MongoDB Search требует `mongot`; одноразовый bootstrap создаёт служебного пользователя и файл пароля в Docker volume. Это настройка инфраструктуры, не миграция документов. ML и Search не имеют внешних портов. Остальные переменные и запуск приложения описаны в корневом README.

Вне Compose задать `Embedding:BaseUrl` для доступного Infinity. Локальный NLI включается через `Nli:Enabled`, `Nli:ModelPath` и `Nli:VocabPath`; по умолчанию выключен для запуска без весов.

Клиенты только возвращают scores/векторы; публикация пока ими не пользуется. [Спецификация #245](../docs/ml-scoring.md), [новый баланс](../docs/config/balance-new.json). Действующий баланс не меняется. Индексы, поля документов, миграция и backfill в PR не входят.

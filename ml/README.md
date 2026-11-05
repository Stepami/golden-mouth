# Локальные модели

Infinity сам скачивает три модели по Hugging Face ID при первом запуске. Кэш хранится в Docker volume `infinity-models` и используется при следующих запусках. Для первой загрузки нужен интернет; токен Hugging Face не требуется.

Папка `ml/` нужна только для локального NLI в backend: установить `huggingface_hub==0.36.0`, выполнить `python ml/prepare_nli.py` при работающем Docker. Скрипт скачивает NLI и экспортирует его в ONNX; файлы остаются в игнорируемом `ml/models/nli/`.

Один Infinity v2 обслуживает rubert-tiny2 (`/embeddings`, модель `embedding`), toxicity и CEDR emotion (`/classify`, модели `toxicity`/`emotion`). NLI исполняется в существующем backend: строковый `/classify` Infinity не передаёт необходимые парные token_type_ids. Отдельного Python-сервиса нет.

Для Compose задать `MONGO_SEARCH_PASSWORD` в окружении. MongoDB Search требует отдельный процесс `mongot`. Хук `mongo.post_start` создаёт служебного пользователя и файл пароля в Linux volume; отдельного bootstrap-контейнера нет. Нужен Compose 2.30+. Это настройка инфраструктуры, не миграция документов. ML и Search не имеют внешних портов. Остальные переменные и запуск приложения описаны в корневом README.

Вне Compose задать `Embedding:BaseUrl` для доступного Infinity. Локальный NLI включается через `Nli:Enabled`, `Nli:ModelPath` и `Nli:VocabPath`; по умолчанию выключен для запуска без весов.

Клиенты только возвращают scores/векторы; публикация пока ими не пользуется. [Спецификация #245](../docs/ml-scoring.md), [новый баланс](../docs/config/balance-new.json). Действующий баланс не меняется. Индексы, поля документов, миграция и backfill в PR не входят.

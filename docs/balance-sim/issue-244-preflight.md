# Issue #244: первоначальная проверка моделей

Дата: 05.11.2026. Это протокол локальной технической проверки первоначальной пары mini-frida + MiniLM, не подбор игрового баланса. После [сравнения кандидатов](../ml-model-research-244.md) runtime-классификатор заменён на Horizon small; результаты MiniLM ниже сохранены как исходная точка сравнения.

## Закреплённые артефакты

| Компонент | Артефакт | Лицензия |
|---|---|---|
| Infinity v2 CPU | `michaelf34/infinity:0.0.78-cpu` | MIT |
| mini-frida | `sergeyzh/rubert-mini-frida` @ `32a82388a9f99c99eca74494731f7ee6e9a9fe3a`, исходный `model.safetensors` | MIT |
| MiniLM NLI | базовая модель `MoritzLaurer/multilingual-MiniLMv2-L6-mnli-xnli`; INT8 ONNX-экспорт из `onnx-community/multilingual-MiniLMv2-L6-mnli-xnli-ONNX` @ `ca5daf3d11b6c4b3143b1f4602a2edfb64c3ad7e` | MIT |
| MongoDB Search | `mongodb/mongodb-community-search:1.70.1` с `mongo:9.0.4-resolute` | [SSPL v1](https://github.com/mongodb/mongot) |

Ревизия mini-frida взята из [истории модели](https://huggingface.co/sergeyzh/rubert-mini-frida/commit/32a82388a9f99c99eca74494731f7ee6e9a9fe3a). Указанная первоначально в #244 ревизия MiniLM отсутствует в репозитории `MoritzLaurer/...`: она принадлежит [ONNX-экспорту](https://huggingface.co/onnx-community/multilingual-MiniLMv2-L6-mnli-xnli-ONNX). Теги Infinity перечислены в [Docker Hub](https://hub.docker.com/r/michaelf34/infinity/tags).

## Проверки до изменения игрового правила

1. На целевом CPU загрузить обе модели из локальных файлов одним процессом Infinity v2; для каждой модели указать собственный engine. [Автор Infinity подтверждает](https://github.com/michaelfeil/infinity/discussions/485) порядок аргументов `--model-id ... --engine ... --model-id ... --engine ...`.
2. Для mini-frida сравнить выход `/embeddings` с `SentenceTransformer` на одинаковом тексте. В [`config_sentence_transformers.json`](https://huggingface.co/sergeyzh/rubert-mini-frida/blob/main/config_sentence_transformers.json) уже указан `default_prompt_name: Classification` и префикс `categorize: `. Infinity v2 вызывает `tokenize()` напрямую и этот prompt автоматически не добавляет; клиент добавляет его один раз. Проверить masked mean pooling, L2-норму и размерность 312.
3. Для MiniLM сравнить три оценки `/classify` с `tokenizer(premise, hypothesis)` на нескольких синтетических парах. [Контракт Infinity](https://github.com/michaelfeil/infinity/blob/main/docs/assets/openapi.json) принимает массив строк, а не пары. Формат адаптации нельзя выбирать без измерения совпадения токенов и оценок.
4. Официальный [`mongot`](https://www.mongodb.com/docs/search/self-managed/current/installation/docker/) требует replica set, конфигурацию `mongod` с адресом Search, пользователя `searchCoordinator` и собственный том. Его `/health` **не** подтверждает готовность индекса. Перед изменением Compose проверить авторизацию текущего Mongo и создание/готовность HNSW-индекса на отдельной коллекции.
5. Только после проверок 1–4 собрать выходы моделей на синтетическом корпусе, подобрать пороги easy/normal/hard и сравнить баланс через `docs/balance-sim`. Не подменять выходы случайным `IsFlop`.

## Результат локальной проверки

- Обе публичные модели скачались без токена Hugging Face. Infinity v2 `0.0.78-cpu` загрузил их из локальных файлов в одном контейнере с `HF_HUB_OFFLINE=1`. На Docker Desktop с 2 CPU контейнер дошёл до готовности примерно за 28 секунд и после прогрева использовал около 2,2 GiB памяти при лимите 3 GiB. Это не замер на целевом сервере.
- Для уменьшения ресурсов проверена настройка без прогрева и ONNX-оптимизации, batch size 4 и без BetterTransformer у mini-frida. Обе исходные модели дошли до готовности при лимите 2,5 GiB; после коротких запросов использовали около 1,6 GiB. На десяти запросах медиана одного эмбеддинга — 13 мс, batch из трёх NLI-пар — 31 мс; первые запросы — 155 и 65 мс. Это локальный замер короткого текста, не нагрузочный тест на целевом сервере.
- В отдельном Compose-проекте MongoDB, `mongot` и Infinity работали одновременно и были healthy. После старта Docker показывал около 302 MiB для MongoDB, 291 MiB для `mongot` и 1,55 GiB для Infinity. Backend, OpenObserve, Collector и frontend в этом совместном замере не участвовали.
- mini-frida: размерность 312, L2-норма около 1; максимальная абсолютная разница с `SentenceTransformer.encode(..., prompt="", normalize_embeddings=True)` на двух синтетических строках составила `8,2×10⁻⁸`.
- MiniLM: строка `premise + " </s></s> " + hypothesis` дала те же токены, что `tokenizer(premise, hypothesis)` на трёх синтетических парах. `/classify` вернул три NLI-оценки на каждую пару; прямой INT8 ONNX дал близкие оценки. Сравнение на широком игровом корпусе ещё не проведено.
- `mongodb/mongodb-community-search:1.70.1` подключился к одноузловому `mongo:9.0.4-resolute` с ролью `searchCoordinator`. HNSW-индекс тестовой коллекции перешёл в `READY`; `$vectorSearch` с фильтром вернул ожидаемую запись. Документ с `embedding: null` не помешал поиску. На Windows пароль для `mongot` пришлось положить в Linux volume с правами `600`; обычный bind mount был отвергнут.
- Новый Compose поднял `mongo`, bootstrap и `mongot` без внешнего порта Search. Тестовый backend image создал `post_embedding_hnsw`: до готовности индекс был `PENDING`, затем стал `READY` и `/health/ready` вернул 200. После остановки `mongot` readiness вернул 503 примерно за 3,1 секунды.

Пороги easy/normal/hard, симуляция и нагрузочные замеры на ожидаемом числе миров и постов ещё не выполнены. Значения `docs/config/balance.json` и `docs/game-flow.md` не менялись.

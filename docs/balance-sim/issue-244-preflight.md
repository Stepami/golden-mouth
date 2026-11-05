# Issue #244: проверка перед внедрением

Дата: 05.11.2026. Это протокол подготовки, не результаты моделей и не новый баланс.

## Закреплённые артефакты

| Компонент | Артефакт | Лицензия |
|---|---|---|
| Infinity v2 CPU | `michaelf34/infinity:0.0.78-cpu` | MIT |
| mini-frida | `sergeyzh/rubert-mini-frida` @ `32a82388a9f99c99eca74494731f7ee6e9a9fe3a`, исходный `model.safetensors` | MIT |
| MiniLM NLI | `MoritzLaurer/multilingual-MiniLMv2-L6-mnli-xnli` @ `ca5daf3d11b6c4b3143b1f4602a2edfb64c3ad7e`, `onnx/model_quantized.onnx`, SHA-256 `55614f3c7da74184742eaa0006b978744437aa91de9ba4913db42f94d7844a8f` | MIT |
| MongoDB Search | `mongodb/mongodb-community-search`, точную совместимую версию закрепить после проверки с `mongo:9.0.4-resolute` | По лицензии образа |

Ревизия mini-frida взята из [истории модели](https://huggingface.co/sergeyzh/rubert-mini-frida/commit/32a82388a9f99c99eca74494731f7ee6e9a9fe3a). Ревизия и SHA MiniLM заданы в [#244](https://github.com/Stepami/golden-mouth/issues/244). Теги Infinity перечислены в [Docker Hub](https://hub.docker.com/r/michaelf34/infinity/tags).

## Проверки до изменения игрового правила

1. На целевом CPU загрузить обе модели из локальных файлов одним процессом Infinity v2; для каждой модели указать собственный engine. [Автор Infinity подтверждает](https://github.com/michaelfeil/infinity/discussions/485) порядок аргументов `--model-id ... --engine ... --model-id ... --engine ...`.
2. Для mini-frida сравнить выход `/v1/embeddings` с `SentenceTransformer` на одинаковом тексте. В [`config_sentence_transformers.json`](https://huggingface.co/sergeyzh/rubert-mini-frida/blob/main/config_sentence_transformers.json) уже указан `default_prompt_name: Classification` и префикс `categorize: `. Проверить, добавляет ли Infinity этот префикс автоматически. На входе модели он должен оказаться ровно один раз. Проверить masked mean pooling, L2-норму и размерность 312.
3. Для MiniLM сравнить три оценки `/classify` с `tokenizer(premise, hypothesis)` на нескольких синтетических парах. [Контракт Infinity](https://github.com/michaelfeil/infinity/blob/main/docs/assets/openapi.json) принимает массив строк, а не пары. Формат адаптации нельзя выбирать без измерения совпадения токенов и оценок.
4. Официальный [`mongot`](https://www.mongodb.com/docs/search/self-managed/current/installation/docker/) требует replica set, конфигурацию `mongod` с адресом Search, пользователя `searchCoordinator` и собственный том. Его `/health` **не** подтверждает готовность индекса. Перед изменением Compose проверить авторизацию текущего Mongo и создание/готовность HNSW-индекса на отдельной коллекции.
5. Только после проверок 1–4 собрать выходы моделей на синтетическом корпусе, подобрать пороги easy/normal/hard и сравнить баланс через `docs/balance-sim`. Не подменять выходы случайным `IsFlop`.

## Состояние проверки

В рабочем окружении 05.11.2026 Docker Engine не запущен (`docker info` не подключается к `dockerDesktopLinuxEngine`), локальных файлов моделей нет, доступ к Hugging Face из PowerShell запрещён сетевыми правами. Поэтому выходы моделей, совместимость контейнеров, HNSW и замеры ресурсов здесь не получены. Значения `docs/config/balance.json` и `docs/game-flow.md` пока не менялись.

# ML-оценки и отписки (#245)

#244 подготовил Compose и ML-клиентов; #259 — хранение эмбеддингов и векторный подбор звёзд. #245 подключает оценку к простому `POST /posts`. Подготовленные профили перенесены в действующий [balance.json](config/balance.json); он является источником чисел.

## Расчёт эмбеддингов постов

Этап `feat/embed` сохраняет вектор при создании поста игрока или звезды. `IEncodeSerializer<T>` в `Reki.Application.Posts.AI` определяет вход: для `GamePost` и `GameComment` — `Text`, для `WorldStar` — имя, роль и описание. Реализации находятся в `Posts/AI/Serializers`.

`IPostRepository.InsertPostAsync(GamePost, CancellationToken)` сам рассчитывает вектор через `IRekiEmbeddingGenerator<GamePost>` и сохраняет техническое поле `posts._embedding`. `FindSimilarPostsAsync(GamePost, threshold, CancellationToken)` рассчитывает вектор запроса внутри Mongo-репозитория, а `IStarVectorStore.GetRelevantCommenterAsync(World, GamePost)` — внутри стора звёзд. Хендлер и ML-скорер передают пост, а не готовый вектор; перегрузок с эмбеддингом нет. Доменные модели и публичный API векторов не содержат; обычные чтения исключают поле projection, сохранение комментариев его не перезаписывает. `PredictedAuthorId` и `FlopReason` — nullable-параметры конструктора `GamePost`.

Миграция 2.6.0 создаёт cosine-индекс `ix_posts_embedding` на 312 измерений и последовательно дополняет старые посты без вектора через `IAsyncEnumerable`. Заполнение выполняется вне транзакции мигратора и возобновляется по отсутствующему полю. Миграция 2.6.2 обновляет уже созданный индекс для ANN/HNSW и добавляет filter-поля WorldId/AuthorId; повторное создание индекса не требуется. Исторический backfill и сохранённые векторы переиспользуются.

## Запуск моделей

Штатный Infinity хостит четыре модели: первые три через torch, NLI через optimum. Готовый FP32 ONNX опубликован в публичном [Stepami/rubert-tiny-bilingual-nli-onnx](https://huggingface.co/Stepami/rubert-tiny-bilingual-nli-onnx); это экспорт исходного cointegrated checkpoint без дообучения. Для загрузки токен Hugging Face не нужен. В контейнере нет пользовательского entrypoint и экспорта при запуске; `--onnx-disable-optimize` оставляет скачанный граф без дополнительной преобразованной копии.

HF_HOME Infinity — `/models-cache`, volume — `infinity-models`. Backend подключает его read-only в `/models` и читает стандартный HF cache: `hub/models--Stepami--rubert-tiny-bilingual-nli-onnx/snapshots/<revision>/model.onnx` и `vocab.txt`. Revision одинаков в настройке модели Infinity и путях backend; он закреплён в Compose. Backend ждёт healthy Infinity, поэтому файлы доступны до его запуска. Модели не попадают в репозиторий; папок `ml/`, `infinity/` и ручного Python-запуска нет. Первый запуск требует интернета; удаление volume удаляет кэш. Backend использует локальный ONNX для правильных NLI-пар, а не строковый `/classify` четвёртой модели.

## Контракты

| Контракт backend | Модель | Результат |
|---|---|---|
| `IEmbeddingGenerator<string, Embedding<float>>`, OpenAI-клиент Infinity | `cointegrated/rubert-tiny2` | Нормированный CLS, 312 измерений, без префикса |
| `IClassifier`, `/classify` Infinity, модель `toxicity` | `cointegrated/rubert-tiny-toxicity` | Независимые non-toxic, insult, obscenity, threat, dangerous |
| `IClassifier`, модель `emotion` | `cointegrated/rubert-tiny2-cedr-emotion-detection` | Независимые no_emotion, joy, sadness, surprise, fear, anger |
| `INliScorer`, ONNX из общего HF cache | `Stepami/rubert-tiny-bilingual-nli-onnx` — экспорт `cointegrated/rubert-tiny-bilingual-nli` | Entailment и not_entailment каждой пары |

NLI принимает premise и список hypotheses с настоящими парными input_ids/attention_mask/token_type_ids. Строковый `/classify` Infinity не сохраняет сегменты пары, поэтому NLI работает в существующем backend. Not_entailment не означает доказанное противоречие. Клиент ограничивает пару 512 токенами, гипотезу 254, остаток получает premise; получает пост целиком и возвращает результат каждой пары. Самодельной нарезки на окна и агрегирования оценок частей нет; размер входа ограничивается токенизатором модели. Клиенты не принимают игровых решений и не обращаются в Mongo.

Адрес классификатора задаётся отдельно через `Infinity:BaseUrl` (`Infinity__BaseUrl` в Compose). `Embedding:BaseUrl` остаётся настройкой embedding-клиента; обе секции в Compose указывают на один контейнер Infinity.

## Оценка «в тему»

`WorldEncodeSerializer` кодирует название и описание мира и роль игрока. `GamePost` кодируется существующим сериализатором текста. `IRekiEmbeddingGenerator<World>` и `<GamePost>` возвращают векторы одной модели; `System.Numerics.Tensors.TensorPrimitives.CosineSimilarity` рассчитывает их cosine. Cosine сравнивается с порогом сложности: Topic = 1 при cosine >= порога, иначе Topic = 0. В расчёт качества и опыта идёт эта бинарная оценка. NLI оценивает только смелость и юмор одним вызовом по целому посту. В проверках темы и повторов NLI не используется. Классификаторы также получают текст без нарезки; для агрессивной ругани сохраняется исключение явно оформленных цитат.

В `balance.json:difficulties` поле `topic_min_cosine` равно 0.55 для easy, 0.65 для normal, 0.75 для hard. При Topic = 1 разбор показывает отметку «В тему», при Topic = 0 — нет. Само несоответствие теме не вводит отдельную причину отписки. В разбор не добавляется выдуманный отдельный числовой вклад. Драма не влияет на этот порог. Quality рассчитывается прежним средним brave/humor/topic и ограничивается 0…1.

## Поиск кандидатов на повтор

Mongo-репозиторий сам кодирует GamePost и выполняет `$vectorSearch` первой стадией pipeline по `ix_posts_embedding`. HNSW работает в ANN-режиме: pre-filter ограничивает WorldId/AuthorId, `limit=20`, `numCandidates=400` задают технический бюджет поиска. Индекс содержит 312-мерный cosine-вектор `_embedding` и оба filter-поля.

`vectorSearchScore` нормализован: `score=(1+cosine)/2`. Перед применением существующего `near_copy_cosine` и выдачей PostSimilarity значение возвращается в raw cosine как `2*score-1`. Наружу выходят только ID, текст и score, без массивов векторов. Повтор определяется наличием кандидата выше cosine-порога с совпадающей последовательностью числовых токенов. Дополнительной NLI-проверки и её права отклонить результат эмбеддингов нет.

ANN может пропустить кандидата, а поисковый индекс обновляется асинхронно. Это принятые свойства нового поиска: полного перебора, exact fallback или ожидания свежего поста в запросе нет. Размер бюджета можно пересмотреть по измерениям; это не игровые пороги. [Контракт MongoDB Vector Search](https://www.mongodb.com/docs/vector-search/query/aggregation-stages/vector-search-stage/).

## Четыре причины

1. `angry_abuse`: агрессивная ругань — min(insult, anger) собственной речи без явно оформленных цитат. Злая критика без оскорблений не равна ругани.
2. `near_copy`: очень близкий собственный повтор в том же мире. Нужны cosine не ниже порога и совпадение последовательности числовых токенов. Проверять полученных ANN-кандидатов; первая публикация не повтор. Хранение `_embedding` и заполнение старых постов уже выполнены миграцией 2.6.0; #245 использует HNSW/cosine и проверку чисел без NLI.
3. `sad_tone`: выраженная грусть полного текста.
4. `anxious_tone`: выраженный страх/тревога полного текста.

По согласованию пользователя искренняя грустная новость, просьба поддержки и эмоция персонажа могут отталкивать аудиторию. Это эмоциональная окраска, не детектор нытья, виновности или обмана. Цитаты для эмоций сохраняются. Реклама, слабая связь с темой и «воздуханство» не отдельные причины.

Приоритет задаётся полями выбранного профиля сложности `reason_priority` и `emotion_tie_break`: ругань → повтор → большая из прошедших порог эмоций (при равенстве грусть). Одна причина, один минус. Смелость/юмор не отменяют причину; счётчика серии нет. Ошибка ML не считается отсутствием причины.

## Пороги и размер потери

Источник чисел — `balance.json:difficulties`. Один профиль `difficulties.easy`, `difficulties.normal` или `difficulties.hard` содержит коэффициенты роста/залёта, настройки хейта, `loss_x`, `max_subscriber_fraction`, `thresholds`, `threshold_reduction_per_step`, порядок причин и вложенные модификаторы `drama`. Схема объекта одинакова для всех трёх сложностей; отдельных верхнеуровневых узлов `unsubscribe` и `drama` нет.

```text
profile = balance.difficulties[difficulty]
drama = profile.drama[dramaLevel]
threshold = profile.thresholds[signal]
            - drama.threshold_steps * profile.threshold_reduction_per_step[signal]
```

При средней драме:

| Сложность | Ругань | Повтор cosine | Грусть / тревога | Доля обычного прироста | Предел аудитории |
|---|---:|---:|---:|---:|---:|
| easy | .80 | .96 | .96 / .94 | 8% | 0.5% |
| normal | .70 | .94 | .94 / .92 | 12% | 1% |
| hard | .60 | .92 | .92 / .90 | 16% | 1.5% |

Базовые пороги профиля заданы для off. Его `drama.off/soft/mid/hot` даёт 0/1/2/3 шага: из базового порога вычитается число шагов × соответствующий `profile.threshold_reduction_per_step`. `profile.loss_x` и `profile.max_subscriber_fraction` умножаются на `drama.loss_multiplier`: .5/.75/1/1.25. В том же объекте драмы лежит прежний `hate_x`; его значения сохранены. Повышение сложности и драмы снижает пороги и увеличивает минус; на малом корпусе соседние уровни могут дать одинаковую частоту. Сравнение scores через >=.

При S — аудитории до поста, G — обычном приросте до множителя залёта, a = `profile.loss_x`, c = `profile.max_subscriber_fraction`, m = `drama.loss_multiplier`:

```text
loss = min(S, max(1, round(G*a*m)), max(1, floor(S*c*m)))
delta = -loss
```

Округление к ближайшему чётному. Минимум 1 может превышать процент для малого канала; при S=0 потеря 0. Опыт растёт как прежде. При причине нет обычного прироста и залёта. Без причины сохраняются прежние положительный расчёт и вероятность залёта; драма обычный прирост не уменьшает. Случайный flop заменяется, не добавляется поверх ML.

Числа — стартовый игровой выбор, не вероятности правоты модели. При допущении 10% целевых примеров симуляция 200 seed × 180 дней дала среднему игроку normal+mid 1M на день 51 вместо 53. При 40–60% целевых постов рост существенно медленнее из-за утраченного положительного прироста. Это допущения, не статистика игроков.

## Подключение

Причины представлены отдельными реализациями интерфейса, получаемыми из DI как `IEnumerable<IFlopTest>`:

```csharp
public interface IFlopTest
{
    int Priority { get; }

    Task<FlopTestResult> IsFlopAsync(GamePost post);
}

public sealed record FlopTestResult(bool IsFlop, string Reason)
{
    public static readonly FlopTestResult Ok = new(false, string.Empty);

    public static FlopTestResult Flop(string reason) => new(true, reason);
}
```

Проверки выполняются последовательно асинхронным LINQ в порядке `Priority` до первого `IsFlop=true`. Каждая реализация возвращает свою причину; скорер не проверяет конкретные типы. Проверка без причины возвращает `FlopTestResult.Ok`; при отсутствии сработавшей проверки причина поста остаётся null. Приоритеты из баланса и сравнение эмоций сохраняются.

`POST /posts?worldId=…` принимает `{text, ownWords, wantId}` и возвращает `{post, result, predictedAuthorId}`. Хендлер линейно вызывает ML-оценку, проверки причин, существующий расчёт прогресса и подбор обязательного автора, затем сохраняет пост и экономику через существующий UoW. Нет requestId/replay создания поста, конкурентных проверок или recovery-состояний. Ошибка ML не создаёт фиктивный успешный итог. Промежуточные scores не включаются в публичный результат.

Frontend анимирует сохранённый итог и последовательно вызывает общий SSE replies/generation с nullable `sourceCommentId=null`, затем replies для сохранения первого корневого комментария. SSE пишет только текст; сбой не отменяет пост. `Publication`, старые маршруты и коллекция удаляются. Лишние тесты старого протокола удаляются; существующие компоненты переиспользуются.

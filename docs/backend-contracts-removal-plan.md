# План удаления проекта Contracts

Статус: согласованный план; перенос кода ещё не выполнен. Работа продолжается в текущей отдельной ветке без issue. Этот документ описывает целевую структуру, а не текущее состояние solution.

## Целевая структура

```text
Reki.Application/
  Responses/
    PageResponse.cs
  Player/
    Responses/
    PlayerResponseMapper.cs
  Posts/
    Responses/
    PostResponseMapper.cs
  Worlds/
    Responses/
    WorldResponseMapper.cs
  Events/
    Responses/
    EventResponseMapper.cs
  Social/
    Responses/
    SocialResponseMapper.cs          # если нужен маппинг

Reki.Api/
  Requests/
    Player/                         # запросы и статический extension mapper
    Posts/
    Worlds/
    Events/
    Social/
    Telemetry/
  Serialization/
    DependencyInjection.cs
    ApiJsonSerializerContext.cs
```

Responses группируются по поддоменам, даже если отдельная модель сейчас нужна одному use case. Общие исходящие модели размещаются в корневой `Application/Responses` по использованиям внутри Application. Mapper исходящих сообщений находится в корне `Application/<Subdomain>`, а специфичные для поддомена модели — в его папке `Responses`, mapper входящих запросов — рядом с запросами в `Api/Requests/<Subdomain>`. Используем существующие мапперы; новые создаём там, где преобразование действительно требуется. Все мапперы — статические extensions, с Mapperly там, где он уместен; DI для мапперов не нужен.

## Что уже выполнено

- Serialization перенесена из Infrastructure в API.
- ProblemDetails и AI-модели включены в единый `ApiJsonSerializerContext`; отдельные production-контексты удалены.
- Настройка MVC, HTTP JSON и `IOptions<JsonSerializerOptions>` находится в `Api/Serialization/DependencyInjection.cs`; AI получает опции через DI, reflection fallback отключён.
- Фоновая генерация поста использует `IStarTurnGenerator`, сериализация истории и построение промптов инкапсулированы в Application.

Повторный перенос JSON-контекста и возврат отдельных AI-контекстов не требуются.

## Этапы

1. **Разобрать типы Contracts по назначению.** Исходящие DTO и SSE events отправить в Application, HTTP requests и вложенные входящие модели — в API. Остальные enum, IDs и статусы распределить по смыслу. Для каждого типа проверить ссылки и JSON-представление. `PublicationStatus` используется прикладной моделью и Mongo-репозиторием, поэтому его нельзя считать только HTTP-типом; `LaunchSurface` используется Application, поэтому его нельзя просто скрыть в API.
2. **Устранить дубли доменных типов.** Переиспользовать существующие доменные enum, если их семантика совпадает, например проверить `Drama` и `WorldDrama`. Учесть, что `Domain.Player.SkillId` — Vogen value object, а `Contracts.Player.SkillId` — enum: механическая замена недопустима, особенно для JSON-ключей словаря навыков. Не создавать доменные enum для чисто транспортных значений. Сохранить camelCase, kebab-case и snake_case значения, числовые представления и правила валидации.
3. **Перенести исходящие сообщения.** Создать `Responses` в нужных поддоменах Application, перенести модели, связанные outbound-типы и полиморфную SSE-иерархию. `PageResponse<T>` разместить в корневой `Application/Responses`. Обновить результаты commands/queries и их потребителей. Структуры ответов отключённых лички и друзей сохранить для существующих заглушек контроллеров; удалённую функциональность не восстанавливать.
4. **Разнести исходящий маппинг.** Сохранить существующий `PlayerResponseMapper` в корне `Player`. `PostContractMapper` привести к статическому extension mapper исходящих сообщений, переименовать в `PostResponseMapper` и оставить в корне `Posts`. Преобразования `MapWorld` и `MapEvent` перенести из `ApiMapper` в `Worlds/WorldResponseMapper` и `Events/EventResponseMapper`. Публичность оставить только там, где преобразование вызывается из другой сборки. Внутренние правила и проверки остаются на своих прикладных и доменных границах.
5. **Перенести входящие сообщения.** Создать `Api/Requests/<Subdomain>`. Сохранить binding attributes, validation attributes, значения по умолчанию и имена query-параметров, в частности для `WorldPostsRequest`. Вложенные `WantResponse`, `CollectionCardResponse`, `WorldRuleResponse`, `WorldStarResponse`, используемые `CreateWorldRequest`, заменить соответствующими request-моделями с прежним JSON-форматом. Не добавлять зависимость Application от API.
6. **Разнести входящий маппинг.** Разместить static extension mappers в соответствующих папках Requests. Перенести туда `MapWorldDefinition`, `MapAnalytics`, преобразования остальных запросов в commands/queries и используемую входящим маппингом очистку telemetry properties. Сохранить действующие ограничения и очистку данных. Обновить controllers, удалить `IApiMapper`, `ApiMapper`, их DI-регистрацию и корневую папку `Api/Mapping`.
7. **Обновить сериализацию и генерацию клиентов.** Обновить namespaces, зарегистрированные типы и enum converters в существующем `ApiJsonSerializerContext`. Не создавать новые контексты и не включать рефлексию. Обновить imports и aliases в `Api/nswag.json`, сохранив `generateDtoTypes=false`: тестовый клиент использует request-типы API и response-типы Application. Проверить результаты build-time OpenAPI и генерации клиентов; namespace-перенос не должен менять публичный JSON-контракт.
8. **Удалить проект.** После переноса всех типов удалить `Reki.Contracts` из solution и project references, обновить source imports в production и тестах, а также build/CI/Docker-конфигурацию, если она явно ссылается на проект. Актуализировать описание структуры в `backend/AGENTS.md` и ссылки в `docs/backend-spec.md` после фактического переноса. Итог: четыре production-проекта, без зависимости Application/Infrastructure от API.
9. **Максимально сократить ручной маппинг через Riok.Mapperly.** Финальным этапом рефакторинга пройти все перенесённые входящие и исходящие extension mappers. Заменить ручное копирование свойств, создание вложенных DTO, преобразование коллекций и совместимых enum генерируемыми static partial extension methods и настройками Mapperly. Переиспользовать типизированные преобразования, оставив ручной код только для правил, вычислений и очистки данных, которые требуют собственной логики. Сохранить JSON-контракт и поведение, явно обработать несовпадения членов без blanket ignore и reflection fallback.
10. **Проверить и закоммитить.** Сверить отсутствие ссылок на удалённый проект в активном коде и конфигурации. Выполнить Release-сборку solution, включая компиляцию тестового проекта и штатную генерацию OpenAPI/клиентов. Тесты и Docker не запускать по договорённости. После успешного завершения миграции и сборки удалить этот временный план из `docs` и ссылку на него из `backend/AGENTS.md`; постоянные правила размещения моделей и мапперов сохранить в `AGENTS.md`. Создать коммит без номера issue.

## Критерии завершения

- В solution отсутствует Contracts и ссылки на его assembly/namespaces в активном коде и настройках генераторов.
- Входящие модели и их extension mappers расположены в `Api/Requests/<Subdomain>`; папки `Api/Mapping` и DI-мэппера нет.
- Исходящие модели расположены в `Application/<Subdomain>/Responses` или общих `Application/Responses`; extension mappers поддоменов находятся в корне `Application/<Subdomain>`, вне папок Responses.
- Application и Domain не зависят от HTTP requests, API или ASP.NET Core.
- Сохранены маршруты, validation/binding, JSON-значения, SSE discriminators и поведение контроллеров.
- Единственная конфигурация сериализации остаётся в API, AI использует DI options без рефлексии.
- Ручной маппинг максимально заменён Riok.Mapperly; оставшиеся ручные преобразования содержат необходимую логику.
- Release-сборка solution проходит без предупреждений и ошибок.

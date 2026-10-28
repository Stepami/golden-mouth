# Проверка переноса #205 — 27.10.2026

- TypeScript (`tsc -b`) и production build: проходят. Сохраняется предупреждение Vite о размере основного JS чуть выше 500 kB (около 505 kB, gzip около 147 kB).
- 36 состояний Chromium: вход, возраст/согласие, каталог, лента, свои записи, редактор, история/навыки, профиль, настройки × 320/390 px × light/dark. Горизонтального переполнения и ошибок JavaScript нет.
- Каталог/лента/профиль/настройки/обсуждение загружены с действующего DEV API. Меню в обсуждении сохраняется.
- Управляемый клиент API в отдельном браузерном контексте: публикация → неизвестный исход → проверка статуса → подтверждённый результат. Повтор использует тот же publicationId и текст; проверено отсутствие дубликата истории. Серверные мутации в проверке заблокированы.
- Два подтверждённых результата: в своих записях показан только последний, во вкладке результатов — оба. Проверены изоляция по игроку/экземпляру мира, повтор одного postId и очистка истории.
- Проверены reduced motion, фокус/Escape диалога результата и отсутствие подстановки фиктивного XP.
- Проверка секретов и `git diff --check`: проходят.

Это проверка интерфейса и действующих контрактов. Семантика AI, серверные гонки/идемпотентность, полноценная сюжетка, экономика бустов, генерация изображений, физическая клавиатура телефона и VoiceOver этим проходом не подтверждаются. Различия прототипа и API перечислены в [README](README.md).


## Продолжение PR #219: сверка с HTML, 390×844

Сохранён и учтён исходный незакоммиченный проход по входу, возрасту, Лере, каталогу и Typewriter. Уточнены геометрия первого экрана, сообщения, типографика и оформление Леры; на возрасте добавлены возврат и Home/End. В каталоге доступны категории и новая карточка своего мира без обещания работающей диктовки или сюжетного автомата. В профиле и истории используется общий HUD; профиль показывает роль и известную обложку.

[Снимки, матрица и переходы](evidence/2026-10-27-parity/README.md): 20 состояний на 390×844 (10 × light/dark), проверены отсутствие переполнения, error boundary, JS-ошибок и незагруженных изображений. Отдельно пройдены десять групп переходов. Проверка использует браузерные ответы текущих wire-контрактов; на дату этого прохода backend на локальном порту был недоступен. Это историческая проверка; актуальная реальная DEV-проверка описана ниже.

Файлы presetGoals.ts, ветка контентной задачи #195 и backend не изменялись. Серверные механики из #221 не реализованы. Реальная генерация и серверное сохранение этим проходом не подтверждаются.


## Реальный DEV, 28.10.2026

Пользователь явно разрешил выкладку frontend #205 / PR #219 на личный DEV до просмотра дизайна. Production требует отдельного устного согласования со Степаном, подтверждённого пользователем. PR не мержится.

Frontend `b7176fb` адресно синхронизирован в существующий bind mount после dry-run и сверки mounts. Private env, emulator state и зависимости сохранены; lockfile совпадает. Из-за кеша Vite перезапущен только frontend-контейнер. Контрольная checksum-сверка frontend-исходников прошла. На Docker-диске 1.3 GiB свободно; образы не собирались, backend не пересобирался, схема БД не менялась, миграции не применялись. Записаны только синтетические данные отдельного QA-пользователя. Frontend и backend readiness: HTTP 200.

Проверка выполнена Chromium с настоящим DEV API, без перехвата/подмены API-ответов. Telegram launch data для отдельного синтетического пользователя подписывает штатный DEV-эмулятор; реальный запуск внутри Telegram этим не подтверждается. Чужие миры не использовались.

- Согласие изначально снято, продолжение заблокировано; возраст 18 ведёт к Лере и каталогу, возраст младше 14 — к экрану ограничения.
- Каталог и обзор мира загружены с сервера. Onboarding, сохранение настроек, создание отдельного тестового мира и повторный вход: HTTP 200.
- Черновик записи восстановлен после перезагрузки страницы без потери текста.
- Настоящая публикация и модерация: HTTP 200, серверная запись, комментарий AI-персонажа, компактный итог **+48 подписчиков**. Все изменения раскрываются в полный итог и сохраняются в локальной истории.
- Ответ герою: реальный SSE HTTP 200; в обсуждении появились тестовый ответ и новые AI-реплики. После повторной загрузки сервер вернул энергию **8/10** и аудиторию **177** (после публикации было 9/10 и 168).
- Лента, свои записи, история с полным результатом и профиль проверены в обеих темах при ширине 390 и 320 px. 16 состояний без горизонтального переполнения, JS-ошибок и битых изображений. Итоговая матрица и снимки — в [DEV evidence](evidence/2026-10-28-dev/README.md).

Ограничения: браузерная эмуляция устройства не заменяет физический телефон, клавиатуру, VoiceOver и Telegram WebView. Не проверялись серверные гонки, повтор неизвестного результата при обрыве реальной сети, атомарность и устойчивое качество AI. История результатов остаётся локальной; её наличие не доказывает серверный журнал. Старые управляемые проверки ошибок выше остаются отдельным свидетельством UI, а не реальной интеграции.

## Продолжение: окно своего мира и инвентарь, 28.10.2026

Проверка функциональных сценариев выше **не подтверждает полный visual parity**. Матрица `2026-10-27-parity` охватывает 10 базовых состояний × 2 темы; `ownWorldInfo` в неё не входил. Первоначальный исследовательский HTML-прототип использовался для основных экранов, а sheet остался упрощённым. Исторические результаты не переписаны так, будто эта модалка была проверена.

Для этого продолжения взят явно указанный актуальный эталон `app.html`: tokens → app-base → app → components CSS, затем catalog/app JS. SHA-256 исходников — в [manifest](evidence/2026-10-28-own-unlock/source-manifest.json). Маркетинговый `reference.json` не является экранным источником.

Восстановлены композиция sheet, карточка открытия, крупный счётчик, progress, альтернатива приглашения, CTA и «К прогрессу». Диктовка не обещается: открытый доступ ведёт в ручной редактор. Право берётся только из серверного `hitOwnWorldMilestone`, не из числа подписчиков. Лучший из загруженных миров помечен по имени; при ошибке списка показан текущий мир с оговоркой, у гостя нет фиктивного 0/500. Текущее значение player не заменяется старым снимком owned worlds.

[Парные кадры и измерения](evidence/2026-10-28-own-unlock/README.md): 320/390 × light/dark, реальные 177 подписчиков отдельного QA-пользователя. API в основной паре не подменён. У эталона намеренно установлен тот же счётчик **как fixture для сравнения**, не как серверный результат. Во всех четырёх парах совпали width, padding20, radius24, title18, lead15 и фон темы; горизонтального переполнения/ошибок JS нет. Высота DEV отличается из-за честной подписи о текстовой настройке и источника счётчика; native progress эталона и стилизованная шкала приложения также не pixel-identical. Invite/progress/close/Tab focus trap прошли на DEV.

Отдельные **UI fixtures** (серверные мутации заблокированы): guest, ошибка профиля, ошибка второго запроса миров после успешного входа, лучший другой мир420, счётчик620 без права, unlocked при177. Проверены отсутствие выдуманных данных, переход к прогрессу другого мира и ручному созданию при серверном флаге. Эти fixtures не доказывают выдачу реального права/работу приглашения на сервере. Реальные пользовательские миры не изменялись.

Общие sheets: title18/1.3, body14/1.5, input16, CTA/close≥44; заголовок и закрытие едины в справке и настройках. Справка навыков проверена в обеих темах на320/390: без overflow, размеры18/14/21/44. Текущая серверная экономика сохранена; пример кристаллов/бустов не перенесён. Дополнен вариант «Системное»: смена OS scheme light→dark и reload сохраняют предпочтение. Закрытие настроек/Escape проверены без сохранения серверных изменений.

[Полный инвентарь](INVENTORY.md) содержит 76 строк поверхностей/групп состояний и 9 ролей дизайн-системы с `matched/adapted/missing`; большинство строк — **adapted**, а не matched. По каждой строке указан тип доказательства: новая pair/live, историческая UI-проверка или source-only. Новая парная визуальная проверка всех строк не заявлена. Конкретный UI-остаток (компактный picker, NPC-профиль, energy help, resume/unseen queue и другие компоновки) передан координатору; API-пробелы отделены. Новые вопросы навигации не решены незаметной заменой рабочего пути.

Frontend обновлён адресно, после dry-run в существующий bind mount; private env/dependencies сохранены. Backend, схема БД, production и merge не затронуты. Состояние DEV-эмулятора после QA восстановлено. TypeScript/build, diff и secret scan проверяются для итогового коммита; результаты CI отражены в PR.

Дополнительно для отсутствия авторизации: frontend `9133b2b` различает401 и404. Проверка с удалённым Authorization в **исходящем запросе**, без подмены ответа, получила реальный `/me`401 и публичные templates200. Каталог/sheet доступны для просмотра; нет счётчика/шкалы, защищённый прогресс предлагает вернуться к мирам. Это отдельная проверка реального отказа авторизации, а не fixture серверного доступа. Основные пары и остальные состояния сняты на `e5ac87a`; последующий `9133b2b` добавляет именно публичную401-ветвь, не меняя композицию авторизованной модалки.


## Продолжение: компактные HUD sheets, 28.10.2026

Frontend **6d91d39** адресно обновлён на личном DEV после dry-run: только GameHeader, GameSheets и redesign.css. Проверены bind mount и прежние checksum; private env, emulator state и зависимости исключены. Перезапущен только frontend, backend/БД/production не менялись. Docker-диск: около1.2GiB свободно, образы не собирались. Frontend/API readiness HTTP200; checksum-сверка frontend не обнаружила отличий исходников.

[Восемь пар эталон↔DEV](evidence/2026-10-28-hud-sheets/README.md): worldPicker и energyPolicyInfo ×320/390×light/dark. Основные пары — настоящий DEV API без перехвата/подмены ответов; отдельный прежний синтетический QA-пользователь,177 подписчиков и8 энергии. Данные эталона — fixture сравнения. Проверены текущий мир, каталог, закрытие/«Понятно», focus trap/Escape/возврат фокуса, backdrop и восстановление черновика после навигации. Серверных публикаций в этом проходе не создавалось; прежняя SSE-проверка остаётся отдельным свидетельством.

У picker совпали ширина viewport−20, padding19/24, radius30, title18, фон; у energy — viewport−24, padding20, radius24, title18, фон. Высота и содержимое адаптированы: API не даёт сюжетный статус, неизвестному шаблону показан аватар, а энергия обновляется в09:00МСК вместо демонстрационной регенерации. Полное совпадение пикселей и проверка всех строк инвентаря не заявлены. Переполнения и ошибок JavaScript в основных парах нет.

TypeScript, production build, secret scan и diff check прошли; предупреждение Vite о основном чанке>500kB сохраняется. Полный редизайн остаётся незавершённым: актуальный остаток — в инвентаре. DEV разрешён пользователем до просмотра Степаном; production требует отдельного согласования, PR не мержится.

Семь дополнительных UI-fixture ветвей (loading/error→retry/empty/many/switch-error/switch-success/zero-energy) прошли на320px с заблокированными серверными мутациями. Проверен показ3из4→все4, сохранение текущего мира при ошибке и открытие другого при успехе; это не реальный API-проход смены между двумя мирами. Состояние DEV-эмулятора восстановлено после QA.


## Повторный проход сценариев и исправление controls, 28.10.2026

Текущий frontend на DEV **8003dd2**, после адресного dry-run/sync и перезапуска только frontend. Исходники проверены checksum; frontend и readiness HTTP200, image/start backend прежние. TypeScript/build, diff и secret scan PASS; CI frontend build и оба secret scan PASS.

[Наборы evidence](evidence/2026-10-28-scenarios/README.md) отделяют20 entry-пар,16 primary-пар с fixtures и настоящие API-сценарии. Настройки мира проверены реальным PUT→повторное открытие→восстановление исходного имени; приглашение — настоящий referral и clipboard. Новый синтетический мир создан через NPC-форму без обязательного mood. Завязка имеет статический заголовок и явный CTA. Черновик сохранён после reload и повторного открытия редактора (сам маршрут после reload не сохраняется). Реальная публикация/SSE дала немодальную плашку над настоящим каналом; expand/close и ответ персонажу прошли. Эти операции выполнялись на b413cb0;8003dd2 меняет только controls/удаление.

Настройки и профиль проверены заново на настоящем DEV8003dd2:320/390/540×2 темы, видимые границы save/edit/settings; подтверждение удаления→отмена, DELETE-запросов0. Ошибка DELETE503 проверена только с перехватом API; форма сохраняется. Удаление аккаунта на сервере не запускалось.

Не подтверждены полное совпадение всех экранов, все ошибки редактора/результата в новой композиции, физический Telegram WebView и серверная идемпотентность при обрыве сети. Отсутствующие серверные сюжетные стадии/награды/точный XP/отношения не заменены fixtures. Следующий UI-шаг и владельцы записаны в REVIEW и PR; не ждём Степана для разрешённых frontend-исправлений.

Финальная frontend-синхронизация: **a950607**, после dry-run; checksum без отличий, frontend/readiness HTTP200. После парной сверки16 редактор/compact/full/channel кадров исправлены позиция close/title, типографика и цвет текста основной кнопки. Границы controls и реальная запись/ответ проверялись ранее на8003dd2/b413cb0; последние изменения не выдаются за повтор новой серверной публикации. На8003dd2 дополнительно прошла24-state реальная матрица и переключение между двумя QA-мирами с reload/возвратом.

## Recovery, participants and resume — 28.10.2026

Implementation `f923177` is deployed to personal DEV. Source: current executable app.html/app.js and its CSS chain; existing source manifest remains applicable. [Evidence](evidence/2026-10-28-recovery-people/README.md).

- **Real backend, no response interception:** browser offline → unknown → GET completed publication → compact/full result/close; publication and moderation POST count **0**. Pending context was seeded locally for an already completed QA publication; this does not prove server idempotency during an interrupted new creation. Real two-NPC roster/profile, author-ID own-profile navigation, direct Live without pending → composer: PASS, 320/390 and both themes, no overflow/JS errors. No new publications, worlds, replies or deletion were performed in this continuation.
- **Controlled UI fault injection, not integration:** 32 recovery states (completed, moderation-ready, generating,503, technical/content/limit/unsafe failures ×2 widths×2 themes), no creation POST, moderation only for server-ready state. Eight paired participant/profile frames. Queue contract: FIFO, deferral on forms/settings, seen persistence, world isolation. Five local resume/storage edge contracts passed.
- Pending publication recovery uses status GET, never re-creates a post. Latest local post/reply draft is resumed with target preserved; pending publication takes priority. Confirmed unseen results are local per world, deferred while forms/dialogs are open. Older history without a seen flag is not retroactively treated as unseen.
- Participants are real world NPCs. Profile posts use exact detail.authorId, not display-name matching. Feed API lacks author filtering and author IDs in list items, so detail reads and cursor pagination are necessary. No server-wide participants directory, relationship meter, authoritative story stage or cross-device unread queue is claimed.
- HUD preserves real energy/skills; prototype crystals/day mechanics are not fabricated. NPC handle/bio/cover use available authoritative world/template data. Custom/edited worlds do not receive unrelated prototype lore or artwork. Fixture pair cover can differ when the fixture does not match a known server template; real DEV template mapping was checked separately.

DEV frontend/readiness HTTP200 and scoped sync checksum passed; backend image/start unchanged. Production, merge, backend and database changes are outside this work. Physical Telegram WebView, real failed-publication outcomes and interrupted-new-publication idempotency remain unverified. Full pixel parity for every reference surface is not claimed.

## Финальная адресная сверка limit/settings/profile, 28.10.2026

Код **3b623a9** после580dbea. [Конечный список](INVENTORY.md#конечный-список-адресного-прохода-после580dbea), [24 пары и реальные проверки](evidence/2026-10-28-final-surfaces/README.md).

Текущий showLimit — справка, поэтому удалён старый полноэкранный экран с Мирой/промо, а не добавлена новая fullscreen-композиция. Profile history сохраняет контекст профиля. Настройки повторяют narrow stack и раскрытия секций; корректно склоняется число записей.

24 контролируемые пары320/390×light/dark: ошибок JS/overflow0. Real DEV без interception: профиль/вкладки, настройки cancel/reopen/Escape, energy help/draft/reload — четыре width/theme комбинации PASS, world/post/account mutation requests0. Это не проверка настоящего нулевого баланса или нового PUT — серверный баланс не менялся; zero route проверен отдельно контролируемыми данными. Backend image/start неизменны; frontend-only sync/checksum и health200. TypeScript/build/diff/secrets PASS, существующее chunk warning>500kB. Эмулятор после QA восстановлен. Физический Telegram WebView штатно недоступен и остаётся непроверенным.

Этот адресный проход завершён; полное pixel parity не заявлено. Конкретные оставшиеся необязательные UI-адаптации и API-границы перечислены без новых issues. Следующий владелец frontend может взять только явно перечисленные UI-пункты; серверные механики требуют отдельного согласованного scope. DEV разрешён до ревью; production только после отдельного согласования со Степаном, подтверждённого пользователем. Merge/review request/backend/DB не выполнялись.

## Три оставшихся UI-пункта завершены, 28.10.2026

Frontend **1c32f4b**, реализация e2aaf63 + доступная геометрия pause. [24 парных состояния и реальные проверки](evidence/2026-10-28-three-ui/README.md), [актуальный инвентарь](INVENTORY.md#три-последних-согласованных-ui-пункта--закрыты).

Pause/resume приветствия и live reduced motion; текущий мир первым, первые2 и native remainder disclosure;3 точных примера навыков с возвратом в существующий post/reply editor. Back/Close/Escape сохраняют текст, выделение/фокус, адресата и request ID. Нет подстановки XP/награды/кристаллов. Контролируемые4-world данные и24 пары не выдаются за backend-интеграцию. Отдельно реальные2 QA мира/выбор и все3 примера post/reply320/390×2темы PASS, мутаций worlds/posts/account0. После reload открывается исходный URL; повторное открытие того же поста восстанавливает reply draft.

TypeScript/build/diff/secrets PASS; существующее chunk warning>500kB. DEV frontend/readiness200, scoped checksum идентичен, после restart только frontend браузер подтвердил актуальные CSS pause44px/no overlap. Backend/БД/production/merge не затронуты; QA emulator восстановлен. Физический Telegram WebView недоступен и не проверен. Эти три UI-различия больше не открытый остаток; намеренные API-границы не названы незавершённым frontend. Следующий шаг — просмотр результата #219 пользователем/ревьюером, без автоматического review request/merge.

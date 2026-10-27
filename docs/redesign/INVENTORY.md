# Инвентарь актуального эталона → frontend — #205 / #219

Дата: 28.10.2026. Это перечень поверхностей и ветвей текущего исполняемого эталона, а не утверждение о полном pixel parity. Исторические README v0.35–41 описывают разные итерации; при расхождении проверялся текущий `app.js`, а не прежнее описание. Например, текущий `welcomeMontage()` снова содержит социальный пост, а `guidedOnboarding()` — два шага Леры.

Источники: `app.html` → `tokens.css`, `app-base.css`, `app.css`, **последним `components.css`**; затем `catalog.js`, `app.js`. `system.html` встраивает эти же поверхности, не отдельную реализацию. `reference.json` с маркетинговым референсом Yope не использовался как макет экранов. Идентичность входных файлов зафиксирована [SHA-256 manifest](evidence/2026-10-28-own-unlock/source-manifest.json), без персональных путей.

- **matched** — проверенное совпадение указанного компонента/роли, не всего экрана.
- **adapted** — рабочий аналог есть, но компоновка, контент или поведение отличаются. Это не pixel parity.
- **missing** — поверхность/ветвь отсутствует. Причина отдельно: API, локальный fixture или UI. Наличие старой gallery-демонстрации не считается продуктовой реализацией.
- **pair** — новые парные кадры; **live** — реальный DEV API; **UI fixture** — управляемый браузерный ответ, не доказательство серверного права; **source** — сверка исходников и маршрута, без заявления о свежем визуальном проходе.

## Вход, каталог и настройка

| Эталон / состояния | Приложение | Статус | Основание / конкретное отличие |
| --- | --- | --- | --- |
| `welcomeMontage`, welcome: фото, пост, ответ, сообщение | `01-1_invite` | adapted | Исторические базовые кадры + live вход; структура сохранена, текст/набор реплик не идентичны |
| `setupWelcomeText`: смена текста, reduced motion | `WelcomeHeadline`, Typewriter | adapted | source; ручная пауза тезисов эталона отсутствует, reduced motion есть |
| `onboardingPage`: колесо возраста, consent, disabled CTA | `01-2_age` | adapted | live + source; собственная проверка 14+, начальное согласие снято |
| `guidedOnboarding`: Лера 1/2, назад/пропуск | `01-2c_guide` | adapted | Базовые кадры, переходы; два шага, не более ранний трёхшаговый fixture |
| Каталог для первого входа / возвращения | `01-3_world` | adapted | live; реальные сохранённые миры вместо prototype instances |
| `cards`, категории и карточки новых миров | `world-catalog`, `WorldCover` | adapted | Базовые кадры/live; фильтры фиксированных пресетов, доступ/персоны с API |
| `worldsPage`: свои миры, продолжение, раскрытие остальных | `world-resume`, `owned-stories` | adapted | source/live; список не сворачивается до двух карточек |
| `worldPicker(false/true)`: компактный sheet / все миры | `GameHeader` → каталог | adapted | source; тот же выбор реальных миров, но **отдельного компактного sheet нет**; изменение навигации не выдано за исправление CSS |
| `pendingPanel`, `resumeContext`: незавершённый контекст | продолжение черновика мира/публикации | adapted | source + прежние UI-проверки; нет единой карточки всех видов контекста |
| `ownWorldHero`: locked / unlocked | `own-world-prototype` | adapted | live/UI fixture; счётчик только при наличии данных, unlock исключительно серверный |
| `ownWorldInfo`: карточка условий, счётчик, progress, приглашение, CTA | `own-unlock-modal` | adapted | **pair 320/390 × light/dark**; композиция восстановлена; вместо обещания голоса — текстовый путь, добавлен источник реального счётчика |
| Own-world sheet: неавторизованный401 / новый пользователь404 | публичный каталог, sheet «—», первый мир | adapted | **live401** с удалённым Authorization запроса, templates200; отдельно404 UI fixture. Нет выдуманного 0, шкалы и перехода в защищённый прогресс |
| Own-world sheet: лучший/текущий мир, загрузка/ошибка списка | реальные owned worlds + текущий player | adapted | live177 + UI fixture420/ошибка; при неполном списке не называется «лучшим» |
| Own-world sheet: счётчик ≥500 без права | locked CTA | adapted | UI fixture620; цифра не заменяет `hitOwnWorldMilestone` |
| Own-world sheet: unlocked при небольшом счётчике | «Можно создавать», текстовый редактор | adapted | UI fixture; серверное право не выводится из счётчика; причина разблокировки не выдумывается |
| `createForm`: профиль в шаблоне / выбор героя | обзор → `01-4_channel` | adapted | live/source; отдельные страницы вместо длинного sheet |
| `ownCharacterSelect`, `ownCustomCharacter`, `ownCharacterChoose` | QuickChannelForm / редактор персоны | adapted | source; реальный состав API, выбранный готовый NPC не дублируется |
| Настройка драмы/сложности | `01-4b_world_setup` | adapted | live/source; нынешние значения API, не новая экономика |
| `ownReview`, редактирование места/персоны/NPC/правил | `05-12_world_advanced`, `05-7b_own_world` | adapted | source/UI fixture; ручной редактор, не результат AI-генерации; заблокированные правила сохраняются |
| Создание / save, ошибка валидации, конфликт, неизвестный исход | StarsForm / WorldAdvanced / WorldSwitchDialog | adapted | source + прежний UI-проход; нет фиктивного успешного сохранения, при неизвестном исходе предлагается сверить миры |
| `ownIdea`: идея → генерация | ручной текстовый редактор | adapted | source; нет сцены с голосовым CTA, поскольку кнопка не запустила бы реальную операцию |
| `ownRecord`, `ownTranscribing` | отсутствуют | missing | **API**: микрофон/STT не подключены; нет имитации распознавания |
| `ownGenerating`, `ownMagic`: генерация и прелоадер | отсутствуют | missing | **API**: нет генератора/операции с проверяемым статусом; существующий DEV-preview не считается им |
| `ownCreating`, возобновление generation/create phase | обычный POST /worlds и сверка списка | adapted | source; нет фальшивых этапов «Компания собралась» |
| `ownImageAction`: upload/generate cover/avatar | DiceBear и известные обложки | missing | **API**: нет серверного медиа/генератора; локальный fixture эталона не переносился как работающий сервис |

## История, HUD, развитие и награды

| Эталон / состояния | Приложение | Статус | Основание / конкретное отличие |
| --- | --- | --- | --- |
| `resourceHud`, `header`, sticky | `GameHeader` | adapted | live + source; нет кристаллов/дня, отдельный HUD-макет и текущая энергия |
| `energyMeter`, countdown +1/15 мин | энергия, reset в 09:00 МСК | adapted | **API**; таймер регенерации не рисуется по fixture |
| `energyPolicyInfo`, `showLimit`: компактная справка | текст reset / `04-1_limit` | adapted | source; **компактного energy sheet нет**, текущий полноэкранный лимит и его старый текст не считаются парным переносом |
| `skillHelp` | `SkillHelp` | adapted | Новая типографика/close + live UI; объясняет текущую механику, без покупки кристаллов |
| `skillExample(0..2)` | объяснения навыков в SkillHelp | adapted | source; нет отдельных per-skill окон с возвратом в редактор |
| `progressPage`, goal / skills | `05-13_progress` | adapted | live; ориентир из контента, реальные шкалы и серия; не состояние автомата |
| `progressionTasks`: отметки подцелей | нет ручной отметки | missing | **API/смысл**: подтверждение сюжетных действий не возвращается; нажатие не выдаётся за достижение |
| `storyDashboard`, pause/resume, chapter claim | контекст/цель без автомата | missing | **API**: нет узла, чекпоинтов, паузы и подтверждённой награды |
| `characterIntro`, `characterOpeningPage` | `01-6_home` | adapted | live/source; завязка и текущая персона, не обещание персональной сгенерированной истории |
| `storyWritingPage`, `editorGoal` | первый пост / контекст Compose | adapted | live/source; статический ориентир вместо активного сюжетного intent |
| `personalArcIntro`, этапы/требования арки | отсутствуют | missing | **API**: нет прогресса персональной арки; fixture не проверяет реальный сюжет |
| `progressionAchievements`, ready/claimed/next batch | отсутствуют | missing | **API**: нет статусов/выдачи наборов; старая collection feature flag не замена |
| `visitStreakCard`, семь дней серии | серверная streak в профиле/истории | adapted | live; серия не подарок дня |
| `claimStreakReward`, gift art / claim / claimed | отсутствуют | missing | **API**: нет проверяемого календаря подарков и однократного получения |
| `developmentWallet`, crystal balance | отсутствует | missing | **API**: нет валюты/баланса/журнала |
| `boostStorePage`, owned/empty/no-money | отсутствует | missing | **API**: нет товаров и inventory |
| `storeGet`, `storeBuy`, `buyBoost` | отсутствуют | missing | **API**: нет атомарной покупки; цены fixture не утверждены |
| `boostPicker`, select/remove/equip, reserve/use/release | отсутствуют | missing | **API**: нет предмета/резерва/списания на конкретный request |
| `skillUpgrade`, `spendSkill` | отсутствуют | missing | **API**: нет подтверждённой покупки XP/бустов |
| `rewardBurst`, toast начисления | отсутствует | missing | **API**: награды не выдаются; декоративный успех без начисления не показывается |

## Лента, редакторы, обсуждение и результаты

| Эталон / состояния | Приложение | Статус | Основание / конкретное отличие |
| --- | --- | --- | --- |
| `feedPage` all / mine, tabs | `03-3_feed`, `03-5_stage` | adapted | live16-state matrix; реальные страницы/курсор вместо local page number |
| feed loading / empty / error / reload / more | usePostPages | adapted | source + прежние UI-проверки; новое реальное прохождение не воспроизводило все сетевые ошибки |
| `postCard`, authorLine, time, metrics | FeedPostCard / Stage | adapted | live; серверные views/emoji, нет фиктивного личного like/repost |
| `peoplePage`: участники | состав NPC в обзоре/редакторе | adapted | source; **отдельной страницы участников нет**, просмотр списка не утверждается как просмотр профиля |
| `profilePage`: конкретный NPC, relationship, его посты | нет равноценной NPC-страницы | missing | **UI + API**: старый `03-2_star_card` — галерейная Мира, не профиль реального NPC; перенос не заявлен, отношения не подставлены |
| `compose` blank / draft / suggestions / disabled | `02-1_compose` | adapted | live + draft reload; полноэкранный редактор и лимит500 вместо modal/2000 |
| `publish`, pending/generating/moderating | `02-2_live` | adapted | live SSE; настоящая операция с request/publication ID |
| `pendingPanel`, failed / unknown / recover publication | `02-2b_publish_error`, «Проверить статус» | adapted | прежние UI-проверки + source; реальный сетевой обрыв не симулировался как доказательство серверной идемпотентности |
| `drawThread`, root / nested target / recipient unavailable | `02-6_thread` | adapted | live/root+nested UI; полноэкранное обсуждение с dock вместо prototype dialog |
| Reply draft / sending / partial / failed / recover | Thread + api.reply | adapted | live SSE success + source/прежние UI-ошибки; при retry сохраняется requestId |
| `publicationPeek`, компактный результат поверх поста | `02-3_result` | adapted | live +48; отдельный focus-trapped dialog вместо раскрытия того же inline details |
| `postOutcome`, full breakdown / expand / collapse | PublicationResult | adapted | live; сохранённые серверные whys/ribbons, не объяснения fixture |
| Result flop / viral / neutral, reduced motion | Result/Flop/Viral | adapted | прежние UI-кадры; минус без зелёного празднования, реальные flop/viral этим проходом не вызывались |
| `resultSkillChanges`, before/after/why | не показываются без серверного снимка | missing | **API**: точного XP поста нет; две загрузки профиля не доказательство причинности |
| `relationshipChanges`, specific NPC delta | отсутствуют | missing | **API**: нет сохранённых изменений отношений/объяснений в результате |
| Reply result как у поста, skill gains | только готовое обсуждение / обновлённый HUD | missing | **API**: ReplyLiveEvent содержит energyLeft/subscribers, но не полный result/снимок XP; разница аудитории не выдана за весь результат |
| Отложенный unseen result при другой вкладке/открытой форме | восстановление текущей публикации | adapted | source; **общей очереди unseen results нет**, это отдельный пробел UI/хранения, не покрытый базовыми20 кадрами |
| `profileHistoryTabs`, `actionHistory` post+reply | история подтверждённых публикаций | adapted | live; локальная, только посты, не полный серверный журнал всех действий |

## Профиль, настройки, состояния и инструменты просмотра

| Эталон / состояния | Приложение | Статус | Основание / конкретное отличие |
| --- | --- | --- | --- |
| `identityPage` профиль / свои посты / история | `05-5_collection`, отдельные Stage/Progress | adapted | live; роль/обложка с API, другая раскладка статистики и вкладок |
| `settingsForm`: персона/мир/NPC/драма/правила | Settings + WorldDetails + WorldAdvanced | adapted | source/live; формы распределены по страницам, контракт сохраняется |
| `appearance`: system/light/dark | сохранённый ThemePreference | adapted | В этом продолжении восстановлено **system**, смена OS scheme/reload проверяются отдельно; ручные темы не переписаны |
| `chooseAvatar`, resetAvatar, локальное фото | DiceBear personalization | adapted | source; загрузка фото отсутствует; это не равнозначные функции |
| settings conflict / error / reload | сообщения ошибок + сохранённый draft | adapted | source/прежний UI-проход; эталонный modal не воспроизведён полностью |
| `inviteInfo`, copy | `04-3_invite` | adapted | live; настоящая referral ссылка вместо example.com fixture; полноэкранный маршрут |
| `worldBoot`, processing stage | загрузка шаблонов/создание | adapted | source; существующая операция не сопровождается вымышленными AI-фазами |
| `uiState`, loading/error/no-energy/pending | реальные состояния каждого запроса | adapted | source; нет единой геометрии всех state-panel эталона |
| `durable=false`, local storage unavailable | предупреждение черновика с session fallback | adapted | source/прежние UI-проверки; не реальный отказ устройства |
| `disclosure`, theme/skin preview, reset, conflict/reply-fault toggles | gallery / preview параметры | adapted | Инструменты просмотра, не продуктовые серверные функции; не доказательство live |
| `firstRunPreview`, restoreReviewSession, return-session | отдельные браузерные QA-контексты | adapted | Fixture-инструмент эталона; копирование локальных данных не подменяет вход в аккаунт |
| `system.html`: navigation/progress/store | этот инвентарь + реальная UI | adapted | Общие компоненты не становятся реализованными от наличия iframe-каталога |
| Дополнительные экраны приложения: safety/report/crisis, плюс, события, уведомления/данные/удаление | существующие маршруты frontend | adapted | **Нет равного экрана в текущем app.js**; не названы matched с эталоном и не удалены ради макета |

## Дизайн-система: что проверено

| Роль | Статус | Проверка / остаток |
| --- | --- | --- |
| Семантические bg/surface/text/muted/border/accent двух тем | matched | Значения tokens.css сопоставлены с redesign.css; у модалки computed bg совпадает в pair |
| Sheet title18/1.3, body14/1.5, action14, поле16, close≥44 | matched | Общий Sheet переведён на эти роли; собственный lead15 соответствует components.css; вторичные окна больше не используют заголовок20 |
| Own-unlock width viewport−24 (max400), padding20, radius24 | matched | Computed measurements в четырёх парах |
| Card unlock radius15/padding16, count27, caption13 | matched | Сверка CSS и пары; реальный источник счётчика добавляет строку/высоту |
| Формы и кнопки вне sheet | adapted | Существующие поля16/controls≥44; большие CTA приложения48 и page title28 — не универсальное совпадение с любым эталонным экраном |
| Sheet/modal: центрирование, scroll, close, Escape, focus trap | adapted | Shared Sheet исправлен; Result и длинные редакторы отдельны; не все модалки pixel-identical |
| Отступы4/8/12/16/24, радиусы полей14/cards22 | adapted | Базовые роли совпадают; специальные карточки/страницы имеют локальные размеры. Глобальное совпадение каждого отступа не заявлено |
| Типографика основных страниц/HUD, фон, dock, иконки | adapted | Общая семья системного sans; font-size16 приложения отличается от body14 токена и некоторых override эталона; отдельные специализированные страницы требуют адресной сверки |
| Motion press/reveal/wait + reduced motion | adapted | Базовая система/reduced motion есть; не заявлена полная парная проверка каждого перехода/паузы |

## Оставшиеся решения, а не новые issues

API-пробелы перечислены выше и в [карте контрактов](README.md#границы-которые-требуют-серверной-работы). Они не закрываются CSS и не разрешают новую backend-реализацию.

Отдельно остаются UI-различия: компактный переключатель, отдельный список/профиль NPC, единый контекст продолжения/очередь unseen результатов, справка энергии, настройки/редакторы и профиль в другом расположении. Они переданы координатору как конкретный остаток #219. Для замены рабочих маршрутов на новые страницы/окна нужно выбрать поведение, а не молча назвать текущий аналог совпадающим. Новые issues, backend-механики и имитация наград не создавались. #205 и #219 остаются открытыми; весь редизайн не объявлен завершённым.

# «Вечерело»: портреты v10

11.11.2026, Refs #205. По запросу владельца продукта: Эдуард получает более выраженные мужские черты, Яков — светлокожую внешность, свой герой — отдельные четыре образа этого мира. Изабелла (`muse-v8.png`) и Лида (`spark-v8.png`) сохранены. Изменения касаются frontend; серверные шаблоны и сохранённые прохождения не мигрируются.

Созданы встроенным imagegen, с прозрачным фоном; финальные PNG экспортированы в 640×640. Не используются CLI/API генерации. Общая часть prompt: single original fictional game avatar for rainy coastal small-town romantic mystery, polished semi-realistic 3D game render, lifelike skin and fabric, soft studio lighting, centered front-facing chest-up square cutout, full head visible, genuine transparent background, no text, logos or props.

Индивидуальные части prompt:

- `evening-eduard-v10.png`: male vampire appearing 18–20, clearly masculine angular face, firm square jaw, pale ivory skin, short swept-back bronze-brown hair, amber eyes, clean-shaven, reserved intense expression, charcoal wool coat over dark blue shirt, no earrings or neon clothes.
- `evening-yakov-v10.png`: white fair-skinned young man 18–20, medium-length straight dark brown hair, brown eyes, warm masculine face, clean-shaven, faded dark red plaid overshirt over charcoal T-shirt, young auto mechanic living with his father by the sea, ordinary human, no wolf attributes or supernatural features.
- `evening-man-1-v10.png`: fair freckled young man 18–20, short sandy blond hair, blue-gray eyes, clean-shaven, calm thoughtful expression, forest-green rain jacket over gray crewneck.
- `evening-man-2-v10.png`: young man 20, light olive skin, short straight black hair, brown eyes, clean-shaven sturdy masculine face, quiet friendly expression, navy weatherproof jacket over burgundy crewneck.
- `evening-woman-1-v10.png`: fair young woman 18–20, long straight chestnut hair, gray-green eyes, attentive curious expression, slate-blue raincoat over oatmeal cable knit sweater.
- `evening-woman-2-v10.png`: fair freckled young woman 20, short wavy auburn bob, hazel eyes, perceptive friendly expression, dark plum raincoat over charcoal knit sweater.

У готовых образов своего героя нет очков, шапок или украшений. Настройка очков удалена по прямому указанию владельца продукта 11.11.2026. Набор выбирается по templateId, другие миры сохраняют прежние образы.

`eveningAppearance.ts` задаёт текст внешности, согласованный с изображениями. Для нового прохождения он передаётся существующим createWorld API в firstContext, rules и stars[].about. В сюжетной роли сохраняется исходная биография NPC вместо замены одной целью. Выбранный готовый образ своего героя также получает текстовое описание; загруженному фото внешность автоматически не приписывается. Результат конструктора остаётся локальным изображением, как до этой правки.

Проверка: TypeScript, production build, secret scan, diff --check; прозрачность всех шести PNG; загрузка файлов и обновлённых модулей с DEV. Реальный AI-ответ и визуальное прокликивание в браузере этой итерацией не проверены: подключение браузерного runtime недоступно. Старые прохождения не переписывались.

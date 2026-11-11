# «Спасти Хогвартс»: портреты, 11.11.2026

Refs #205. По замечаниям к DEV: готовые герои должны сохранять узнаваемые черты, а свои образы — принадлежать этому миру, без современных курток.

Встроенный imagegen, семь отдельных PNG с прозрачностью, уменьшены до 640 px по длинной стороне. Обложка мира сохранена. После проверки прозрачности Гарри получил отдельный PNG из обложки, см. уточнение ниже.

## Общий промпт

Use case: stylized-concept. One illustrated fantasy game character portrait, square, centered bust including complete hair and shoulders with generous margins, true transparent background. Clearly non-photorealistic high-end painted character concept art, sophisticated brushwork, soft sculpted forms, textured velvet and parchment, rich dark emerald/burgundy/antique gold palette, magical astronomical accessories. Reference cover guides palette and costume detail only. Original illustrated faces, no real people or actor likeness. Adults. No text, frame, scenery, collage or extra people.

## Персонажи и отдельные промпты

- `hogwarts-hermione-v11.png`: Hermione Granger, fictional adult book heroine, original painted interpretation; thick unruly chestnut curls, brown eyes, alert analytical expression, slightly raised eyebrow; burgundy velvet scholar robe, parchment collar with constellations, brass hourglass pendant.
- `hogwarts-ron-v11.png`: Ron Weasley, fictional adult book character, original painted interpretation; bright ginger tousled hair, blue eyes, long nose, freckles, lopsided mischievous smile; rust-burgundy velvet robe, knitted sweater, parchment collar and simple brass celestial pin.
- `hogwarts-draco-v11.png`: Draco Malfoy, fictional adult book character, original painted interpretation; short sleek platinum hair, icy grey eyes, pointed pale face, proud calculating expression; emerald velvet robe, black silk lapels, silver fastening, restrained serpent embroidery, alchemical parchment collar.
- `hogwarts-man-1-v11.png`: original male wizard scholar, chestnut wavy hair, hazel eyes, freckles, thoughtful welcoming expression; green velvet cloak, parchment-lined collar with constellations, brass astrolabe brooch.
- `hogwarts-man-2-v11.png`: original male wizard, straight ash-blond hair, grey-blue eyes, angular clean-shaven face; navy velvet robe, parchment collar, silver moon brooch and star embroidery.
- `hogwarts-woman-1-v11.png`: original female wizard, dark brown bob, grey-green eyes, calm curious expression; teal velvet robe, sculptural parchment collar, gold crescent earring and celestial clasp.
- `hogwarts-woman-2-v11.png`: original female wizard, long auburn braid, blue eyes, freckles, lively bold expression; plum velvet robe, parchment collar, crystal and brass star clasp.

Состав ролей, цели, навыки и backend не изменены. Загруженные фотографии игрока не подменяются. Старые штатные фотографии готовых героев заменяются при показе. Сохранённый собственный образ игрока не мигрируется: новый набор предлагается при выборе своего героя.

## Проверка

TypeScript и production build, secret scan и diff --check прошли. В изолированном headless Chrome на DEV отрисованы настоящие HeroStudio и AvatarBuilder: восемь источников загружаются, набор своего героя содержит четыре отдельных образа, на 390 и 1280 px нет горизонтального переполнения. Проверена подмена старого штатного портрета Драко и сохранение пользовательского data:image/png. Изображения просмотрены на скриншотах. Игровые данные для проверки не создавались.

## Прозрачность: исправление Гарри, v12

По дополнительному запросу проверены все 25 PNG из рабочего набора аватаров: альфа-канал и визуальное отображение на контрастной клетчатой подложке. Вне силуэтов нет сплошного фона. Непрозрачность одежды в нижних углах не считается фоном.

Гарри больше не использует JPEG обложки как аватар. Встроенным imagegen из обложки подготовлен `hogwarts-harry-v12.png` с настоящей прозрачностью, 427 × 640 px. Обложка не изменялась. При показе заменяется также её путь, сохранённый предыдущей итерацией в персоне; пользовательские загруженные фотографии сохраняются.

Промпт: Use case: background-extraction. Edit target: supplied existing fantasy cover portrait. Remove only the dark green background around the person. Deliver an isolated head-and-shoulders portrait cutout on a genuinely transparent background, including the full hair, parchment collar, burgundy cloak, glasses and hanging jewellery. Preserve the existing illustrated person, facial features, expression, hairstyle, clothing, colours and lighting. Do not redesign the face or add anything. No green rectangular backdrop, no shadow panel, no checkerboard pixels. Frame the bust with a little transparent space around hair and shoulders; no text or border.

Старые JPEG для прежних/отложенных миров остаются историческими файлами, в этот PNG-набор не входят. Загруженные фотографии пользователя не обрабатывались.

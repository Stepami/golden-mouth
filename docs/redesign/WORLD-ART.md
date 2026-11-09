# Художественные обложки миров · v8

Задача #205 · 08.11.2026. Новая серия по запросу владельца продукта: артистичные крупные планы по мотивам исходных обложек Gorpcore и Harry Potter. Эти обложки остаются в каталоге; новая серия применяется к четырём серверным пресетам. «Спасти Хогвартс» использует иллюстрацию своего пресета «Школа чародеев». Свои миры без известного пресета сохраняют нейтральную обложку.

Использован встроенный image_gen. Исходные иллюстрации из каталога переданы как визуальные референсы; новые изображения сгенерированы отдельно. Это редакционные портреты для атмосферы мира, не новые игровые персонажи. Панорамная проба не используется.

Экспорт: JPEG, 480×720 для списка и 960×1440 для входа в мир; только изменение размера и сжатие через ffmpeg. Совокупный вес восьми файлов — около 1,65 МБ. Карточки загружаются лениво, полноразмерный портрет — при входе. Реестр связей: `frontend/src/app/worldArt.ts`.

## Айти-тусовка

- [Карточка 480](../../frontend/public/assets/worlds/it-portrait-480-v8.jpg)
- [Портрет 960](../../frontend/public/assets/worlds/it-portrait-960-v8.jpg)

Финальный запрос:

```text
Use case: photorealistic-natural. Asset: artistic portrait cover for a mobile social roleplaying world, vertical 2:3 full bleed. The two supplied images are STYLE REFERENCES ONLY: borrow their bold close-up editorial framing, tactile fashion detail, eccentric art direction, dark solid backgrounds and strong face/wardrobe silhouette. Create a NEW composition and face, not a collage or duplicated reference. One adult person in a very tight head-and-shoulders crop, the head and sculptural styling occupy most of the image. Fashion magazine editorial photography with surreal but physically plausible styling, direct flash mixed with sculpted light, finely resolved skin and fabric, subtle grain. Express the world through a striking portrait rather than scenery. Rich decisive colors, premium contemporary image, visually legible as a tiny cover. Lower fifth quieter for separate UI title. No text, letters, logo, border, UI, watermark, landscape vista, castle exterior, group of people or generic fantasy illustration. An adult young creative technologist with short dark hair in a tight profile portrait, wearing sculptural translucent cobalt wraparound safety glasses with warm amber reflections, lime technical nylon hood and a chunky ribbed charcoal collar. A few fine orange cable-like cords integrated into the hood, tactile absurdist startup-world fashion. Cobalt and acid-lime against a deep petrol plain backdrop. Charming, confident, ironic, photographically real; no brand logos, no laptop, no generic office.
```

## Вечерело

- [Карточка 480](../../frontend/public/assets/worlds/evening-portrait-480-v8.jpg)
- [Портрет 960](../../frontend/public/assets/worlds/evening-portrait-960-v8.jpg)

Финальный запрос:

```text
Use case: photorealistic-natural. Asset: artistic portrait cover for a mobile social roleplaying world, vertical 2:3 full bleed. The two supplied images are STYLE REFERENCES ONLY: borrow their bold close-up editorial framing, tactile fashion detail, eccentric art direction, dark solid backgrounds and strong face/wardrobe silhouette. Create a NEW composition and face, not a collage or duplicated reference. One adult person in a very tight head-and-shoulders crop, the head and sculptural styling occupy most of the image. Fashion magazine editorial photography with surreal but physically plausible styling, direct flash mixed with sculpted light, finely resolved skin and fabric, subtle grain. Express the world through a striking portrait rather than scenery. Rich decisive colors, premium contemporary image, visually legible as a tiny cover. Lower fifth quieter for separate UI title. No text, letters, logo, border, UI, watermark, landscape vista, castle exterior, group of people or generic fantasy illustration. A mysterious adult woman in an extreme close portrait, direct gaze, dark bob hair, translucent rain veil draped across one side of the face, sculptural oxblood scarf and one red cherry earring. Editorial neo-noir mystery and late-night romance: deep burgundy, black, wet silver highlights, emerald darkness. Single sharp warm flash across the eyes, dark plain backdrop. Intriguing, tangible and intimate, not horror.
```

## Игра домов

- [Карточка 480](../../frontend/public/assets/worlds/houses-portrait-480-v8.jpg)
- [Портрет 960](../../frontend/public/assets/worlds/houses-portrait-960-v8.jpg)

Финальный запрос:

```text
Use case: photorealistic-natural. Asset: artistic portrait cover for a mobile social roleplaying world, vertical 2:3 full bleed. The two supplied images are STYLE REFERENCES ONLY: borrow their bold close-up editorial framing, tactile fashion detail, eccentric art direction, dark solid backgrounds and strong face/wardrobe silhouette. Create a NEW composition and face, not a collage or duplicated reference. One adult person in a very tight head-and-shoulders crop, the head and sculptural styling occupy most of the image. Fashion magazine editorial photography with surreal but physically plausible styling, direct flash mixed with sculpted light, finely resolved skin and fabric, subtle grain. Express the world through a striking portrait rather than scenery. Rich decisive colors, premium contemporary image, visually legible as a tiny cover. Lower fifth quieter for separate UI title. No text, letters, logo, border, UI, watermark, landscape vista, castle exterior, group of people or generic fantasy illustration. An adult noble with silver-blond hair and an icy intent gaze in tight three-quarter portrait. Oversized sculptural collar built of layered aged silver scales resembling dragon armor and heavy charcoal wool; a small piece of dark carved horn swept beside the temple, frost catching on the metal. Northern dynastic intrigue expressed through fashion. Cool slate background, metallic pewter, pale hair, restrained warm skin. Elegant raw editorial flash photograph, no sword, no castle panorama.
```

## Школа чародеев / Спасти Хогвартс

- [Карточка 480](../../frontend/public/assets/worlds/sorcerers-portrait-480-v8.jpg)
- [Портрет 960](../../frontend/public/assets/worlds/sorcerers-portrait-960-v8.jpg)

Финальный запрос:

```text
Use case: photorealistic-natural. Asset: artistic portrait cover for a mobile social roleplaying world, vertical 2:3 full bleed. The two supplied images are STYLE REFERENCES ONLY: borrow their bold close-up editorial framing, tactile fashion detail, eccentric art direction, dark solid backgrounds and strong face/wardrobe silhouette. Create a NEW composition and face, not a collage or duplicated reference. One adult person in a very tight head-and-shoulders crop, the head and sculptural styling occupy most of the image. Fashion magazine editorial photography with surreal but physically plausible styling, direct flash mixed with sculpted light, finely resolved skin and fabric, subtle grain. Express the world through a striking portrait rather than scenery. Rich decisive colors, premium contemporary image, visually legible as a tiny cover. Lower fifth quieter for separate UI title. No text, letters, logo, border, UI, watermark, landscape vista, castle exterior, group of people or generic fantasy illustration. An adult young wizard with tousled dark hair and delicate round wire glasses, gazing slightly upward. An extravagant collar of translucent folded parchment and tiny silver charms rises around the face, one delicate glass star reflected beside the eye. Burgundy velvet uniform, ivory parchment, dark evergreen backdrop, poetic British magic academy mood. Hands not shown. The face fills the upper half; brave, curious, slightly rebellious. No imitation of a real actor's exact face.
```

# Портреты v9

11.11.2026. Созданы встроенным imagegen по запросу владельца продукта. Референс стиля — существующий `sage-v8.png`; его личность не копируется. Финальные PNG уменьшены до 640 px для интерфейса. Ника и остальные ранее утверждённые портреты сохранены; Меркантил заменён после дополнительного комментария владельца продукта.

Общий prompt для каждого нового портрета: single original game avatar, polished semi-realistic 3D character render, realistically detailed skin and fabric, soft studio lighting, centered front-facing chest-up cutout in square frame, genuine transparent background, no text, logos or props. Match reference visual style only; use a distinct fictional identity.

Дополнения к prompt:

- `burger-v9.png`: adult male ironic tech commentator in his early thirties, short messy dark brown hair, light beard and stubble, rectangular masculine face, amused skeptical expression, dark hoodie over muted orange shirt.
- `kulyash-v9.png`: male skeptical tech investigator about thirty-five, angular face, straight short dark hair with slight receding hairline, short neat beard, dark rectangular glasses, serious inquisitive expression, navy work jacket over gray tee.
- `custom-man-1-v9.png`: fair-skinned man about twenty-seven, short sandy blond hair, clean shaven strong jaw, relaxed confident half smile, muted forest green overshirt over charcoal tee. No glasses, jewelry or hat.
- `custom-man-2-v9.png`: fair light-skinned man about thirty-two, subtle freckles, short dark brown slightly wavy hair, short neat beard, sturdy masculine face, calm gaze, dark burgundy zip jacket over black tee. No glasses, jewelry or hat. This is the selected replacement of the discarded first variant.
- `custom-woman-1-v9.png`: fair-skinned woman about twenty-six, straight shoulder-length dark blond hair tucked behind ears, brown eyes, calm attentive expression, muted blue denim jacket over cream tee. No glasses, jewelry or hat.
- `custom-woman-2-v9.png`: fair-skinned woman about twenty-nine, soft freckles, short wavy copper bob, hazel green eyes, expressive eyebrows, slightly mischievous smile, charcoal casual jacket over muted plum crewneck. No glasses, jewelry or hat.

Only the four custom portraits are constructor bases. `avatarBuilder.ts` composes a base and glasses on a transparent background. Eye anchor coordinates are calibrated to these exports; changing a base requires checking the anchors. The constructor does not change hairstyle or generate new facial geometry. Source templates and discarded generated variants are not runtime assets.

- `merkantil-v9.png`: fair light-skinned adult European man about thirty-four, assertive Russian infobusiness course seller, slicked-back medium brown hair, short neat beard, light gray eyes, confident slightly smug expression, charcoal tailored blazer over black turtleneck. Same polished semi-realistic 3D cutout style. No glasses, jewelry, text, logos or props. Reference style: custom-man-2-v9.png, with a distinct fictional identity. Generated with built-in imagegen and exported as 640 px PNG.

# 14.10.2026: Default for AI stars, optional local avatar personalization for the player.
avatar_personalization = '<label style="display:flex;gap:10px;align-items:center;margin:14px 0 6px"><input type="checkbox" checked style="width:18px;height:18px;accent-color:#9B6BFF"><span>Персонализация аватарки</span></label><p class="small muted">Учитывает обращение и возраст. Если выключить, портрет зависит только от ника и стиля мира.</p>'

S['01-4_channel'] = release_page('Персонаж · 1 / 2', '<h1>Кем ты будешь в этой истории?</h1><p class="muted">Свой персонаж или герой мира — история будет идти от твоего лица.</p><div class="card">✦ Свой персонаж　✓</div><p class="small muted">Герои мира: Изабелла · Эдуард · Яков</p>' + release_field('Имя персонажа', 'Моя история') + release_field('Кто ты в этом мире', 'Автор хроники') + '<p class="small muted">Как к тебе обращаться</p><span class="chip on">она</span><span class="chip">он</span><span class="chip">по нику</span><details class="card" open style="margin-top:16px"><summary>Моя история · @evening.story · Изменить</summary>' + release_field('Ник', 'evening.story') + avatar_personalization + '</details>', 'Выбрать градус драмы')
# Compact the screenshot so the expanded portrait controls are visible.
S['01-4_channel'] = S['01-4_channel'].replace('<span class="chip" style="margin:6px 0">Очистить</span>', '').replace('<h1>', '<h1 style="font-size:24px;margin:0 0 8px">').replace('<p class="muted">Свой персонаж или герой мира — история будет идти от твоего лица.</p>', '').replace('<p class="small muted">Герои мира: Изабелла · Эдуард · Яков</p>', '')

settings_marker = '🙂 Как к тебе обращаться</div><span class="muted">она ›</span></div>'
assert settings_marker in S['05-8_settings']
S['05-8_settings'] = S['05-8_settings'].replace(settings_marker, settings_marker + avatar_personalization + '<p class="small muted">Выбор сохраняется на этом устройстве.</p>', 1)

S['05-12_world_advanced'] = S['05-12_world_advanced'].replace('<summary>Твой персонаж</summary>', '<summary>Твой персонаж</summary>' + avatar_personalization, 1)
S['05-12_world_advanced'] = S['05-12_world_advanced'].replace('<details class="card" style="margin:16px 0;padding:18px"><summary>Твой персонаж', '<details class="card" open style="margin:16px 0;padding:18px"><summary>Твой персонаж', 1)

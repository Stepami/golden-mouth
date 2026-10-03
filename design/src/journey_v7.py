# Правки фреймов v7 (03.10.2026): итоговый аудит экранов против docs/game-flow.md.

def subk7(k, a, b):
    assert a in S[k], (k, a[:60]); S[k] = S[k].replace(a, b, 1)

# Открытые на старте миры: Сцена, Академия, Айти-тусовка (K-pop, аниме, IT); Стрим на юмор 4.
# Иначе подруга из 01-1 / 03-4 / 04-4 не может играть в Академии с первого дня.
WORLDS7 = [("🎤", "Сцена", "Айдол-агентство: трейни, дебют, фансайты", 1, "мало провалов · хейт с 5k · ценят смелость", None),
           ("🌸", "Академия", "Аниме-школа: клубы, соперницы, фестиваль", 2, "рост ×1,1 · хейт с 3k · ценят «в тему»", None),
           ("💻", "Айти-тусовка", "Блогеры, разоблачения, коалиции, курсы", 3, "рост ×1,25 · хейт с 1k · больше риска", None),
           ("🎮", "Стрим", "Стримеры, донаты, рейды, турниры", 2, "рост ×1,1 · ценят юмор", "😂 юмор 4")]
_old = S['01-3_world']; _a = _old.index('<div class="card" style="display:flex;gap:12px;align-items:center;margin-bottom:9px'); _b = _old.index('<div class="card" style="display:flex;gap:12px;align-items:center;padding:11px 14px;border:1px dashed')
cards = ''
for i, (e, n, d, lvl, val, need) in enumerate(WORLDS7):
    st = 'outline:2px solid #FF6B8B;' if i == 0 else ('opacity:.6;' if need else '')
    right = f'<div class="small" style="color:#FFD66B;text-align:right;flex:none">🔒 {need}</div>' if need else ''
    cards += (f'<div class="card" style="display:flex;gap:12px;align-items:center;margin-bottom:9px;padding:11px 14px;{st}"><div style="font-size:28px">{e}</div>'
              f'<div style="flex:1"><b style="font-size:16px">{n}</b> <span style="margin-left:4px">{dots(lvl)}</span><div class="small muted">{d}</div>'
              f'<div class="small" style="color:#FF6B8B;margin-top:2px">{val}</div></div>{right}</div>')
S['01-3_world'] = _old[:_a] + cards + '\n' + _old[_b:]
subk7('05-5_collection', 'Мир «Академия»', 'Мир «Стрим»')
subk7('04-2b_plus_push', 'все миры сразу, без 10k', 'все миры сразу, без прокачки')

# Заруба: счёт по качеству постов, а не охват на подписчика
subk7('03-4c_challenge', 'СИЛА ПОСТОВ · охват на одного подписчика', 'СЧЁТ · оценка постов + рост к своей норме')
subk7('03-4c_challenge', 'Считаем по силе постов, поэтому шансы равные', 'Считаем качество постов, а не подписчиков, поэтому шансы равные')
subk7('03-4_friends', 'Считаем, как залетают посты, а не число подписчиков', 'Считаем качество постов, а не число подписчиков')

# Род на общих карточках и в чужих постах
subk7('05-6_stats_card', '>ПЕРЕЖИЛА<', '>ПЕРЕЖИТО<')
subk7('05-6_stats_card', '🌩 хейт-волну фансайта', '🌩 хейт-волна фансайта')
subk7('03-3_feed', 'который ты не выложила.', 'который так и не выложен.')

# Навыки уровнями: опыт, а не «+12 к смелости»
subk7('05-2b_hate_result', '+12</div><div class="small muted">к смелости', '+12</div><div class="small muted">опыта смелости')
subk7('02-4_flop', 'смелость +1', 'опыт смелости +1')
subk7('03-5_stage', 'Твои шкалы', 'Навыки · ур. 2 из 10')

# Числа
subk7('05-3_morning', '✍️ 7 новых постов', '✍️ 10 новых постов')
S['02-4_flop'] = S['02-4_flop'].replace('>подписчиков</div>', '>подписчика</div>', 1)

# Настройки: градус драмы вместо выключателя, голос канала, обращение
_s = S['05-8_settings']; _i = _s.index('🌩 Хейт-волны'); _row0 = _s.rfind('<div style="display:flex', 0, _i); _row1 = _s.index('</span></div>', _i) + len('</span></div>')
seg = ''.join(f'<span style="flex:1;text-align:center;padding:7px 0;border-radius:9px;font-size:13px;{"background:linear-gradient(90deg,#FF6B8B,#9B6BFF);color:#fff" if i == 1 else "color:#A9A6BD"}">{t}</span>' for i, t in enumerate(["мягко", "средне", "остро", "без хейта"]))
S['05-8_settings'] = _s[:_row0] + (f'<div style="padding:12px 0"><div>🌩 Градус драмы</div><div class="small muted" style="margin:2px 0 8px">Сколько хейта и скандалов в мире. Фильтр 14+ работает всегда</div>'
    f'<div style="display:flex;gap:4px;background:#14141B;border-radius:11px;padding:3px">{seg}</div></div>') + _s[_row1:]
subk7('05-8_settings', '✨ Реки+', '🎙 Голос канала</div><span class="muted">«коротко, с самоиронией» ›</span></div><div style="display:flex;align-items:center;gap:10px;padding:12px 0;border-bottom:1px solid #262634"><div style="flex:1">🙂 Как к тебе обращаться</div><span class="muted">она ›</span></div><div style="display:flex;align-items:center;gap:10px;padding:12px 0;border-bottom:1px solid #262634"><div style="flex:1">✨ Реки+')

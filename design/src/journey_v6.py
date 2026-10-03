# Правки фреймов v6 (03.10.2026) по аудиту геймдизайна (product/audit-gamedesign-sims.md):
# миры отличаются механикой, новый мир без потери подписчиков, 10 уровней навыка, желания дня, «почти залетел».

def subk6(k, a, b):
    assert a in S[k], (k, a[:60]); S[k] = S[k].replace(a, b, 1)

# ---------- 01-3: сложность миров числами ----------
WORLDS6 = [("🎤", "Сцена", "Айдол-агентство: трейни, дебют, фансайты", 1, "мало провалов · хейт с 5k · ценят смелость", None),
           ("🎮", "Стрим", "Стримеры, донаты, рейды, турниры", 2, "рост ×1,1 · хейт с 3k · ценят юмор", None),
           ("🌸", "Академия", "Аниме-школа: клубы, соперницы, фестиваль", 2, "рост ×1,1 · ценят «в тему»", "😂 юмор 4"),
           ("💻", "Айти-тусовка", "Блогеры, разоблачения, коалиции, курсы", 3, "рост ×1,25 · хейт с 1k · больше риска", None)]
cards = ''
for i, (e, n, d, lvl, val, need) in enumerate(WORLDS6):
    st = 'outline:2px solid #FF6B8B;' if i == 0 else ('opacity:.6;' if need else '')
    right = f'<div class="small" style="color:#FFD66B;text-align:right;flex:none">🔒 {need}</div>' if need else ''
    cards += (f'<div class="card" style="display:flex;gap:12px;align-items:center;margin-bottom:9px;padding:11px 14px;{st}"><div style="font-size:28px">{e}</div>'
              f'<div style="flex:1"><b style="font-size:16px">{n}</b> <span style="margin-left:4px">{dots(lvl)}</span><div class="small muted">{d}</div>'
              f'<div class="small" style="color:#FF6B8B;margin-top:2px">{val}</div></div>{right}</div>')
S['01-3_world'] = P(ONB(2) + f'''<div style="padding:4px 20px"><div style="font-size:26px;font-weight:800;margin:4px 0 6px">Где станешь звездой?</div>
<div class="muted" style="margin-bottom:12px">Чем сложнее мир ●, тем больше хейта и провалов, но и рост быстрее</div>
{cards}
<div class="card" style="display:flex;gap:12px;align-items:center;padding:11px 14px;border:1px dashed #9B6BFF77;background:transparent"><div style="font-size:24px">✨</div><div class="small muted"><b style="color:#fff">Свой мир</b> по описанию голосом или текстом. Откроется на 10k или когда друг сделает первый пост по твоей ссылке</div></div></div>{BTN("В Сцену")}''')

# ---------- 02-1: желания дня ----------
WANTS = ''.join(f'<div style="display:flex;gap:10px;align-items:center;padding:7px 0;{"" if i == 2 else "border-bottom:1px solid #262634"}"><span>{"✅" if done else "⬜"}</span><span style="flex:1;{"color:#A9A6BD;text-decoration:line-through" if done else ""}">{t}</span><span class="small" style="color:#C9A8FF">{r}</span></div>'
                for i, (t, r, done) in enumerate([("Пошути про хореографа", "опыт ×1,3", False), ("Ответь Еве в личке", "+отношения", True), ("Пост про свою ошибку", "опыт ×1,3", False)]))
subk6('02-1_compose', '<div class="small muted" style="margin:4px 0 8px">💡 ПОДСКАЖИ</div>',
      f'<div class="card" style="padding:6px 14px;margin:6px 0 10px"><div class="small muted" style="padding-top:6px">ЖЕЛАНИЯ ДНЯ · 1 из 3</div>{WANTS}</div><div class="small muted" style="margin:4px 0 8px">💡 ПОДСКАЖИ</div>')

# ---------- 02-3: «почти залетел» ----------
_r = ribbon("👑", "Мира: отношения +1")
subk6('02-3_result', _r, ribbon("🌊", "Пост почти залетел в Реки", "не хватило 12 репостов: смелее, и залетит") + _r)

# ---------- 05-7: новый мир без потери подписчиков ----------
subk6('05-7_new_world', 'Открыт мир «Академия»', 'Открыта новая линия: Академия')
subk6('05-7_new_world', '🎤 Сцена остаётся: канал, звёзды и коллекция с тобой', '🎤 Канал тот же: все 10 000 подписчиков с тобой, Сцена никуда не делась')
subk6('05-7_new_world', '🚚 Часть фанатов пойдёт за тобой: старт с 1 200, а не со 120', '🌸 Новые звёзды и события Академии приходят в твою ленту рядом со Сценой')
subk6('05-7_new_world', '"Заглянуть в Академию","Остаться в Сцене"', '"Заглянуть в Академию","Позже"') if '"Заглянуть в Академию","Остаться в Сцене"' in S['05-7_new_world'] else None
S['05-7_new_world'] = S['05-7_new_world'].replace('Остаться в Сцене', 'Позже')

# ---------- 05-5: навыки уровнями из 10 ----------
s = S['05-5_collection']
i = s.index('<div class="sc">'); j = s.rindex('</div></div>', 0, s.index('Коллекция 5/12'))
lv = ''.join(f'<div class="sc"><span>{n}</span><div class="bar"><i style="width:{w}%"></i></div><b class="small" style="width:46px;text-align:right">ур. {l}</b></div>'
             for n, l, w in [('смелость', 6, 40), ('юмор', 5, 70), ('в тему', 4, 30)])
S['05-5_collection'] = s[:i] + lv + s[j:]
S['05-5_collection'] = S['05-5_collection'].replace('<b>Шкалы</b>', '<b>Навыки · из 10</b>').replace('откроется на 😂 юмор 2 · сейчас 1,6', 'откроется на 😂 юмор 4')

# Серия дней (research/sim/game_sim4.py): видимая неделя и лестница наград
WEEK = ''.join(f'<div style="flex:1;text-align:center"><div style="height:30px;border-radius:9px;display:flex;align-items:center;justify-content:center;font-size:14px;'
               f'{"background:linear-gradient(135deg,#FF6B8B,#9B6BFF)" if s == 1 else ("border:2px dashed #FF6B8B" if s == 2 else "background:#20202B")}">{"🔥" if s == 1 else ("?" if s == 2 else "")}</div>'
               f'<div class="small muted" style="margin-top:3px">{dn}</div></div>' for dn, s in zip(["пн", "вт", "ср", "чт", "пт", "сб", "вс"], [1, 1, 1, 1, 2, 0, 0]))
_st = S['05-5_collection']; _i = _st.index('<div class="card" style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;margin-bottom:8px"><b>Навыки')
S['05-5_collection'] = _st[:_i] + (f'<div class="card" style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;margin-bottom:8px"><b>Серия 4 дня</b>'
    f'<span class="small" style="color:#FFD66B">7 дней: карточка «Неделя в эфире»</span></div><div style="display:flex;gap:6px">{WEEK}</div></div>') + _st[_i:]
S['05-5_collection'] = S['05-5_collection'].replace('🔥 4</div><div class="small muted">дня подряд', '🎯 1/3</div><div class="small muted">желаний дня', 1)

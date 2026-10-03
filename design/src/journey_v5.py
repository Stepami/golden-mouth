# Правки фреймов v5 (03.10.2026) по комментариям к v4.
# Род: системные тексты нейтральные, AI пишет в форме, которую игрок выбрал в 01-4 (она / он / по нику).
# Заруба 3 дня без штрафа проигравшему. «Поделиться миром» = «Сыграй мою историю»: та же завязка, без переписки.
# В онбординге объяснено, зачем навыки и коллекция. События: понятно, что это пост. Миры разной сложности, открываются навыками.

def subk(k, a, b, n=1):
    assert S[k].count(a) >= n, (k, a[:60]); S[k] = S[k].replace(a, b, n)

# ---------- Род ----------
subk('01-4_channel', '<div class="muted small" style="margin-bottom:8px">ТАК ТЕБЯ',
     '<div class="muted small" style="margin:12px 0 8px">КАК К ТЕБЕ ОБРАЩАТЬСЯ</div>'
     '<div style="margin-bottom:10px"><span class="chip on">она</span><span class="chip">он</span><span class="chip">по нику</span></div>'
     '<div class="muted small" style="margin-bottom:8px">ТАК ТЕБЯ')
subk('01-1_invite', '+5 постов нам обеим', '+5 постов тебе и мне')
subk('02-1_compose', 'я поняла, что…', 'до меня дошло, что…')
h, t = S['03-4_friends'].rsplit('её фанаты увидят', 1); S['03-4_friends'] = h + 'его фанаты увидят' + t  # второй друг Дима
subk('05-2b_hate_result', 'Ты ответила «', 'Твой ответ: «')
subk('05-2b_hate_result', 'Пережила хейт', 'Хейт пережит')
subk('05-2b_hate_result', 'как ты выстояла', 'как держать удар')
subk('04-2b_plus_push', 'Ты просила сказать первой', 'Обещали сказать тебе первым делом')

# ---------- Заруба: 3 дня, проигравший ничего не теряет ----------
subk('03-4c_challenge', 'день 3 из 7', 'день 2 из 3')
subk('03-4c_challenge', '<span class="up">+3 поста в день на неделю</span>', '<span class="up">+3 поста в день на 3 дня</span>')
subk('03-4c_challenge', '<span class="down">−3 поста в день на неделю</span>', '<span class="muted">ничего не теряет · карточка «Реванш»</span>')
subk('03-4_friends', '<b>Заруба на неделю</b>', '<b>Заруба на 3 дня</b>')
subk('03-4_friends', 'Поделиться миром', 'Сыграй мою историю')

# ---------- «Сыграй мою историю» вместо копии мира ----------
subk('04-3_invite', '<b>Позвать в мой мир</b><div class="muted">Друг начнёт с копии: Мира, ваша ссора, твои звёзды. И разрулит по-своему',
     '<b>Сыграй мою историю</b><div class="muted">Друг начнёт с твоей завязки: Сцена, Мира злится на новенького. Твою переписку не видит. Потом сравните, кто как разрулил')

# ---------- Онбординг: зачем навыки и коллекция ----------
subk('01-6_home', '''<div class="card" style="margin-top:12px;display:flex;gap:12px;align-items:center"><div style="font-size:26px">🌊</div><div class="small" style="color:#C9C6D8"><b style="color:#fff">Реки</b> — общая лента, где тебя видят все. Хороший пост может туда залететь, и придут тысячи фанатов</div></div>''',
     '<div class="card small" style="margin-top:12px;color:#C9C6D8;padding:6px 14px">' + ''.join(
         f'<div style="display:flex;gap:12px;align-items:center;padding:8px 0;{"" if i == 2 else "border-bottom:1px solid #262634"}"><div style="font-size:22px">{e}</div><div><b style="color:#fff">{t}</b> {d}</div></div>'
         for i, (e, t, d) in enumerate([
             ("🌊", "Реки", "общая лента. Хороший пост залетает туда, и приходят тысячи фанатов"),
             ("📈", "Навыки", "смелость, юмор, в тему растут от постов и открывают новые приёмы и миры"),
             ("🃏", "Коллекция", "карточки звёзд и моментов. Карточка звезды открывает её характер и личный сюжет")])) + '</div>')

# ---------- События: понятно, что это пост ----------
EV_NOTE = '<div class="small" style="color:#C9A8FF;margin-bottom:8px">Это пост в твоём канале · лимит не тратит. Выбери идею или напиши свой</div>'
subk('05-0_event_collab', '<span class="chip opt2">честно:', EV_NOTE + '<span class="chip opt2">честно:')
subk('05-0_event_collab', '<div class="card muted" style="background:#20202B;margin-top:4px">Своими словами…</div>',
     '<div class="card muted" style="background:#20202B;margin-top:4px">Написать пост своими словами · ×1,5</div>')
subk('05-2_event', '<span class="chip opt2">«я тут ради', '<div class="small" style="color:#C9A8FF;margin-bottom:8px">Ответ уйдёт постом в твой канал · лимит не тратит</div><span class="chip opt2">«я тут ради')
subk('05-2_event', '<div class="card muted" style="background:#20202B;margin-top:4px">Своими словами…</div>',
     '<div class="card muted" style="background:#20202B;margin-top:4px">Ответить своими словами · ×1,5</div>')
subk('03-1_dm', '<div class="card muted" style="flex:1">Своими словами…</div>', '<div class="card muted" style="flex:1">Ответить Мире своими словами…</div>')

# ---------- Миры: разная сложность и что ценят, открываются навыками ----------
def dots(n): return ''.join(f'<i style="display:inline-block;width:7px;height:7px;border-radius:50%;margin-right:3px;background:{"#FF6B8B" if i < n else "#33323F"}"></i>' for i in range(3))
WORLDS5 = [("🎤", "Сцена", "Айдол-агентство: трейни, дебют, фансайты", 1, "фанаты добрые · ценят смелость", None),
           ("🎮", "Стрим", "Стримеры, донаты, рейды, турниры", 2, "чат токсичный · ценят юмор", None),
           ("🌸", "Академия", "Аниме-школа: клубы, соперницы, фестиваль", 2, "ценят «в тему»", "😂 юмор 2"),
           ("💻", "Айти-тусовка", "Блогеры, разоблачения, коалиции, курсы", 3, "хейт с первого дня · ценят смелость", None)]
cards = ''
for i, (e, n, d, lv, val, need) in enumerate(WORLDS5):
    st = 'outline:2px solid #FF6B8B;' if i == 0 else ('opacity:.6;' if need else '')
    right = (f'<div class="small" style="color:#FFD66B;text-align:right;flex:none">🔒 {need}</div>' if need else '')
    cards += (f'<div class="card" style="display:flex;gap:12px;align-items:center;margin-bottom:9px;padding:11px 14px;{st}"><div style="font-size:28px">{e}</div>'
              f'<div style="flex:1"><b style="font-size:16px">{n}</b> <span style="margin-left:4px">{dots(lv)}</span><div class="small muted">{d}</div>'
              f'<div class="small" style="color:#FF6B8B;margin-top:2px">{val}</div></div>{right}</div>')
S['01-3_world'] = P(ONB(2) + f'''<div style="padding:4px 20px"><div style="font-size:26px;font-weight:800;margin:4px 0 6px">Где станешь звездой?</div>
<div class="muted" style="margin-bottom:12px">У каждого мира свой характер и сложность ●. Новые миры открывают навыки</div>
{cards}
<div class="card" style="display:flex;gap:12px;align-items:center;padding:11px 14px;border:1px dashed #9B6BFF77;background:transparent"><div style="font-size:24px">✨</div><div class="small muted"><b style="color:#fff">Свой мир</b> по описанию голосом или текстом. Откроется на 10k или когда друг сделает первый пост по твоей ссылке</div></div></div>{BTN("В Сцену")}''')

subk('05-5_collection', 'откроется на 10 000 · 62%', 'откроется на 😂 юмор 2 · сейчас 1,6')


# Симуляция 03.10 (research/sim): «своими словами» ×1,3 вместо ×1,5, иначе игрок с подсказками отстаёт вдвое
for k in S: S[k] = S[k].replace('×1,5', '×1,3').replace('в 1,5 раза больше', 'на треть больше')

# Циклы 1–2 симуляции (research/sim/README.md): Реки+ +10 постов (итого 20), ценность в мирах и оформлении
subk('04-1_limit', 'Реки+: +20 постов в день ›', 'Реки+: +10 постов и все миры ›')
subk('04-2_fake_door', '("30 постов в день", True)', '("20 постов в день", True)') if '("30 постов в день", True)' in S['04-2_fake_door'] else None
S['04-2_fake_door'] = S['04-2_fake_door'].replace('30 постов в день', '20 постов в день')
subk('04-2b_plus_push', '✍️ +20 постов в день', '✍️ +10 постов в день')

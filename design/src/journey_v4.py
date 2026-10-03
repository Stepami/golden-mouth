# Правки фреймов v4 (02.10.2026) по встрече «Обсуждение MVP: механики, монетизация и план разработки».
# Подключается из build_journey.py после journey_v3.py.
# Главное: мир «Айти-тусовка» в пресетах; «свой мир» описываешь голосом или текстом, правила мира видны и правятся;
# открывается на 10k или когда друг сделал первый пост по твоей ссылке; гонка с друзьями только по вызову;
# Реки+ оформлен тарифом; на лимите звёздам не пишем; служебные пометки про логику событий убраны.

def sub(k, a, b):
    assert a in S[k], (k, a[:60]); S[k] = S[k].replace(a, b, 1)

# ---------- 1. Выбор мира: + Айти-тусовка, свой мир с понятным условием ----------
WORLDS = [("🎤", "Сцена", "Айдол-агентство: трейни, дебют, камбэки, фансайты", "для тех, кто в K-pop"),
          ("🌸", "Академия", "Аниме-школа: клубы, соперницы, фестиваль", "для аниме и манхвы"),
          ("🎮", "Стрим", "Стримеры, донаты, рейды, турниры", "для игр"),
          ("💻", "Айти-тусовка", "Блогеры, разоблачения, коалиции, прогревы курсов", "для тех, кто следит за IT-драмами")]
S['01-3_world'] = P(ONB(2) + f'''<div style="padding:4px 20px"><div style="font-size:26px;font-weight:800;margin:4px 0 6px">Где станешь звездой?</div>
<div class="muted" style="margin-bottom:14px">Мир задаёт звёзд, фанатов и события</div>
{''.join(f'<div class="card" style="display:flex;gap:14px;align-items:center;margin-bottom:10px;padding:12px 14px;{"outline:2px solid #FF6B8B" if i==0 else ""}"><div style="font-size:30px">{e}</div><div><b style="font-size:16px">{n}</b><div class="small muted">{d}</div><div class="small" style="color:#FF6B8B;margin-top:2px">{t}</div></div></div>' for i,(e,n,d,t) in enumerate(WORLDS))}
<div class="card" style="display:flex;gap:14px;align-items:center;padding:12px 14px;border:1px dashed #9B6BFF77;background:transparent"><div style="font-size:26px">🔒</div><div><b>Свой мир</b><div class="small muted">Расскажи голосом или текстом, какой мир хочешь. Откроется на 10k или когда друг сделает первый пост по твоей ссылке</div></div></div></div>{BTN("В Сцену")}''')

# ---------- 2. Свой мир: конструктор (идея из UX почтового клиента: AI под контролем игрока) ----------
def rule(t, on=True):
    return (f'<div style="display:flex;gap:8px;align-items:flex-start;padding:7px 0;border-bottom:1px solid #262634;font-size:14px">'
            f'<span style="color:{"#6BFFB8" if on else "#55536A"}">{"✓" if on else "○"}</span><span style="flex:1">{t}</span><span class="muted small">✎</span></div>')
S['05-7b_own_world'] = P(f'''<div class="top"><span style="font-size:22px">‹</span><b>Свой мир</b><div class="pill">открыт · 10k</div></div>
<div style="padding:14px 16px"><div class="small muted" style="margin-bottom:6px">НАЧНИ С ГОТОВОГО</div>
<div style="margin-bottom:10px"><span class="chip opt2">🏫 моя школа</span><span class="chip">💻 IT-драма</span><span class="chip">🏰 фэнтези-академия</span><span class="chip">⚽ футбольный клуб</span></div>
<div class="card" style="margin-bottom:10px;display:flex;gap:10px;align-items:center"><div style="flex:1;font-size:15px;line-height:1.4">школа как наша, но все учителя ведут блоги, а директор тайно стример. я новенькая в 9Б<span style="color:#FF6B8B">|</span></div>
<div style="width:44px;height:44px;border-radius:50%;background:linear-gradient(135deg,#FF6B8B,#9B6BFF);display:flex;align-items:center;justify-content:center;font-size:20px;flex:none">🎙</div></div>
<div class="card" style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;margin-bottom:4px"><b>Правила мира</b><span class="small" style="color:#C9A8FF">AI собрал из описания</span></div>
<div class="small muted" style="margin-bottom:4px">Звёзды и события следуют им. Поправь любое</div>
{rule("Звёзды: завуч-блогерша, директор-стример, отличница-сплетница")}{rule("Фанаты: весь чат параллели, обожают драму")}{rule("Тон: смешно и по-доброму")}{rule("События: контрольная, дискотека, слив переписки", True)}{rule("Без тем 18+ и травли по внешности", True)}</div>
<div class="small muted" style="text-align:center">Можно дописать голосом: «пусть физрук будет главным злодеем»</div></div>{BTN("Создать мир")}''')

# ---------- 3. Друзья: прогресс виден всем, гонка только по вызову ----------
s = S['03-4_friends']
start = s.index('<div class="card" style="text-align:center;background:linear-gradient(135deg,#2A1A2E,#1E1A33)"><b>Кто быстрее до 10k?</b>')
end = s.index('Позвать ещё друзей</button></div>') + len('Позвать ещё друзей</button></div>')
S['03-4_friends'] = s[:start] + '''<div class="card" style="background:linear-gradient(135deg,#2A1A2E,#1E1A33)"><div style="display:flex;align-items:center;gap:10px"><div style="font-size:26px">⚔️</div><div style="flex:1"><b>Заруба на неделю</b><div class="small muted">Только если оба согласны. Считаем, как залетают посты, а не число подписчиков</div></div></div>
<div style="display:flex;gap:8px;margin-top:10px"><button class="btn" style="padding:10px;font-size:14px;flex:1">Вызвать друга</button><button class="btn btn2" style="padding:10px;font-size:14px;flex:1">Поделиться миром</button></div></div>''' + s[end:]

S['03-4c_challenge'] = P(f'''<div class="top"><span style="font-size:22px">‹</span><b>Заруба</b><div class="pill">день 3 из 7</div></div>
<div style="padding:16px"><div style="display:flex;align-items:center;justify-content:center;gap:18px;margin:6px 0 14px">
<div style="text-align:center">{ava("Л","g1",64,26,"margin:0 auto 6px")}<b>Лиза</b><div class="small muted">🎤 Сцена</div></div><div style="font-size:28px">⚔️</div>
<div style="text-align:center">{ava("К","g5",64,26,"margin:0 auto 6px")}<b>Катя</b><div class="small muted">🌸 Академия</div></div></div>
<div class="card" style="margin-bottom:10px"><div class="small muted" style="margin-bottom:8px">СИЛА ПОСТОВ · охват на одного подписчика</div>
<div class="sc"><span>Лиза</span><div class="bar"><i style="width:68%"></i></div><b style="width:40px;text-align:right">×3,4</b></div>
<div class="sc"><span>Катя</span><div class="bar"><i style="width:55%;background:linear-gradient(90deg,#6BB8FF,#9B6BFF)"></i></div><b style="width:40px;text-align:right">×2,8</b></div>
<div class="small muted" style="margin-top:6px">У Кати 5 120 подписчиков, у тебя 2 310. Считаем по силе постов, поэтому шансы равные</div></div>
<div class="card" style="margin-bottom:10px"><b>Ставка</b><div class="why"><span>🏆 Победитель</span><span class="up">+3 поста в день на неделю</span></div><div class="why" style="border:none"><span>🫠 Проигравший</span><span class="down">−3 поста в день на неделю</span></div></div>
<div class="card small" style="background:#20202B">🔥 Лучший пост дня: Лиза, «зато уверенно» · ×6,1</div></div>{BTN("Новый пост в зарубу")}''')

# ---------- 4. Лимит: звёздам не пишем, Реки+ как тариф ----------
sub('04-1_limit', 'отвечать в тредах · писать звёздам · заглянуть к друзьям · листать ленту',
    'отвечать на комментарии к своим постам · комментировать друзей · листать ленту')
sub('04-1_limit', 'Реки+: 20 постов в день ›', 'Реки+: +20 постов в день ›')

def plan(name, price, rows, hi=False):
    st = 'border:2px solid #FF6B8B;background:linear-gradient(160deg,#2A1A2E,#1A1A24)' if hi else 'border:1px solid #33323F'
    return (f'<div class="card" style="flex:1;{st};padding:12px"><b>{name}</b><div style="font-size:20px;font-weight:900;margin:2px 0 8px">{price}</div>'
            + ''.join(f'<div class="small" style="margin-bottom:5px;{"" if on else "color:#55536A"}">{"✓" if on else "—"} {t}</div>' for t, on in rows) + '</div>')
S['04-2_fake_door'] = P(f'''{HEAD("2 310","0")}<div style="position:absolute;inset:0;background:rgba(8,8,12,.75)"></div>
<div style="position:absolute;left:12px;right:12px;bottom:24px;background:#1A1A24;border-radius:26px;padding:20px;text-align:center">
<div style="font-size:22px;font-weight:900;margin-bottom:4px">✨ Реки+</div><div class="muted small" style="margin-bottom:14px">Для тех, кому 10 постов в день мало</div>
<div style="display:flex;gap:8px;text-align:left;margin-bottom:12px">
{plan("Бесплатно", "0 ₽", [("10 постов в день", True), ("+5 за друга", True), ("свой стиль канала", False), ("все миры сразу", False)])}
{plan("Реки+", "349 ₽/мес", [("30 постов в день", True), ("+5 за друга", True), ("свой стиль канала", True), ("все миры сразу", True)], True)}</div>
<div class="muted small" style="margin-bottom:12px">Реки+ ещё в работе, деньги не списываются. Нажми, и мы напишем в боте, когда откроем</div>
<div class="card" style="margin-bottom:12px;background:linear-gradient(135deg,#1F2A26,#1A1A24);border:1px solid #6BFFB855"><b class="up">А пока держи +5 постов</b></div>
<button class="btn" style="margin-bottom:8px">Хочу Реки+, напишите мне</button><div class="muted small">Забрать только +5 постов</div></div>''')

S['04-2b_plus_push'] = TG('Реки', 'бот', 'g2', 'Р', '''<div class="small" style="text-align:center;color:#6C8EA8;margin-bottom:10px">сегодня</div>
<div class="tgb">✨ <b>Реки+ открыт.</b> Ты просила сказать первой<br><br>✍️ +20 постов в день<br>🎨 свой стиль канала: фон, рамка аватарки, шрифт<br>🌍 все миры сразу, без 10k<br><br>Первая неделя 99 ₽, дальше 349 ₽ в месяц. Отменить можно в настройках</div>
<div class="tgbtn">Подключить Реки+</div><div class="tgbtn">Не сейчас</div>
<div class="small" style="color:#6C8EA8;margin-top:8px">12:10</div>''', '12:10')

# ---------- 4. Приглашение: друг открывает «свой мир» и может начать с копии твоего ----------
sub('04-3_invite', '<div class="small" style="color:#C9C6D8;margin-bottom:12px;text-align:center">Вернётся завтра: карточка «Дуэт» для вас обоих</div>',
    '<div class="card" style="margin-bottom:12px;display:flex;gap:10px;align-items:center"><div style="font-size:22px">🔓</div><div class="small" style="flex:1"><b>Первый друг откроет «Свой мир»</b><div class="muted">не нужно ждать 10k</div></div></div>'
    '<div class="card" style="margin-bottom:12px;display:flex;gap:10px;align-items:center"><div style="flex:1" class="small"><b>Позвать в мой мир</b><div class="muted">Друг начнёт с копии: Мира, ваша ссора, твои звёзды. И разрулит по-своему</div></div>' + toggle(True) + '</div>')

# ---------- 5. События: без служебных пометок, порядок случайный ----------
sub('05-0_event_collab', 'События по кругу: коллаб, бренд, сплетня, «отмена». Хейт-волна только с 5k',
    'Справишься: карточка момента и +смелость')

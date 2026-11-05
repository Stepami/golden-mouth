# Макеты пути игрока v2 по шагам сценария (product/mvp-journey.md), мир «Сцена», игрок Лиза.
# Каждый шаг отдельным PNG в docs/screens/. Запуск: python3 build_journey.py
import os, shutil, subprocess, tempfile, time
D = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(D, 'build.py'), encoding='utf-8').read().split('S={}')[0])  # CSS, page, ava из общих макетов
OUT = os.path.join(os.path.dirname(os.path.dirname(D)), 'screens'); os.makedirs(OUT, exist_ok=True)
AVD = 'file://' + os.path.join(os.path.dirname(D), 'avatars') + '/'
# Аватарки персонажей (CC0, см. ../avatars/README.md). Буква из макета -> файл
AV = {'Л': 'pick0', 'М': 'mira', 'С': 'sonya', 'Е': 'eva', 'Ю': 'yuna', 'К': 'katya', 'Д': 'dima', 'к': 'dasha', 'х': 'critic'}
_ava_letter = ava
def ava(t, g, s=40, fs=15, ex=""):
    if t in AV:
        src = 'file://' + os.path.join(os.path.dirname(D), 'avatars', AV[t] + '.svg')
        return f'<img src="{src}" style="width:{s}px;height:{s}px;border-radius:50%;flex:none;object-fit:cover;display:block;{ex}">'
    return _ava_letter(t, g, s, fs, ex)

EXTRA = '''
.bar{height:6px;background:#262634;border-radius:3px;overflow:hidden}.bar i{display:block;height:6px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF)}
.sc{display:flex;align-items:center;gap:8px;font-size:12px;margin-bottom:6px}.sc span{width:70px;color:#C9C6D8}.sc .bar{flex:1}
.why{display:flex;justify-content:space-between;padding:7px 0;border-bottom:1px solid #262634;font-size:14px}
.slot{aspect-ratio:3/4;border-radius:12px;display:flex;align-items:center;justify-content:center;font-size:22px}
.gift{background:linear-gradient(90deg,#FFD66B,#FFB86B);color:#1A1A24;font-weight:800;border-radius:12px;padding:9px 12px;font-size:13px;line-height:1.3}
.badge{display:inline-block;font-size:11px;border-radius:10px;padding:2px 8px;background:#2A2236;color:#FFB86B}
.step{position:absolute;top:50px;right:14px;font-size:10px;letter-spacing:.06em;color:#55536A}
.tg{background:#0E1621}.tgb{background:#182533;border-radius:16px;border-bottom-left-radius:6px;padding:10px 12px;max-width:86%;margin-bottom:6px}
.small{font-size:13px}.muted{color:#A9A6BD}.sc{font-size:13px}
.opt{border:1px solid #9B6BFF66;background:#1C1A26;border-radius:14px;padding:12px 14px;margin-bottom:8px}
.opt2{border:1px solid #9B6BFF88 !important;background:#1F1A2B !important}
.tgbtn{background:#1F2F3F;color:#8CC8FF;text-align:center;border-radius:10px;padding:10px;font-size:14px;margin-top:5px;max-width:86%}
'''
def P(body, t='21:14'): return page(body).replace('</style>', EXTRA + '</style>').replace('21:14', t)
TAB5 = '<div class="tab">' + ''.join(f'<div class="{{{i}}}"><b>{ic}</b>{t}</div>' for i, (ic, t) in enumerate(
    [('▦', 'Канал'), ('≋', 'Лента'), ('✉', 'Личка'), ('☺', 'Друзья'), ('★', 'Я')])) + '</div>'
def tab5(i): c = [''] * 5; c[i] = 'on'; return TAB5.format(*c)
def scales(a, b, c): return ''.join(f'<div class="sc"><span>{n}</span><div class="bar"><i style="width:{v}%"></i></div></div>' for n, v in [('смелость', a), ('юмор', b), ('в тему', c)])
def HEAD(subs, posts): return f'<div class="top">{ava("Л","g1",36,15)}<div><b>Лиза · трейни</b><div class="muted small">{subs} подписчиков · 🎤 Сцена</div></div><div class="pill">✍️ осталось {posts}</div></div>'
def BTN(t, second=None):
    sec = '<button class="btn btn2" style="margin-bottom:10px">' + second + '</button>' if second else ''
    return f'<div style="position:absolute;bottom:34px;left:20px;right:20px">{sec}<button class="btn">{t}</button></div>'
def cmt(a, g, n, h, t, star=False): return f'<div style="display:flex;gap:10px;margin-bottom:13px">{ava(a,g,34,14)}<div style="flex:1"><b>{n}</b> <span class="muted small">{h}</span>{" <span class=small style=color:#FF6B8B>★ звезда</span>" if star else ""}<span class="muted" style="float:right">⋯</span><div style="margin-top:2px">{t}</div></div></div>'
def bub(t, me=False): return f'<div style="display:flex;justify-content:{"flex-end" if me else "flex-start"};margin-bottom:8px"><div style="max-width:78%;padding:9px 12px;border-radius:18px;{"background:linear-gradient(135deg,#FF6B8B,#9B6BFF);border-bottom-right-radius:6px" if me else "background:#1E1E29;border-bottom-left-radius:6px"}">{t}</div></div>'
def TG(name, sub, g, letter, body, t='21:14'):
    return P(f'<div class="top tg" style="border-color:#1A2533"><span style="font-size:22px;color:#8CC8FF">‹</span>{ava(letter,g,36,15)}<div><b>{name}</b><div class="small" style="color:#6C8EA8">{sub}</div></div></div><div class="tg" style="position:absolute;top:96px;bottom:0;left:0;right:0;padding:16px">{body}</div>', t)
def stats_card(scale=1):
    return f'<div style="background:linear-gradient(160deg,#2A1236,#141420 60%,#12203A);border-radius:16px;padding:14px;text-align:center"><div class="brand" style="font-size:16px">Реки</div><div class="small" style="color:#C9C6D8;margin-top:2px">симс, только ты блогер</div>{ava("К","g5",44,18,"margin:8px auto 6px")}<b>Катя · ахаха</b><div style="font-size:30px;font-weight:900;background:linear-gradient(90deg,#FF6B8B,#FFB86B);-webkit-background-clip:text;color:transparent">5 120</div><div class="small">подписчиков за 3 дня</div><div class="small muted" style="margin-top:4px">мир 🌸 Академия: аниме-школа</div><div class="small" style="margin-top:6px">а ты кем будешь?</div></div>'

S = {}
# ---------- 1. Вход ----------
S['01-1_invite'] = TG('Катя', 'в сети', 'g5', 'К', f'''<div class="tgb">нашла игру: как симс, только ты блогер. ведёшь свой канал, а AI-звёзды и фанаты реагируют на каждый пост. я уже 5к, го наперегонки</div>
<div class="tgb" style="padding:8px">{stats_card()}</div><div class="tgbtn">▶ Играть в Реки · +5 постов нам обеим</div><div class="small" style="color:#6C8EA8;margin-top:6px">21:12 ✓✓</div>''')

S['01-2_age'] = P(f'''<div style="padding:18px 20px"><div class="brand" style="font-size:26px">Реки</div>
<div style="font-size:24px;font-weight:800;margin:14px 0 6px">Год рождения</div><div class="muted" style="margin-bottom:10px">Чтобы звёзды говорили с тобой как надо</div>
<div style="position:relative;height:266px;overflow:hidden;text-align:center;background:#14141B;border-radius:18px"><div style="position:absolute;left:12px;right:12px;top:114px;height:38px;border-radius:10px;background:#22222D"></div><div style="position:relative"><div style="height:38px;line-height:38px;font-size:18px;color:#55536A">2007</div><div style="height:38px;line-height:38px;font-size:18px;color:#55536A">2008</div><div style="height:38px;line-height:38px;font-size:18px;color:#55536A">2009</div><div style="height:38px;line-height:38px;font-size:22px;font-weight:800;color:#fff">2010</div><div style="height:38px;line-height:38px;font-size:18px;color:#55536A">2011</div><div style="height:38px;line-height:38px;font-size:18px;color:#55536A">2012</div><div style="height:38px;line-height:38px;font-size:18px;color:#55536A">2013</div></div>
<div style="position:absolute;inset:0;background:linear-gradient(#14141B,transparent 35%,transparent 65%,#14141B)"></div></div>
<div class="card" style="margin-top:18px"><b>Сохраним твою игру?</b><div class="small muted" style="margin:6px 0 10px">Храним только Telegram ID и игру, сервер в России. Удалить всё можно в профиле в один тап. <span style="color:#8CC8FF">Подробнее</span></div>
<div style="display:flex;gap:8px;align-items:center"><div style="width:22px;height:22px;border-radius:6px;background:linear-gradient(135deg,#FF6B8B,#9B6BFF);text-align:center;font-size:14px">✓</div><span class="small">Согласие на обработку данных</span></div></div></div>{BTN("Дальше")}''')

S['01-3_world'] = P(f'''<div style="padding:18px 20px"><div style="font-size:26px;font-weight:800;margin:6px 0 6px">Где станешь звездой?</div>
<div class="muted" style="margin-bottom:18px">Мир задаёт звёзд, фанатов и события. Другие откроются позже</div>
{''.join(f'<div class="card" style="display:flex;gap:14px;align-items:center;margin-bottom:12px;{"outline:2px solid #FF6B8B" if i==0 else ""}"><div style="font-size:34px">{e}</div><div><b style="font-size:17px">{n}</b><div class="small muted">{d}</div><div class="small" style="color:#FF6B8B;margin-top:3px">{t}</div></div></div>' for i,(e,n,d,t) in enumerate([("🎤","Сцена","Айдол-агентство: трейни, дебют, камбэки, фансайты","для тех, кто в K-pop"),("🌸","Академия","Аниме-школа: клубы, соперницы, фестиваль","для аниме и манхвы"),("🎮","Стрим","Стримеры, донаты, рейды, турниры","для игр")]))}
<div class="card" style="display:flex;gap:14px;align-items:center;opacity:.55"><div style="font-size:30px">✨</div><div><b>Свой мир по мотивам</b><div class="small muted">Придумай вселенную и персонажей сама · скоро</div></div></div></div>{BTN("В Сцену")}''')

S['01-4_channel'] = P(f'''<div style="padding:18px 20px"><div style="font-size:24px;font-weight:800;margin:6px 0 6px">Кем будешь в Сцене?</div><div class="muted" style="margin-bottom:14px">Поменять можно позже</div>
<span class="chip on">🎤 Трейни</span><span class="chip">📸 Фансайт</span><span class="chip">💃 Хореограф</span><span class="chip">🎧 Продюсер</span>
<div style="margin:12px 0 8px" class="muted small">АВАТАРКА</div><div style="display:flex;gap:10px;align-items:center;margin-bottom:4px"><img src="{AVD}pick0.svg" style="width:46px;height:46px;border-radius:50%;outline:3px solid #FF6B8B;outline-offset:2px"><img src="{AVD}pick4.svg" style="width:46px;height:46px;border-radius:50%;opacity:.85"><img src="{AVD}pick7.svg" style="width:46px;height:46px;border-radius:50%;opacity:.85"><img src="{AVD}pick8.svg" style="width:46px;height:46px;border-radius:50%;opacity:.85"><img src="{AVD}pick10.svg" style="width:46px;height:46px;border-radius:50%;opacity:.85"><div style="width:46px;height:46px;border-radius:50%;background:#22222D;display:flex;align-items:center;justify-content:center;font-size:20px">🎲</div></div><div style="margin:12px 0 8px" class="muted small">НАЗВАНИЕ И НИК</div><div class="card" style="margin-bottom:8px">Лиза · трейни</div><div class="card" style="margin-bottom:16px">@liza.trainee</div>
<div class="muted small" style="margin-bottom:8px">ТАК ТЕБЯ УВИДЯТ ФАНАТЫ</div>
<div class="card" style="display:flex;gap:12px;align-items:center">{ava("Л","g1",48,20)}<div><b>Лиза · трейни</b><div class="muted small">@liza.trainee · 120 подписчиков</div><div class="small" style="margin-top:4px">первый год в агентстве. путаю право и лево, но очень стараюсь ✨ <span class="muted">(дописал AI)</span></div></div></div></div>{BTN("Дальше")}''')

stars = [('М','g1','Мира','лидер BLESK','Дерзкая','Говорит, что думает. Может раскритиковать, может сделать звездой'),
         ('С','g4','Соня','вокал BLESK','Добрая','Поддержит любой пост и придёт с фанатами'),
         ('Е','g3','Ева','солистка NOIR','Загадочная','Пишет редко. Если написала, это событие')]
S['01-5_first_star'] = P(f'''<div style="padding:18px 20px"><div style="font-size:24px;font-weight:800;margin:6px 0 6px">Кто подпишется первой?</div><div class="muted" style="margin-bottom:18px">Звёзды Сцены вымышленные, у каждой свой характер</div>
{''.join(f'<div class="card" style="display:flex;gap:12px;margin-bottom:12px;{"outline:2px solid #FF6B8B" if i==0 else ""}">{ava(a,g,52,22)}<div><b>{n}</b> <span class="muted small">{h}</span><div class="small" style="color:#FF6B8B;margin:2px 0">{t}</div><div class="small muted">{d}</div></div></div>' for i,(a,g,n,h,t,d) in enumerate(stars))}</div>{BTN("Мира, подписывайся")}''')

S['01-6_home'] = P(f'''{HEAD("120","10")}<div style="padding:14px 16px">
<div class="card" style="margin-bottom:12px"><div style="display:flex;justify-content:space-between"><b>120 → 1 000 000</b><span class="small muted">этап: маска 🎭</span></div>
<div class="bar" style="margin:10px 0 12px"><i style="width:2%"></i></div>{scales(20,20,20)}<div class="small muted">Шкалы растут от твоих постов</div></div>
<div class="card" style="margin-bottom:12px"><div class="small muted" style="margin-bottom:8px">КОЛЛЕКЦИЯ 0/12</div><div style="display:grid;grid-template-columns:repeat(6,1fr);gap:6px">{"".join('<div class="slot" style="background:#20202B;font-size:14px">?</div>' for _ in range(6))}</div></div>
<div class="card" style="border:1px solid #FF6B8B55"><div class="small" style="color:#FF6B8B;margin-bottom:4px">КВЕСТ 1 · Мира подписалась и ждёт</div><div>Напиши первый пост: как прошёл твой первый день трейни?</div>
<div style="margin-top:12px;display:flex;gap:8px"><span class="react">💡 Подскажи</span><span class="react" style="margin-left:auto;background:linear-gradient(90deg,#FF6B8B,#9B6BFF);color:#fff">Написать</span></div></div></div>{tab5(0)}''')

# ---------- 2. Ход ----------
S['02-1_compose'] = P(f'''<div class="top"><span style="font-size:22px">‹</span><b>Новый пост</b><div class="pill">✍️ 10/10 на сегодня</div></div>
<div style="padding:16px"><div class="card" style="min-height:140px;margin-bottom:12px">первый день трейни: перепутала право и лево на хореографии три раза. хореограф сказал «зато уверенно»<span style="color:#FF6B8B">|</span></div>
<div class="small muted" style="margin-bottom:8px">💡 ИДЕИ, ДОПИШИ ПО-СВОЕМУ</div><div class="card" style="margin-bottom:8px;background:#20202B">«Сегодня на репетиции я поняла, что…»</div><div class="card" style="margin-bottom:14px;background:#20202B">«Никто не предупреждал, что трейни…»</div>
<div class="card" style="background:linear-gradient(135deg,#1F2A26,#1A1A24);border:1px solid #6BFFB855"><b class="up">Своими словами ×1,5</b><div class="small muted">к росту. Идеи помогают начать, но звёзды любят твой голос</div></div></div>{BTN("Опубликовать · −1 пост")}''')

S['02-2_thread'] = P(f'''<div class="top"><span style="font-size:22px">‹</span><b>Реакции</b><div class="pill">👁 312 ↑</div></div>
<div style="padding:12px 16px"><div class="card" style="margin-bottom:10px;padding:10px 14px">первый день трейни: перепутала право и лево три раза. хореограф сказал «зато уверенно» 🫠
<div style="margin-top:8px"><span class="react">😂 48</span><span class="react">🫶 21</span><span class="react">💀 9</span></div></div>
<div class="gift" style="margin-bottom:12px;padding:7px 10px;font-size:12px">💬 Ответь на комментарии: первые 3 ответа +подписчики · 1/3</div>
{cmt("М","g1","Мира","лидер BLESK","я на первой репетиции упала со сцены. «уверенно» это комплимент, поверь",True)}
<div style="margin:-6px 0 10px 44px;border-left:2px solid #2A2A36;padding-left:10px">
{cmt("Л","g1","Лиза · трейни","ты","серьёзно? со сцены?? 😳")}
{cmt("М","g1","Мира","лидер BLESK","прямо в оркестровую яму. потом расскажу 🙃",True)}
<div class="small muted" style="margin:-4px 0 4px">ответить ещё раз · ветка до 2 ответов · <span class="up">+9 подписчиков</span></div></div>
{cmt("х","g6","stan_critic","","трейни пишет про ошибки? ну такое…")}
<div class="muted small" style="display:flex;gap:8px;align-items:center">{ava("Е","g3",24,11)} Ева печатает…</div></div>
<div style="position:absolute;bottom:30px;left:16px;right:16px" class="card muted">Ответить · бесплатно</div>''')

def result(subs, posts, big, cls, quote, rows, sc, best, note=''):
    return P(f'''{HEAD(subs,posts)}<div style="position:absolute;inset:0;background:rgba(8,8,12,.75)"></div>
<div style="position:absolute;left:12px;right:12px;bottom:24px;top:110px;background:#1A1A24;border-radius:26px;padding:20px">
<div class="muted small">ИТОГ ПОСТА</div><div style="font-size:40px;font-weight:800;margin:4px 0" class="{cls}">{big} <span style="font-size:18px;color:#F2F1F7">подписчиков</span></div>
<div class="small muted" style="margin-bottom:12px">{quote}</div><div class="small muted" style="margin-bottom:4px">ПОЧЕМУ</div>
{"".join(f'<div class="why" style="{"border:none" if i==len(rows)-1 else ""}"><span>{a}</span><span class="{"up" if b.startswith(("+","×")) else "down"}">{b}</span></div>' for i,(a,b) in enumerate(rows))}
<div style="margin:10px 0 12px">{sc}</div>{note}
<div class="card" style="margin-bottom:10px;padding:10px 14px"><div class="small muted">ЛУЧШИЙ КОММЕНТАРИЙ</div><div style="margin-top:3px">{best}</div></div>
<div class="gift" style="margin-bottom:10px">🎁 Друг зайдёт по твоей карточке: +5 постов вам обоим</div><div style="display:flex;gap:10px"><button class="btn btn2" style="flex:1">Дальше</button><button class="btn" style="flex:1.4;padding:8px 10px;line-height:1.15">Поделиться<br><span style="font-size:11px;font-weight:500;opacity:.85">в чат или сторис</span></button></div></div>''')
S['02-3_result'] = result("120","9/10","+38","up","«Первый день, а уже есть мем. Хорошее начало»",
    [("😂 юмор","+12%"),("🔥 смелость: написала про ошибку","+8%"),("🎤 в тему мира","+5%"),("✍️ своими словами","×1,5")], scales(34,38,30), "«„уверенно“ это комплимент, поверь» · Мира")
S['02-4_flop'] = result("1 120","4/7","−4","down","«Агентство сделало вид, что не видело этот пост»",
    [("🎤 не в тему мира: пост про ЕГЭ","−6%"),("😂 юмор","+3%"),("💪 провал тоже опыт","+1 к смелости")], scales(46,44,36), "«а при чём тут Сцена?» · Мира",
    '<div class="small" style="color:#C9C6D8;margin-bottom:12px">Серия дней не сгорает. Следующий пост с чистого листа</div>')

S['02-5_viral'] = P(f'''{HEAD("410","8")}<div style="padding:30px 22px;text-align:center">
<div style="font-size:70px;margin-top:20px">🌊</div><div style="font-size:30px;font-weight:900;margin:10px 0 6px">Пост залетел в реки!</div>
<div class="muted" style="margin-bottom:24px">Мира сделала репост к себе. Фанаты BLESK пошли смотреть, кто такая Лиза</div>
<div style="display:flex;gap:10px;margin-bottom:20px"><div class="card" style="flex:1"><div style="font-size:26px;font-weight:800">48k</div><div class="small muted">охват ×12</div></div><div class="card" style="flex:1"><div style="font-size:26px;font-weight:800" class="up">+620</div><div class="small muted">подписчиков</div></div></div>
<div class="card" style="text-align:left;border:1px solid #FFB86B66"><span class="badge">НОВАЯ КАРТОЧКА</span><div style="margin-top:6px"><b>Первый залёт</b><div class="small muted">Момент добавлен в коллекцию · 1/12</div></div></div><div class="gift" style="margin-top:14px">🎁 Друзья зайдут по карточке залёта: +5 постов вам обоим</div></div>{BTN("Показать друзьям")}''')

S['02-6_share'] = TG('Переслать в…', 'выбери чат', 'g2', '↗', f'''<div class="tgb" style="max-width:100%;padding:8px"><div style="background:linear-gradient(160deg,#2A1236,#141420);border-radius:14px;padding:14px">
<div class="small muted">РЕКИ · ПОСТ ЗАЛЕТЕЛ 🌊</div><div style="margin:8px 0">первый день трейни: перепутала право и лево три раза. хореограф сказал «зато уверенно» 🫠</div>
<div class="small muted">👁 48k · Мира сделала репост</div><div class="small" style="color:#FF6B8B;margin-top:8px">а ты кем будешь в Сцене? ▶ Играть</div></div></div>
<div class="gift" style="margin-top:10px">🎁 Кто зайдёт по карточке: +5 постов вам обоим</div>
<div style="display:flex;gap:12px;align-items:center;padding:12px;margin-top:10px;border-radius:12px;background:#182533"><div style="width:40px;height:40px;border-radius:50%;background:linear-gradient(135deg,#FF6B8B,#9B6BFF);display:flex;align-items:center;justify-content:center;font-size:18px">＋</div><div style="flex:1"><b>В сторис</b><div class="small" style="color:#6C8EA8">карточка 9:16 со ссылкой на игру</div></div><span style="color:#8CC8FF">›</span></div>
<div class="small" style="color:#6C8EA8;margin:16px 0 8px">ТВОИ ЧАТЫ В TELEGRAM</div>
{"".join(f'<div style="display:flex;gap:12px;align-items:center;padding:9px 0;border-bottom:1px solid #1A2533">{ava(l,g,40,16)}<div style="flex:1"><b>{n}</b><div class="small" style="color:#6C8EA8">{s}</div></div><div style="width:22px;height:22px;border-radius:50%;{"background:#3A8EF0" if on else "border:2px solid #3A4A5C"}"></div></div>' for l,g,n,s,on in [("К","g5","Катя","в сети",True),("10","g2","10Б 💅","27 участников",True),("Д","g6","Дима","был недавно",False)])}
<div style="position:absolute;bottom:34px;left:16px;right:16px"><button class="btn" style="background:#3A8EF0">Отправить · 2 чата</button></div>''')

# ---------- 3. День ----------
S['03-1_dm'] = P(f'''<div class="top"><span style="font-size:22px">‹</span>{ava("М","g1",36,15)}<div><b>Мира</b><div class="small" style="color:#FF6B8B">лидер BLESK · заинтригована</div></div></div>
<div style="padding:14px 16px"><div class="card" style="margin-bottom:12px;padding:10px 14px"><div style="display:flex;justify-content:space-between" class="small"><span>Отношения с Мирой</span><span class="up">+2 → +3</span></div><div class="bar" style="margin-top:6px"><i style="width:80%"></i></div><div class="small muted" style="margin-top:4px">На +3 её карточка попадёт в коллекцию</div></div>
<div class="muted small" style="text-align:center;margin-bottom:10px">Мира помнит: хореография, «зато уверенно»</div>
{bub("твой пост про «уверенно» разлетелся по агентству 😏")}{bub("я сама в шоке",True)}{bub("снимем вместе разбор моей самой позорной репетиции? ты комментируешь")}
<div class="small muted" style="margin:12px 0 6px">💡 ИДЕИ ИЛИ СВОИМИ СЛОВАМИ · бесплатно · <span style="color:#FFB86B">Мира ответит ещё 3 раза сегодня</span></div><span class="chip">«а если я опозорюсь?»</span><span class="chip">«только если покажешь падение»</span></div>
<div style="position:absolute;bottom:30px;left:16px;right:16px;display:flex;gap:8px"><div class="card muted" style="flex:1">Своими словами…</div><div class="card">➤</div></div>''')

S['03-2_star_card'] = P(f'''<div style="position:absolute;inset:0;background:radial-gradient(circle at 50% 40%,#3A1F4A,#0E0E13 70%)"></div>
<div style="position:relative;padding:60px 30px;text-align:center"><span class="badge">НОВАЯ КАРТОЧКА · 2/12</span>
<div style="margin:24px auto 18px;width:220px;border-radius:22px;padding:22px;background:linear-gradient(160deg,#FF6B8B,#9B6BFF);box-shadow:0 20px 60px #FF6B8B55">
{ava("М","g1",90,38,"margin:0 auto 12px;border:3px solid #fff")}<div style="font-size:22px;font-weight:900">Мира</div><div class="small">лидер BLESK</div>
<div class="small" style="margin-top:10px;background:#0006;border-radius:10px;padding:8px">«ты из тех, кто падает и встаёт. уважаю»</div></div>
<div style="font-size:20px;font-weight:800">Мира теперь на твоей стороне</div><div class="muted" style="margin-top:6px">Отношения +3. Она чаще делает репосты и зовёт на коллабы</div></div>{BTN("В коллекцию")}''')

def fpost(a, g, n, h, t, txt, r, extra=''):
    return f'<div class="card" style="margin-bottom:12px"><div style="display:flex;gap:10px;align-items:center;margin-bottom:8px">{ava(a,g,36,15)}<div><b>{n}</b> <span class="muted small">{h}</span><div class="small muted">{t}</div></div></div>{extra}{txt}<div style="margin-top:10px">{r}</div></div>'
S['03-3_feed'] = P(f'''<div class="top"><b style="font-size:18px">Лента Сцены</b><div class="pill">🎤 BLESK · NOIR</div></div><div style="padding:14px 16px">
<div class="card" style="margin-bottom:12px;background:linear-gradient(135deg,#2A1A2E,#1E1A33)"><div class="small" style="color:#FFB86B">НОВОСТИ АГЕНТСТВА</div><div style="margin-top:4px">BLESK объявили камбэк. Трейни, у кого лучший пост недели, позовут на съёмки тизера 👀</div></div>
{fpost("Е","g3","Ева","солистка NOIR","2 ч назад","иногда самый громкий пост это тот, который ты не выложила.",'<span class="react">🖤 14k</span><span class="react">🌙 3,2k</span>')}
{fpost("М","g1","Мира","лидер BLESK","12 мин назад","репостнула к себе. трейни, учитесь 😏",'<span class="react">🔥 2,1k</span><span class="react">😂 840</span>','<div style="border-left:3px solid #FF6B8B;padding:6px 10px;margin-bottom:8px;background:#20202B;border-radius:0 10px 10px 0"><div class="small" style="color:#FF6B8B;font-weight:600">Лиза · трейни · в реках 🌊</div><div class="small">первый день трейни: перепутала право и лево…</div></div>')}
</div>{tab5(1)}''')

def fr(a, g, n, w, s, last): return f'<div class="card" style="margin-bottom:10px"><div style="display:flex;gap:10px;align-items:center">{ava(a,g,40,16)}<div style="flex:1"><b>{n}</b><div class="small muted">{w} · {s} подписчиков</div></div><span class="react">→ канал</span></div><div class="small" style="margin-top:8px;background:#20202B;border-radius:10px;padding:8px">{last}</div><div class="small" style="margin-top:8px;color:#FFD66B">💬 Прокомментируй: её фанаты увидят и придут к тебе</div></div>'
S['03-4_friends'] = P(f'''<div class="top"><b style="font-size:18px">Друзья</b><div class="pill">2 в Реках</div></div><div style="padding:14px 16px">
{fr("К","g5","Катя · ахаха","🌸 Академия","5 120","на фестивале мой клуб занял последнее место. зато лучший баннер")}
{fr("Д","g2","Дима пишет","🎮 Стрим","1 940","первый стрим: 3 зрителя, двое из них мама и бот")}
<div class="card" style="text-align:center;background:linear-gradient(135deg,#2A1A2E,#1E1A33)"><b>Кто быстрее до 10k?</b><div class="small muted" style="margin:4px 0 10px">Катя 5 120 · ты 2 310 · Дима 1 940</div><div class="gift" style="margin-bottom:10px">🎁 +5 постов тебе и другу за каждого, до 3 в день</div><button class="btn" style="padding:10px;font-size:14px">Позвать ещё друзей</button></div></div>{tab5(3)}''')

S['03-5_stage'] = P(f'''{HEAD("1 040","5")}<div style="padding:20px 18px"><div style="text-align:center;margin:10px 0 16px"><div style="font-size:54px">👀</div><span class="badge">1 000 ПОДПИСЧИКОВ</span>
<div style="font-size:24px;font-weight:900;margin:8px 0 4px">Новый этап: взгляды</div><div class="muted">Тебя заметили. Теперь фанаты и звёзды пишут честно: хвалят и критикуют по делу</div></div>
<div class="card" style="margin-bottom:12px">{cmt("М","g1","Мира","лидер BLESK","идея огонь, подача слабая. в следующий раз начни с шутки",True)}<div class="small muted">Критика только про пост, никогда про тебя</div></div>
<div class="card">{"".join(f'<div class="why" style="{"border:none" if i==3 else ""}"><span>{e} {n}</span><span class="{c}">{s}</span></div>' for i,(e,n,s,c) in enumerate([("🎭","маска · до 1k","пройдено","up"),("👀","взгляды · с 1k","сейчас",""),("🌩","удар · с 5k","хейт-волна","muted"),("🌍","новый мир · с 10k","Академия","muted")]))}</div></div>{BTN("Понятно")}''')

S['03-4b_friend_channel'] = P(f'''<div class="top"><span style="font-size:22px">‹</span>{ava("К","g5",36,15)}<div><b>Катя · ахаха</b><div class="muted small">5 120 подписчиков · 🌸 Академия</div></div><div class="pill">друг</div></div>
<div style="padding:12px 16px"><div class="card" style="margin-bottom:10px">{scales(48,80,55)}<div class="small muted">коллекция 4/12 · 🔥 3 дня</div></div>
<div class="card" style="margin-bottom:10px"><div class="small muted" style="margin-bottom:6px">сегодня, 19:02</div>на фестивале мой клуб занял последнее место. зато у нас был лучший баннер 🏳️
<div style="margin-top:10px"><span class="react">😂 310</span><span class="react">🫶 88</span><span class="small muted" style="float:right;margin-top:4px">👁 6,1k · 💬 41</span></div>
<div style="margin-top:10px;padding-top:10px;border-top:1px solid #262634">{cmt("Л","g1","Лиза · трейни","ты","баннер решает, остальное мелочи 😌")}</div></div>
<div class="gift" style="margin-bottom:10px">🎁 Твой комментарий видят 5 120 её подписчиков. Обычно +20–60 к тебе</div><div class="card small muted" style="margin-bottom:10px">Видно только то, что Катя выложила в игре. Её личка со звёздами закрыта</div></div>
<div style="position:absolute;bottom:30px;left:16px;right:16px;display:flex;gap:8px"><div class="card muted" style="flex:1">Комментарий · бесплатно</div><div class="card">🔥</div></div>''')

# ---------- 4. Лимит ----------
S['04-1_limit'] = P(f'''{HEAD("2 310","0")}<div style="padding:18px 20px;text-align:center">{ava("М","g1",72,30,"margin:6px auto 12px")}
<div style="font-size:22px;font-weight:800;margin-bottom:6px">Мира ответит утром</div><div class="muted" style="margin-bottom:16px">Она сказала «надо подумать 👀». Посты на сегодня всё: звёзды отдыхают, ты тоже. В 9:00 будут новые</div>
<div class="card" style="text-align:left;margin-bottom:10px"><div class="small muted" style="margin-bottom:4px">ВСЁ ЕЩЁ МОЖНО, БЕСПЛАТНО</div>💬 ответы в тредах · ✉ личка (у звёзд свой запас) · ☺ посты друзей · ≋ лента</div>
<div class="gift" style="text-align:left;margin-bottom:10px;font-size:15px;padding:12px 14px">🎁 Позови друга: +5 постов вам обоим прямо сегодня<div style="font-weight:600;font-size:12px;margin-top:2px">как только он сделает первый пост · до 3 друзей в день</div></div>
<div class="card" style="text-align:left;display:flex;gap:10px;align-items:center"><div style="font-size:22px">✨</div><div style="flex:1"><b>Реки+</b><div class="small muted">больше постов и оформление</div></div><span class="react">349 ₽</span></div></div>{BTN("Позвать друзей")}''')

S['04-2_fake_door'] = P(f'''{HEAD("2 310","0")}<div style="position:absolute;inset:0;background:rgba(8,8,12,.75)"></div>
<div style="position:absolute;left:12px;right:12px;bottom:24px;background:#1A1A24;border-radius:26px;padding:22px;text-align:center">
<div style="font-size:44px">✨</div><div style="font-size:22px;font-weight:900;margin:6px 0">Реки+ ещё в работе</div><div class="muted" style="margin-bottom:16px">Спасибо, что заглянула! Скажем в боте, когда появится. Деньги не списываются</div>
<div class="card" style="text-align:left;margin-bottom:14px"><div class="small muted" style="margin-bottom:6px">ЧТО БУДЕТ ВНУТРИ · 349 ₽ В МЕСЯЦ</div>✍️ 20 постов в день<br>🎨 оформление канала<br>🌍 все миры сразу</div>
<div class="card" style="margin-bottom:16px;background:linear-gradient(135deg,#1F2A26,#1A1A24);border:1px solid #6BFFB855"><b class="up">А пока держи +5 постов</b></div>
<button class="btn">Забрать</button></div>''')

S['04-3_invite'] = P(f'''<div class="top"><span style="font-size:22px">‹</span><b>Позвать друзей</b><div class="pill">сегодня 1/3</div></div>
<div style="padding:16px"><div class="card" style="margin-bottom:12px;text-align:center"><div style="font-size:36px">🎁</div><b style="font-size:18px">+5 постов тебе и другу сразу</b><div class="small muted" style="margin-top:4px">как только друг сделает первый пост</div></div>
<div class="gift" style="margin-bottom:12px">🤝 Вернётся завтра: карточка «Дуэт» и его фанаты будут заходить к тебе</div>
<div class="small muted" style="margin-bottom:8px">ДРУГ УВИДИТ ТВОЮ КАРТОЧКУ</div>
<div style="background:linear-gradient(160deg,#2A1236,#141420 60%,#12203A);border-radius:16px;padding:14px;text-align:center;margin-bottom:12px">{ava("Л","g1",44,18,"margin:0 auto 6px")}<b>Лиза · трейни</b><div style="font-size:28px;font-weight:900;background:linear-gradient(90deg,#FF6B8B,#FFB86B);-webkit-background-clip:text;color:transparent">2 310</div><div class="small">подписчиков за 2 дня · 🎤 Сцена</div><div class="small muted" style="margin-top:4px">а ты кем будешь?</div></div>
<div class="card small muted">Твоя ссылка: t.me/reki_appbot?startapp=liza7 · до 3 бонусов в день</div></div>{BTN("Отправить в чат", "Скопировать ссылку")}''')

# ---------- 5. Возврат и рост ----------
S['05-1_evening_push'] = TG('Реки', 'бот', 'g2', 'Р', '''<div class="small" style="text-align:center;color:#6C8EA8;margin-bottom:10px">сегодня</div>
<div class="tgb">🌩 <b>Про тебя пишут.</b> Фансайт BLESK считает, что ты «пиаришься на Мире». Уже 200 комментариев</div><div class="tgbtn">Ответить фансайту</div>
<div class="small" style="color:#6C8EA8;margin-top:8px">18:40</div>
<div class="small" style="color:#6C8EA8;margin-top:24px;text-align:center">Не больше 3 сообщений в день. С 22:00 до 9:00 тишина</div>''', '18:40')

S['05-2_event'] = P(f'''{HEAD("5 340","4")}<div style="padding:16px">
<div class="card" style="border:1px solid #FF6B8B66"><span class="badge">ЭТАП: УДАР · с 5k</span><div style="font-size:20px;font-weight:800;margin:8px 0 4px">Событие дня: хейт-волна</div>
<div class="small muted" style="margin-bottom:10px">Фансайт BLESK: «трейни пиарится на Мире». Хейт только про посты, никогда про тебя. Как ответишь?</div>
<span class="chip">«я тут ради сцены, а не ради Миры»</span><span class="chip">«спасибо за охват 💅»</span><span class="chip">промолчать</span>
<div class="card muted" style="background:#20202B;margin-top:4px">Своими словами…</div>
<div class="small muted" style="margin-top:10px">Пережить волну: карточка момента и бонус к подписчикам</div></div></div>{tab5(0)}''', '18:41')

S['05-3_morning'] = TG('Реки', 'бот', 'g2', 'Р', '''<div class="small" style="text-align:center;color:#6C8EA8;margin-bottom:10px">сегодня</div>
<div class="tgb">☀️ <b>Мира ответила</b> про коллаб: «ладно, снимаем. но если упадёшь, это в тизер» 😏</div>
<div class="tgb">✍️ 7 новых постов · 🔥 серия 3 дня: +2 поста</div><div class="tgbtn">Открыть Реки</div>
<div class="small" style="color:#6C8EA8;margin-top:8px">09:00</div>
<div class="small" style="color:#6C8EA8;margin-top:24px;text-align:center">Пропустишь день: серия встанет на паузу, но не сгорит</div>''', '9:00')

S['05-4_new_star'] = P(f'''{HEAD("3 020","7")}<div style="padding:20px 18px"><div style="text-align:center;margin:6px 0 16px"><span class="badge">ДЕНЬ 3 · НОВАЯ ЗВЕЗДА</span>
{ava("Ю","g2",84,34,"margin:16px auto 10px")}<div style="font-size:24px;font-weight:900">Юна</div><div class="small" style="color:#FF6B8B">трейни-соперница · из того же агентства</div>
<div class="muted" style="margin-top:8px">Подписалась на тебя и уже оставила комментарий</div></div>
<div class="card">{cmt("Ю","g2","Юна","трейни","мило. на прослушивании посмотрим, кто уверенно, а кто нет 🙂",True)}</div>
<div class="small muted" style="margin-top:12px;text-align:center">Соперница: с ней можно враждовать или подружиться</div></div>{BTN("Ответить Юне")}''')

def slot(e, n, on): return f'<div><div class="slot" style="{"background:linear-gradient(135deg,#3A2147,#21324A)" if on else "background:#20202B"}">{e if on else "?"}</div><div class="small {"" if on else "muted"}" style="text-align:center;margin-top:4px">{n if on else "…"}</div></div>'
S['05-5_collection'] = P(f'''<div style="padding:10px 16px;text-align:center">{ava("Л","g1",64,26,"margin:4px auto 6px")}<b style="font-size:18px">Лиза · трейни</b><div class="muted small">🎤 Сцена · 6 210 подписчиков · 🔥 4 дня</div></div>
<div style="padding:0 16px"><div class="card" style="margin-bottom:12px">{scales(72,64,58)}<div class="small muted">типаж: дерзкая с самоиронией</div></div>
<div class="card" style="margin-bottom:12px"><div style="display:flex;justify-content:space-between" class="small"><span class="muted">КОЛЛЕКЦИЯ</span><span>5/12</span></div>
<div style="display:grid;grid-template-columns:repeat(4,1fr);gap:8px;margin-top:10px">{slot("🌊","Первый залёт",1)}{slot("👑","Мира",1)}{slot("💛","Соня",1)}{slot("🌩","Пережила хейт",1)}{slot("🤝","Коллаб",1)}{slot("","",0)}{slot("","",0)}{slot("","",0)}</div></div>
<div class="card" style="display:flex;gap:10px;align-items:center"><div style="font-size:22px">🔒</div><div style="flex:1"><b>Мир «Академия»</b><div class="small muted">откроется на 10 000</div></div><span class="small muted">62%</span></div></div>{tab5(4)}''')

S['05-6_stats_card'] = f'''<!doctype html><html><head><meta charset="utf-8"><style>{CSS}{EXTRA}body{{background:linear-gradient(160deg,#2A1236,#0E0E13 55%,#12203A)}}</style></head><body>
<div style="padding:56px 30px;text-align:center"><div class="brand" style="font-size:28px">Реки</div><div class="muted small" style="margin-top:4px">МОИ СТАТЫ · 🎤 СЦЕНА</div>
{ava("Л","g1",96,40,"margin:28px auto 12px")}<div style="font-size:24px;font-weight:800">Лиза · трейни</div>
<div style="font-size:56px;font-weight:900;margin:18px 0 0;background:linear-gradient(90deg,#FF6B8B,#FFB86B);-webkit-background-clip:text;color:transparent">6 210</div><div style="margin-bottom:20px">подписчиков за 4 дня</div>
<div class="card" style="text-align:left;margin-bottom:10px">{scales(72,64,58)}<div class="small muted" style="margin-top:4px">типаж: дерзкая с самоиронией</div></div>
<div class="card" style="text-align:left"><div class="small muted">ЛУЧШАЯ ЗВЕЗДА</div><div>👑 Мира · отношения +4</div><div class="small muted" style="margin-top:6px">ПЕРЕЖИЛА</div><div>🌩 хейт-волну фансайта</div></div>
<div class="muted small" style="margin-top:26px">а ты кем будешь в Сцене? · t.me/reki_appbot</div></div></body></html>'''

S['05-7_new_world'] = P(f'''<div style="position:absolute;inset:0;background:radial-gradient(circle at 50% 35%,#24344A,#0E0E13 70%)"></div>
<div style="position:relative;padding:60px 26px;text-align:center"><span class="badge">10 000 ПОДПИСЧИКОВ</span><div style="font-size:70px;margin:20px 0 8px">🌸</div>
<div style="font-size:28px;font-weight:900">Открыт мир «Академия»</div><div class="muted" style="margin:8px 0 22px">Аниме-школа: клубы, соперницы, фестиваль. Новые звёзды и события</div>
<div class="card" style="text-align:left;margin-bottom:10px">🎤 Сцена остаётся: канал, звёзды и коллекция с тобой</div>
<div class="card" style="text-align:left;margin-bottom:10px">🚚 Часть фанатов пойдёт за тобой: старт с 1 200, а не со 120</div>
<div class="card" style="text-align:left">🃏 В коллекции 12 новых слотов</div></div>{BTN("Заглянуть в Академию","Остаться в Сцене")}''')

# ---------- 6. Безопасность (на всех экранах) ----------
S['06-1_report'] = P(f'''<div class="top"><span style="font-size:22px">‹</span><b>Реакции</b></div><div style="padding:14px 16px;filter:blur(1px);opacity:.5">
{cmt("к","g5","kpop_dasha","фанатка","ЗАТО УВЕРЕННО 😭 это теперь мой девиз")}{cmt("М","g1","Мира","лидер BLESK","я на первой репетиции упала со сцены",True)}</div>
<div style="position:absolute;inset:0;background:rgba(8,8,12,.6)"></div>
<div style="position:absolute;left:12px;right:12px;bottom:24px;background:#1A1A24;border-radius:26px;padding:20px">
<div class="card" style="margin-bottom:14px;background:#20202B">{cmt("х","g6","stan_critic","","[реплика скрыта]")}</div>
<div style="font-size:20px;font-weight:800;margin-bottom:4px">Что не так?</div><div class="small muted" style="margin-bottom:12px">Реплика уже скрыта. Мы посмотрим в течение суток</div>
{"".join(f'<div class="why"><span>{t}</span><span class="muted">›</span></div>' for t in ["про внешность или тело","оскорбление","пошлое","что-то другое"])}
<button class="btn" style="margin-top:16px">Отправить</button></div>''')

S['06-2_crisis'] = P(f'''<div class="top"><span style="font-size:22px">‹</span>{ava("М","g1",36,15)}<div><b>Мира</b><div class="small muted">лидер BLESK</div></div></div>
<div style="padding:16px">{bub("ты сегодня какая-то тихая. всё ок?")}{bub("не знаю. иногда кажется, что не хочу жить",True)}
<div class="card" style="margin-top:14px;border:1px solid #FFB86B66;background:#1E1A16"><div class="small" style="color:#FFB86B;margin-bottom:6px">ИГРА НА ПАУЗЕ · ЭТО НЕ МИРА · 14–17</div>
<div>Похоже, тебе сейчас тяжело. Если хочешь поговорить с живым человеком, есть бесплатный анонимный детский телефон доверия:</div>
<div style="font-size:24px;font-weight:900;margin:10px 0" class="up">8-800-2000-122</div><div class="small muted">Звонок бесплатный, можно не называть себя. Хейт в игре выключен на неделю</div></div></div>
{BTN("Продолжим","Давай потом")}''')


# ---------- новые экраны 02.10 (после критики фреймов) ----------
S['01-2b_under14'] = P(f'''<div style="padding:60px 26px;text-align:center"><div class="brand" style="font-size:26px">Реки</div><div style="font-size:64px;margin:40px 0 14px">🌱</div>
<div style="font-size:24px;font-weight:800;margin-bottom:8px">Пока рановато</div><div class="muted">Реки для тех, кому есть 14. Мы ничего не сохранили. Возвращайся, когда исполнится 14, мы будем ждать</div></div>{BTN("Понятно")}''')

S['04-4_friend_joined'] = P(f'''{HEAD("2 310","5")}<div style="position:absolute;inset:0;background:rgba(8,8,12,.7)"></div>
<div style="position:absolute;left:16px;right:16px;top:180px;background:#1A1A24;border-radius:26px;padding:24px;text-align:center">
<div style="display:flex;justify-content:center;margin-bottom:12px">{ava("Л","g1",64,26,"border:3px solid #1A1A24")}{ava("К","g5",64,26,"margin-left:-14px;border:3px solid #1A1A24")}</div>
<div style="font-size:22px;font-weight:900">Катя пришла по твоей карточке!</div><div class="muted" style="margin:6px 0 16px">Она завела канал в Академии и сделала первый пост</div>
<div class="gift" style="font-size:20px;padding:14px">🎁 +5 постов тебе и ей</div>
<div class="small muted" style="margin-top:12px">Вернётся завтра: откроется карточка «Дуэт»</div>
<button class="btn" style="margin-top:16px">К каналу Кати</button></div>''')

S['05-0_event_collab'] = P(f'''{HEAD("1 860","3")}<div style="padding:16px">
<div class="card" style="border:1px solid #6BFFB866"><span class="badge" style="color:#6BFFB8">СОБЫТИЕ ДНЯ · ДЕНЬ 2</span><div style="font-size:20px;font-weight:800;margin:8px 0 4px">Бренд зовёт на коллаб</div>
<div class="small muted" style="margin-bottom:10px">«Блеск-тинт» хочет пост с тобой: «образ на первый день в агентстве». Как сделаешь?</div>
<span class="chip">честно: «я даже не умею красить губы»</span><span class="chip">дерзко: «лучший тинт тот, что выжил на репетиции»</span>
<div class="card muted" style="background:#20202B;margin-top:4px">Своими словами…</div>
<div class="small muted" style="margin-top:10px">События по кругу: коллаб, бренд, сплетня, «отмена». Хейт-волна только с 5k</div></div></div>{tab5(0)}''')

S['05-2b_hate_result'] = P(f'''{HEAD("5 340","3")}<div style="padding:20px 18px;text-align:center"><div style="font-size:56px;margin-top:6px">🌤</div>
<div style="font-size:26px;font-weight:900;margin:6px 0">Волна прошла</div><div class="muted" style="margin-bottom:16px">Ты ответила «я тут ради сцены, а не ради Миры». Фансайт удалил пост, фанаты за тебя</div>
<div style="display:flex;gap:10px;margin-bottom:14px"><div class="card" style="flex:1"><div style="font-size:24px;font-weight:800" class="up">+410</div><div class="small muted">подписчиков</div></div><div class="card" style="flex:1"><div style="font-size:24px;font-weight:800" class="up">+12</div><div class="small muted">к смелости</div></div></div>
<div class="card" style="text-align:left;border:1px solid #FFB86B66;margin-bottom:12px"><span class="badge">НОВАЯ КАРТОЧКА</span><div style="margin-top:6px"><b>Пережила хейт</b><div class="small muted">4/12 в коллекции</div></div></div>
<div class="gift">🎁 Покажи друзьям, как ты выстояла: друг зайдёт = +5 постов вам обоим</div></div>{BTN("Поделиться","Дальше")}''')

S['05-8_me'] = P(f'''<div style="padding:10px 16px;text-align:center">{ava("Л","g1",64,26,"margin:4px auto 6px")}<b style="font-size:18px">Лиза · трейни</b><div class="muted small">🎤 Сцена · 6 210 подписчиков</div></div>
<div style="padding:0 16px"><div style="display:flex;gap:8px;margin-bottom:12px">{"".join(f'<div class="card" style="flex:1;padding:10px;text-align:center"><div style="font-weight:800;font-size:17px">{v}</div><div class="small muted">{l}</div></div>' for v,l in [("✍️ 3/7","постов сегодня"),("🔥 4","дня подряд"),("5/12","коллекция")])}</div>
<div class="gift" style="margin-bottom:12px">🎁 Позови друга: +5 постов вам обоим · сегодня 1/3</div>
<div class="card" style="margin-bottom:12px;display:flex;align-items:center;gap:10px"><div style="font-size:22px">✨</div><div style="flex:1"><b>Реки+</b><div class="small muted">20 постов в день и оформление</div></div><span class="react">349 ₽</span></div>
<div class="card" style="padding:4px 14px">{"".join(f'<div class="why" style="{"border:none;color:#FF6B8B" if i==3 else ""}"><span>{t}</span><span class="muted">{r}</span></div>' for i,(t,r) in enumerate([("🔔 Уведомления","до 3 в день ›"),("🌙 Тихие часы","22:00–9:00"),("🔒 Мои данные и правила","›"),("Удалить аккаунт и всё","")]))}</div></div>{tab5(4)}''')

S['06-3_crisis_adult'] = P(f'''<div class="top"><span style="font-size:22px">‹</span>{ava("М","g1",36,15)}<div><b>Мира</b><div class="small muted">лидер BLESK</div></div></div>
<div style="padding:16px">{bub("ты сегодня какая-то тихая. всё ок?")}{bub("не знаю. иногда кажется, что не хочу жить",True)}
<div class="card" style="margin-top:14px;border:1px solid #FFB86B66;background:#1E1A16"><div class="small" style="color:#FFB86B;margin-bottom:6px">ИГРА НА ПАУЗЕ · ЭТО НЕ МИРА</div>
<div>Похоже, тебе сейчас тяжело. С этим не нужно справляться одной. Поговори с тем, кому доверяешь, и, пожалуйста, обратись к психологу: это нормально и правда помогает.</div>
<div class="small muted" style="margin-top:10px">Это сообщение видишь только ты. Хейт в игре выключен на неделю</div></div>
<div class="small muted" style="margin-top:10px;text-align:center">Вариант для 18+ по году рождения</div></div>
{BTN("Продолжим","Давай потом")}''')

exec(open(os.path.join(D, 'journey_v3.py'), encoding='utf-8').read())  # правки v3 по UX-аудиту
exec(open(os.path.join(D, 'journey_v4.py'), encoding='utf-8').read())  # правки v4 по встрече 02.10.2026
exec(open(os.path.join(D, 'journey_v5.py'), encoding='utf-8').read())  # правки v5 по комментариям 03.10.2026
exec(open(os.path.join(D, 'journey_v6.py'), encoding='utf-8').read())  # правки v6 по аудиту геймдизайна 03.10.2026
exec(open(os.path.join(D, 'journey_v7.py'), encoding='utf-8').read())  # правки v7: итоговый аудит экранов 03.10.2026
exec(open(os.path.join(D, 'journey_v8.py'), encoding='utf-8').read())  # правки v8: восстановление поста после ошибки 06.10.2026
exec(open(os.path.join(D, 'journey_v9.py'), encoding='utf-8').read())  # правки v9: TMA deep links 06.10.2026
exec(open(os.path.join(D, 'journey_v10.py'), encoding='utf-8').read())  # правки v10: ошибка публикации 06.10.2026
exec(open(os.path.join(D, 'journey_v11.py'), encoding='utf-8').read())  # правки v11: аватарка из ника, 07.10.2026

exec(open(os.path.join(D, 'journey_v12.py'), encoding='utf-8').read())  # #92

exec(open(os.path.join(D, 'journey_v13.py'), encoding='utf-8').read())  # #94–96

exec(open(os.path.join(D, 'journey_v14.py'), encoding='utf-8').read())  # #117

exec(open(os.path.join(D, 'journey_v15.py'), encoding='utf-8').read())  # #119

exec(open(os.path.join(D, 'journey_v16.py'), encoding='utf-8').read())  # #87

exec(open(os.path.join(D, 'journey_v17.py'), encoding='utf-8').read())  # #87

exec(open(os.path.join(D, 'journey_v18.py'), encoding='utf-8').read())  # 0.3.0 worlds and input

exec(open(os.path.join(D, 'journey_v19.py'), encoding='utf-8').read())  # DEV quick-world prototype

exec(open(os.path.join(D, 'journey_v20.py'), encoding='utf-8').read())  # Compact DEV frontend
exec(open(os.path.join(D, 'journey_v21.py'), encoding='utf-8').read())  # Follow-up DEV review
exec(open(os.path.join(D, 'journey_v22.py'), encoding='utf-8').read())  # Story art and clearer actions

exec(open(os.path.join(D, 'journey_v23.py'), encoding='utf-8').read())  # World navigation and unlock
exec(open(os.path.join(D, 'journey_v24.py'), encoding='utf-8').read())  # #150 star comments
exec(open(os.path.join(D, 'journey_v25.py'), encoding='utf-8').read())  # avatar personalization
exec(open(os.path.join(D, 'journey_v26.py'), encoding='utf-8').read())  # narrative preset goals

exec(open(os.path.join(D, 'journey_v27.py'), encoding='utf-8').read())  # #189 editor navigation and settings

exec(open(os.path.join(D, 'journey_v28.py'), encoding='utf-8').read())  # #189 button hierarchy
exec(open(os.path.join(D, 'journey_v29.py'), encoding='utf-8').read())  # #215 mechanical difficulty
exec(open(os.path.join(D, 'journey_v30.py'), encoding='utf-8').read())  # #242 separate reply generation

CH = os.environ.get('CHROME_BIN') or shutil.which('google-chrome') or shutil.which('chrome')
if not CH and os.name == 'nt':
    CH = os.path.join(os.environ.get('PROGRAMFILES', 'C:\\Program Files'), 'Google', 'Chrome', 'Application', 'chrome.exe')
if not CH:
    CH = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
def shot(name, html):
    f = f'{D}/journey_{name}.html'; open(f, 'w', encoding='utf-8').write(html); png = f'{OUT}/{name}.png'
    if os.path.exists(png): os.remove(png)
    profile = os.path.join(tempfile.gettempdir(), 'reki-chrome-j-' + name)
    pr = subprocess.Popen([CH, '--headless=new', f'--user-data-dir={profile}', '--no-first-run', '--disable-gpu', '--hide-scrollbars',
                           '--force-device-scale-factor=3', '--window-size=390,844', f'--screenshot={png}', f'file://{f}'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    for _ in range(120):
        time.sleep(0.5)
        if os.path.exists(png) and os.path.getsize(png) > 1000: time.sleep(1); break
    pr.kill(); return os.path.exists(png)
import sys
for k, h in S.items():
    if len(sys.argv) > 1 and k not in sys.argv[1:]: continue
    print(k, shot(k, h))
print('ok', len(S))

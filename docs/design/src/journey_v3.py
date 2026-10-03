# Правки фреймов v3 (02.10.2026) по UX-аудиту: product/ux-audit.md.
# Подключается из build_journey.py после всех экранов и переопределяет их.
# Главное: «момент после поста» одной карточкой, без цепочки окон; онбординг с шагами и «назад»;
# согласие не отмечено заранее; один главный CTA; одна жёлтая плашка на экран; кризис без служебных пометок.

def ONB(n, back=True):
    dots = ''.join('<i style="display:inline-block;width:%dpx;height:6px;border-radius:3px;margin-left:5px;background:%s"></i>'
                   % (18 if i == n else 6, '#FF6B8B' if i <= n else '#33323F') for i in range(1, 5))
    arrow = '<span style="font-size:24px;color:#C9C6D8">‹</span>' if back else ''
    return f'<div style="display:flex;align-items:center;padding:2px 16px 0;height:30px">{arrow}<div style="margin-left:auto">{dots}</div></div>'

def whisper(text, who='М', name='Мира'):
    return ('<div style="display:flex;gap:8px;align-items:flex-end;margin:8px 0 10px">' + ava(who, 'g1', 30, 12) +
            '<div style="background:#251E33;border:1px solid #9B6BFF77;border-radius:14px 14px 14px 4px;padding:8px 11px;font-size:13px;line-height:1.35">'
            '<div style="color:#C9A8FF;font-size:11px;margin-bottom:2px">' + name + ' подсказывает</div>' + text + '</div></div>')

def why(ic, t, n):
    cls = 'up' if n[0] in '+×' else 'down'
    return f'<span class="chip" style="background:#22222D;margin:0 6px 6px 0;font-size:14px">{ic} {t} <b class="{cls}">{n}</b></span>'

def ribbon(ic, t, sub=''):
    sub_html = f'<div class="small muted">{sub}</div>' if sub else ''
    return (f'<div style="display:flex;gap:10px;align-items:center;background:#22222D;border-radius:14px;padding:9px 12px;margin-bottom:8px">'
            f'<div style="font-size:20px">{ic}</div><div style="flex:1;font-size:14px"><b>{t}</b>{sub_html}</div><span class="muted">›</span></div>')

SPARK = ''.join(f'<span style="position:absolute;left:{x}%;top:{y}px;font-size:{s}px;color:{c};opacity:{o}">✦</span>'
                for x, y, s, c, o in [(8, 22, 14, '#FFD66B', .9), (24, 70, 9, '#FF6B8B', .7), (80, 18, 13, '#9B6BFF', .9),
                                      (90, 70, 9, '#FFD66B', .7), (66, 44, 7, '#fff', .6), (40, 12, 8, '#fff', .5), (56, 84, 10, '#FF6B8B', .6)])

def CH_BG(subs, posts, text='первый день трейни: перепутала право и лево три раза. хореограф сказал «зато уверенно» 🫠', views='312'):
    return (HEAD(subs, posts) + '<div style="padding:14px 16px"><div class="card"><div class="small muted" style="margin-bottom:6px">только что</div>'
            + text + f'<div style="margin-top:10px"><span class="react">😂 48</span><span class="react">🫶 21</span><span class="small muted" style="float:right;margin-top:4px">👁 {views}</span></div></div></div>')

CTA_NEXT = ('<button class="btn">Дальше</button>'
            '<div style="text-align:center;margin-top:10px;font-size:14px;color:#C9A8FF">Поделиться в чат или сторис ›</div>'
            '<div style="text-align:center;font-size:12px;color:#FFD66B;margin-top:3px">🎁 друг зайдёт по карточке: +5 постов вам обоим</div>')
CTA_SHARE = ('<button class="btn" style="padding:10px;line-height:1.25">Поделиться залётом'
             '<div style="font-size:12px;font-weight:600;color:#FFE9A8">🎁 друг зайдёт: +5 постов вам обоим</div></button>'
             '<div style="text-align:center;margin-top:10px;font-size:14px;color:#A9A6BD">Дальше</div>')

def result_v3(bg, big, cls, headline, whys, ribbons, best, cta, hint='', theme='#1A1A24', label='ИТОГ ПОСТА',
              shift=0, show_rest=True, show_chips=True, best_who='М'):
    spark = SPARK if cls == 'up' else ''
    rest = (ribbons + f'<div class="small" style="display:flex;gap:8px;align-items:center;margin:4px 0 14px;color:#C9C6D8">{ava(best_who,"g1",24,11)}<span>«{best}»</span></div>'
            + hint + cta) if show_rest else ''
    chips = f'<div style="margin-bottom:6px">{whys}</div>' if show_chips else ''
    return P(bg + f'''<div style="position:absolute;inset:0;background:rgba(8,8,12,.72)"></div>
<div style="position:absolute;left:10px;right:10px;bottom:20px;background:{theme};border-radius:28px;padding:20px 18px;overflow:hidden;transform:translateY({shift}px)">{spark}
<div style="position:relative"><div class="small" style="color:#C9C6D8;letter-spacing:.06em">{label}</div>
<div style="font-size:58px;font-weight:900;line-height:1.05;margin:4px 0 0" class="{cls}">{big}</div>
<div class="small muted" style="margin-bottom:10px">подписчиков</div>
<div style="font-size:17px;font-weight:700;margin-bottom:12px">{headline}</div>{chips}{rest}</div></div>''')

# ---------- 1. Вход ----------
s = S['01-2_age']
for a, b in [('<div style="padding:18px 20px"><div class="brand" style="font-size:26px">Реки</div>',
              ONB(1, False) + '<div style="padding:4px 20px"><div class="brand" style="font-size:26px">Реки</div>'),
             ('<div style="width:22px;height:22px;border-radius:6px;background:linear-gradient(135deg,#FF6B8B,#9B6BFF);text-align:center;font-size:14px">✓</div>',
              '<div style="width:24px;height:24px;border-radius:6px;border:2px solid #8E8BA3;flex:none"></div>'),
             ('<button class="btn">Дальше</button></div>',
              '<div class="small muted" style="text-align:center;margin-bottom:8px">Отметь согласие, чтобы продолжить</div><button class="btn" style="opacity:.35">Дальше</button></div>')]:
    assert a in s, a[:60]; s = s.replace(a, b, 1)
S['01-2_age'] = s
for k, n in [('01-3_world', 2), ('01-4_channel', 3), ('01-5_first_star', 4)]:
    a = '<div style="padding:18px 20px">'
    assert a in S[k]; S[k] = S[k].replace(a, ONB(n) + '<div style="padding:4px 20px">', 1)

S['01-6_home'] = P(f'''{HEAD("120","10")}<div style="padding:16px">
<div class="small muted" style="margin-bottom:10px">цель игры: 120 → 1 000 000 подписчиков</div>
<div class="card" style="padding:18px;border:1px solid #FF6B8B66;background:linear-gradient(160deg,#2A1A2E,#181821)">
<div style="display:flex;gap:10px;align-items:center;margin-bottom:12px">{ava("М","g1",44,18)}<div><b>Мира подписалась на тебя</b><div class="small muted">лидер BLESK · ждёт твой первый пост</div></div></div>
<div style="font-size:20px;font-weight:800;margin-bottom:16px">Как прошёл твой первый день трейни?</div>
<button class="btn">Написать первый пост</button>
<div style="text-align:center;margin-top:10px;font-size:14px;color:#C9A8FF">Нет идей? Внутри есть «Подскажи»</div></div>
<div class="card" style="margin-top:12px;display:flex;gap:12px;align-items:center"><div style="font-size:26px">🌊</div><div class="small" style="color:#C9C6D8"><b style="color:#fff">Реки</b> — общая лента, где тебя видят все. Хороший пост может туда залететь, и придут тысячи фанатов</div></div>
</div>{tab5(0)}''')

# ---------- 2. Ход: пост → живая реакция → одна карточка итога ----------
S['02-1_compose'] = P(f'''<div class="top"><span style="font-size:22px">‹</span><b>Новый пост</b><div class="pill">✍️ осталось 10 на сегодня</div></div>
<div style="padding:14px 16px"><div class="card" style="min-height:104px;margin-bottom:6px">первый день трейни: перепутала право и лево на хореографии три раза. хореограф сказал «зато уверенно»<span style="color:#FF6B8B">|</span></div>
{whisper("Не знаешь, что писать? Возьми идею и допиши по-своему. Своими словами подписчиков приходит в 1,5 раза больше")}
<div class="small muted" style="margin:4px 0 8px">💡 ПОДСКАЖИ</div>
<div class="opt">«Сегодня на репетиции я поняла, что…»</div><div class="opt">«Никто не предупреждал, что трейни…»</div></div>
{BTN("Опубликовать")}''')

def live(views, reacts=(), toast=False, comments=0, glow=1.0):
    rs = ''.join(f'<span style="position:absolute;left:{x}%;top:{y}px;font-size:{s}px;opacity:{o}">{e}</span>' for e, x, y, s, o in reacts)
    toast_html = (f'<div style="position:absolute;top:118px;left:16px;right:16px;display:flex;gap:10px;align-items:center;background:#262636;border:1px solid #FF6B8B66;border-radius:16px;padding:10px 12px;box-shadow:0 10px 30px #0008;z-index:3">{ava("М","g1",32,13)}'
                  '<div class="small"><b>Мира прокомментировала</b><div style="color:#C9C6D8">«„уверенно“ это комплимент, поверь»</div></div></div>') if toast else ''
    cm = ''
    if comments >= 1: cm += cmt("к", "g5", "kpop_dasha", "фанатка", "ЗАТО УВЕРЕННО 😭 это теперь мой девиз")
    if comments >= 2: cm += cmt("М", "g1", "Мира", "лидер BLESK", "я на первой репетиции упала со сцены. «уверенно» это комплимент", True)
    typing = f'<div class="muted small" style="display:flex;gap:8px;align-items:center">{ava("С","g4",22,10)} Соня печатает…</div>'
    return P(f'''{HEAD("120","9")}{toast_html}<div style="padding:{86 if toast else 14}px 16px 14px;position:relative">
<div class="card" style="box-shadow:0 0 0 2px rgba(255,107,139,{.6*glow}),0 0 44px rgba(255,107,139,{.35*glow});margin-bottom:12px"><div class="small" style="color:#FF6B8B;margin-bottom:6px">только что опубликовано</div>
первый день трейни: перепутала право и лево три раза. хореограф сказал «зато уверенно» 🫠
<div style="display:flex;align-items:center;margin-top:12px"><span style="font-size:26px;font-weight:900">👁 {views}</span><span class="up small" style="margin-left:8px">растёт</span></div></div>
<div style="position:absolute;right:14px;top:30px;width:120px;height:200px;pointer-events:none">{rs}</div>
{cm}{typing if comments < 2 else ""}</div>
<div style="position:absolute;bottom:96px;left:0;right:0;text-align:center" class="small muted">реакции ещё идут · итог через пару секунд</div>{tab5(0)}''')

REACTS = [('😂', 10, 150, 26, .9), ('🫶', 55, 110, 22, .8), ('🔥', 20, 60, 20, .6), ('😂', 70, 20, 16, .4), ('💀', 40, 0, 14, .3)]
S['02-2_live'] = live(312, REACTS, toast=True, comments=1)

R_HINT = whisper("Подписчики приходят за смелость, юмор и попадание в тему мира. Смотри, за что дали в этот раз")
S['02-3_result'] = result_v3(CH_BG("120", "9"), "+38", "up", "Тебя заметили: первый мем уже есть",
                             why("😂", "смешно", "+12") + why("🔥", "смело", "+8") + why("✍️", "своими словами", "×1,5"),
                             ribbon("👑", "Мира: отношения +1"), "„уверенно“ это комплимент, поверь", CTA_NEXT, hint=R_HINT)
S['02-4_flop'] = result_v3(CH_BG("1 120", "4", "сегодня сдавала пробник по ЕГЭ, всё сложно", "640"), "−4", "down",
                           "Агентство сделало вид, что не видело этот пост",
                           why("🎤", "не про Сцену", "−6") + why("😂", "смешно", "+3"),
                           ribbon("💪", "Провал тоже опыт: смелость +1", "серия дней не сгорает"), "а при чём тут Сцена?",
                           '<button class="btn">Дальше</button>')
S['02-5_viral'] = result_v3(CH_BG("410", "8", views="48k"), "+620", "up", "Мира сделала репост. Фанаты BLESK пошли смотреть, кто такая Лиза",
                            why("👁", "охват 48k", "×12"),
                            ribbon("🃏", "Новая карточка: Первый залёт", "нажми, чтобы открыть") + ribbon("👑", "Мира: отношения +1"),
                            "трейни, учитесь", CTA_SHARE, theme='linear-gradient(170deg,#1B2A4A,#2A1236 60%,#1A1A24)', label='🌊 ПОСТ ЗАЛЕТЕЛ В РЕКИ')

th = S.pop('02-2_thread')
a = '<div style="position:absolute;bottom:30px;left:16px;right:16px" class="card muted">Ответить · бесплатно</div>'
assert a in th
S['02-6_thread'] = th.replace(a, '<div style="position:absolute;bottom:30px;left:16px;right:16px;display:flex;gap:8px"><div class="card muted" style="flex:1">Ответить · бесплатно</div><div class="card" style="padding:14px 18px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF)">➤</div></div>')
S['02-7_share'] = S.pop('02-6_share')

# ---------- 3. День ----------
for k in ['03-1_dm', '05-0_event_collab', '05-2_event']:
    S[k] = S[k].replace('<span class="chip">', '<span class="chip opt2">')

S['03-5_stage'] = P(f'''{HEAD("1 040","5")}<div style="padding:12px 16px">
<div class="card" style="margin-bottom:10px;background:linear-gradient(135deg,#2A1A2E,#1E1A33);border:1px solid #FF6B8B55;position:relative"><span class="muted" style="position:absolute;right:14px;top:10px">✕</span><span class="badge">НОВЫЙ ЭТАП · 1 000 ПОДПИСЧИКОВ</span><div style="font-size:18px;font-weight:800;margin:6px 0 2px">👀 Взгляды</div><div class="small" style="color:#C9C6D8">Тебя заметили. Теперь фанаты и звёзды пишут честно: хвалят и критикуют по делу. Только про посты</div></div>
<div class="card" style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;margin-bottom:8px"><b>Твои шкалы</b><span class="small" style="color:#C9A8FF">как поднять ›</span></div>{scales(46,52,38)}</div>
<div class="card" style="margin-bottom:10px;display:flex;align-items:center;gap:10px"><div style="font-size:22px">🃏</div><div style="flex:1"><b>Коллекция 1/12</b><div class="small muted">Первый залёт</div></div><span class="muted">›</span></div>
<div class="card"><div class="small muted" style="margin-bottom:6px">вчера · в реках 🌊</div>первый день трейни: перепутала право и лево три раза…<div style="margin-top:8px"><span class="react">😂 412</span><span class="react">🫶 96</span><span class="small muted" style="float:right;margin-top:4px">👁 48k</span></div></div></div>
<div style="position:absolute;bottom:96px;left:16px;right:16px"><button class="btn">Новый пост</button></div>{tab5(0)}''')

# ---------- 4. Лимит ----------
S['04-1_limit'] = P(f'''{HEAD("2 310","0")}<div style="padding:14px 20px;text-align:center">{ava("М","g1",72,30,"margin:2px auto 10px")}
<div style="font-size:22px;font-weight:800;margin-bottom:6px">Мира ответит утром</div><div class="muted" style="margin-bottom:14px">Она сказала «надо подумать 👀». Посты на сегодня всё, звёзды отдыхают. В 9:00 будут новые</div>
<div class="card" style="text-align:left;margin-bottom:12px"><div class="small" style="color:#C9C6D8;margin-bottom:4px">А пока бесплатно</div>отвечать в тредах · писать звёздам · заглянуть к друзьям · листать ленту</div>
<div class="gift" style="text-align:left;font-size:15px;padding:12px 14px">🎁 Позови друга: +5 постов вам обоим прямо сегодня<div style="font-weight:600;font-size:12px;margin-top:2px">как только он сделает первый пост · до 3 друзей в день</div></div>
<div style="margin-top:14px;font-size:14px;color:#A9A6BD">Реки+: 20 постов в день ›</div></div>{BTN("Позвать друга")}''')

a = '<div class="gift" style="margin-bottom:12px">🤝 Вернётся завтра: карточка «Дуэт» и его фанаты будут заходить к тебе</div>'
assert a in S['04-3_invite']
S['04-3_invite'] = S['04-3_invite'].replace(a, '<div class="small" style="color:#C9C6D8;margin-bottom:12px;text-align:center">Вернётся завтра: карточка «Дуэт» для вас обоих</div>')

# ---------- 5. Я и настройки ----------
coll = ''.join(f'<div class="slot" style="{"background:linear-gradient(135deg,#3A2147,#21324A)" if e else "background:#20202B;font-size:14px"}">{e or "?"}</div>' for e in ["🌊", "👑", "💛", "🌩", "🤝", ""])
stat = ''.join(f'<div class="card" style="flex:1;padding:10px;text-align:center"><div style="font-weight:800;font-size:16px">{v}</div><div class="small muted">{l}</div></div>'
               for v, l in [("✍️ 4", "постов осталось"), ("🔥 4", "дня подряд"), ("6 210", "подписчиков")])
S['05-5_collection'] = P(f'''<div class="top" style="border:none"><b style="font-size:18px">Я</b><div class="pill">⚙ Настройки</div></div>
<div style="padding:0 16px;text-align:center">{ava("Л","g1",64,26,"margin:0 auto 6px")}<b style="font-size:18px">Лиза · трейни</b><div class="muted small">🎤 Сцена · типаж: дерзкая с самоиронией</div></div>
<div style="padding:10px 16px"><div style="display:flex;gap:8px;margin-bottom:10px">{stat}</div>
<div class="card" style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;margin-bottom:8px"><b>Шкалы</b><span class="small" style="color:#C9A8FF">как поднять ›</span></div>{scales(72,64,58)}</div>
<div class="card" style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;margin-bottom:8px"><b>Коллекция 5/12</b><span class="small" style="color:#C9A8FF">все ›</span></div><div style="display:grid;grid-template-columns:repeat(6,1fr);gap:6px">{coll}</div></div>
<div class="card" style="display:flex;gap:10px;align-items:center"><div style="font-size:22px">🔒</div><div style="flex:1"><b>Мир «Академия»</b><div class="small muted">откроется на 10 000 · 62%</div></div><span class="small" style="color:#FFD66B">позвать друга ›</span></div></div>{tab5(4)}''')

def toggle(on=True):
    return (f'<span style="width:42px;height:26px;border-radius:13px;background:{"#2E9E6F" if on else "#33323F"};display:inline-block;position:relative;flex:none">'
            f'<i style="position:absolute;{"right" if on else "left"}:3px;top:3px;width:20px;height:20px;border-radius:50%;background:#fff"></i></span>')
def row(t, r='', sub='', last=False):
    sub_html = f'<div class="small muted">{sub}</div>' if sub else ''
    return f'<div style="display:flex;align-items:center;gap:10px;padding:12px 0;{"" if last else "border-bottom:1px solid #262634"}"><div style="flex:1">{t}{sub_html}</div><span class="muted">{r}</span></div>'
S.pop('05-8_me', None)
S['05-8_settings'] = P(f'''<div class="top"><span style="font-size:22px">‹</span><b>Настройки</b></div>
<div style="padding:14px 16px"><div class="card" style="padding:2px 14px;margin-bottom:12px">
{row("🔔 Уведомления","до 3 в день ›")}{row("🌙 Тихие часы","22:00–9:00 ›")}{row("🌩 Хейт-волны", toggle(True), "Выключи, если хочешь играть без хейта", True)}</div>
<div class="card" style="padding:2px 14px;margin-bottom:12px">{row("✨ Реки+","349 ₽ ›")}{row("🔒 Мои данные и правила","›")}{row("✉ Поддержка","›","",True)}</div>
<div class="card" style="text-align:center;color:#FF6B8B;background:transparent;border:1px solid #FF6B8B55">Удалить аккаунт и всё</div>
<div class="small muted" style="text-align:center;margin-top:8px">Удалим канал, переписку и коллекцию. Вернуть будет нельзя</div></div>''')

# ---------- 6. Безопасность ----------
s = S['06-1_report']
for a, b in [('<div style="font-size:20px;font-weight:800;margin-bottom:4px">Что не так?</div>',
              '<div style="display:flex;align-items:center;margin-bottom:4px"><div style="font-size:20px;font-weight:800;flex:1">Что не так?</div><span style="font-size:22px;color:#A9A6BD">✕</span></div>'),
             ('<button class="btn" style="margin-top:16px">Отправить</button>',
              '<button class="btn" style="margin-top:16px">Отправить</button><div style="text-align:center;margin-top:10px;color:#A9A6BD">Отмена</div>')]:
    assert a in s, a[:50]; s = s.replace(a, b, 1)
S['06-1_report'] = s

CRISIS_BTNS = ('<div style="position:absolute;bottom:34px;left:20px;right:20px"><button class="btn btn2" style="margin-bottom:10px">Вернуться в игру</button>'
               '<button class="btn btn2">Закрыть игру на сегодня</button></div>')
def crisis(text, call):
    call_html = ('<div class="btn" style="display:block;text-align:center;background:#2E9E6F;margin-top:14px">📞 Позвонить 8-800-2000-122</div>'
                 '<div class="small muted" style="text-align:center;margin-top:6px">детский телефон доверия · бесплатно и анонимно</div>') if call else ''
    return P(f'''<div class="top"><span style="font-size:22px">‹</span>{ava("М","g1",36,15)}<div><b>Мира</b><div class="small muted">лидер BLESK</div></div></div>
<div style="padding:16px">{bub("ты сегодня какая-то тихая. всё ок?")}{bub("не знаю. иногда кажется, что не хочу жить",True)}
<div class="card" style="margin-top:14px;border:1px solid #FFB86B66;background:#1E1A16"><div class="small" style="color:#FFB86B;margin-bottom:8px">ИГРА НА ПАУЗЕ · ЭТО ПИШЕТ НЕ МИРА</div>
<div style="font-size:16px">{text}</div>{call_html}
<div class="small muted" style="margin-top:12px">Это видишь только ты. Хейт в игре выключен на неделю</div></div></div>{CRISIS_BTNS}''')
S['06-2_crisis'] = crisis("Похоже, тебе сейчас тяжело. Если хочешь поговорить с живым человеком, тебе помогут:", True)
S['06-3_crisis_adult'] = crisis("Похоже, тебе сейчас тяжело. С этим не нужно справляться в одиночку. Поговори с тем, кому доверяешь, и, пожалуйста, обратись к психологу: это нормально и правда помогает", False)

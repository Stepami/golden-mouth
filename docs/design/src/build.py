# Генератор макетов «Рек»: HTML-экраны 390x844 -> PNG через headless Chrome.
import os, subprocess
D=os.path.dirname(os.path.abspath(__file__)); OUT=os.path.dirname(D)
CSS='''
*{box-sizing:border-box;margin:0;padding:0}
body{width:390px;height:844px;background:#0E0E13;color:#F2F1F7;font:15px/1.4 -apple-system,"SF Pro Text","Helvetica Neue",sans-serif;overflow:hidden;position:relative}
.status{height:44px;display:flex;justify-content:space-between;align-items:center;padding:0 22px;font-weight:600;font-size:15px}
.top{display:flex;align-items:center;gap:10px;padding:6px 16px 12px;border-bottom:1px solid #22222C}
.brand{font-weight:800;font-size:20px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF);-webkit-background-clip:text;color:transparent}
.pill{margin-left:auto;background:#1C1C26;border-radius:20px;padding:6px 12px;font-size:13px;color:#C9C6D8}
.ava{border-radius:50%;display:flex;align-items:center;justify-content:center;font-weight:700;color:#fff;flex:none}
.g1{background:linear-gradient(135deg,#FF6B8B,#FFB86B)}.g2{background:linear-gradient(135deg,#6BC5FF,#9B6BFF)}.g3{background:linear-gradient(135deg,#3A3A55,#8E7BFF)}.g4{background:linear-gradient(135deg,#6BFFB8,#2E9E8F)}.g5{background:linear-gradient(135deg,#FFD66B,#FF8A5C)}.g6{background:linear-gradient(135deg,#8A8A99,#50505E)}
.card{background:#181821;border-radius:18px;padding:14px}
.muted{color:#8E8BA3}.small{font-size:12px}
.btn{border:none;border-radius:14px;padding:14px;font-weight:700;font-size:16px;width:100%;color:#fff;background:linear-gradient(90deg,#FF6B8B,#9B6BFF)}
.btn2{background:#22222D;color:#F2F1F7}
.chip{display:inline-block;border-radius:20px;padding:8px 14px;background:#1C1C26;margin:0 6px 8px 0;font-size:14px}
.chip.on{background:linear-gradient(90deg,#FF6B8B,#9B6BFF);color:#fff;font-weight:600}
.react{white-space:nowrap;display:inline-flex;gap:4px;align-items:center;background:#22222D;border-radius:14px;padding:4px 9px;font-size:13px;margin-right:6px}
.tab{position:absolute;bottom:0;left:0;right:0;height:78px;background:#121219;border-top:1px solid #22222C;display:flex;justify-content:space-around;padding-top:12px;font-size:11px;color:#8E8BA3;text-align:center}
.tab div b{display:block;font-size:20px;font-weight:400}
.tab .on{color:#FF6B8B}
.label{position:absolute;top:50px;right:16px;font-size:10px;letter-spacing:.08em;color:#55536A;text-transform:uppercase}
.up{color:#6BFFB8}.down{color:#FF6B8B}
'''
def page(body,label=''): return f'<!doctype html><html><head><meta charset="utf-8"><style>{CSS}</style></head><body><div class="status"><span>21:14</span><span>●●● ▮</span></div>{body}</body></html>'
def ava(t,g,s=40,fs=15,ex=""): return f'<div class="ava {g}" style="width:{s}px;height:{s}px;font-size:{fs}px;{ex}">{t}</div>'
TAB='<div class="tab"><div class="{0}"><b>▦</b>Мой канал</div><div class="{1}"><b>≋</b>Лента</div><div class="{2}"><b>✉</b>Личка</div><div class="{3}"><b>★</b>Я</div></div>'
def tab(i): c=['']*4; c[i]='on'; return TAB.format(*c)
S={}
# 1 Онбординг
S['01_onboarding']=page(f'''
<div style="padding:18px 20px">
<div class="brand" style="font-size:26px">Реки</div>
<div style="font-size:26px;font-weight:800;margin:14px 0 6px">Какой канал заведёшь?</div>
<div class="muted" style="margin-bottom:16px">Выбери тему. Поменять можно позже.</div>
<span class="chip">💄 Бьюти</span><span class="chip on">😂 Юмор</span><span class="chip">🎤 Музыка</span><span class="chip">🎮 Игры</span><span class="chip">🌸 K-pop</span><span class="chip">🦊 Свой персонаж</span>
<div style="margin:18px 0 8px" class="muted small">НАЗВАНИЕ И НИК</div>
<div class="card" style="margin-bottom:8px">Лиза пишет</div>
<div class="card" style="margin-bottom:18px">@liza.writes</div>
<div class="muted small" style="margin-bottom:8px">ТАК ТЕБЯ УВИДЯТ В ЛЕНТЕ</div>
<div class="card" style="display:flex;gap:12px;align-items:center">{ava("Л","g1",48,20)}<div><b>Лиза пишет</b><div class="muted small">@liza.writes · 0 подписчиков</div><div class="small" style="margin-top:4px">смешно о том, о чём все молчат ✨ <span class="muted">(дописал AI)</span></div></div></div>
</div>
<div style="position:absolute;bottom:34px;left:20px;right:20px"><button class="btn">Дальше</button></div>''')
# 2 Первая звезда
stars=[('М','g1','Мира','@mira.blesk','Дерзкая','Говорит, что думает. Может раскритиковать, может сделать звездой.'),
('С','g4','Соня','@sonya.sunny','Добрая','Поддержит любой пост и придёт с фанатами.'),
('Е','g3','Ева','@eva.noir','Загадочная','Редко пишет. Но если написала, это событие.')]
cards=''.join(f'''<div class="card" style="display:flex;gap:12px;margin-bottom:12px;{'outline:2px solid #FF6B8B' if i==0 else ''}">{ava(a,g,52,22)}<div><b>{n}</b> <span class="muted small">{h}</span><div class="small" style="color:#FF6B8B;margin:2px 0">{t}</div><div class="small muted">{d}</div></div></div>''' for i,(a,g,n,h,t,d) in enumerate(stars))
S['02_first_star']=page(f'''<div style="padding:18px 20px"><div style="font-size:24px;font-weight:800;margin-bottom:6px">Кто подпишется на тебя первой?</div><div class="muted" style="margin-bottom:18px">Звёзды вымышленные, у каждой свой характер.</div>{cards}</div>
<div style="position:absolute;bottom:34px;left:20px;right:20px"><button class="btn">Мира, подписывайся</button></div>''')
# 3 Мой канал
S['03_my_channel']=page(f'''
<div class="top">{ava("Л","g1",36,15)}<div><b>Лиза пишет</b><div class="muted small">2 347 подписчиков</div></div><div class="pill">⚡ 11 действий</div></div>
<div style="padding:14px 16px">
<div class="card" style="margin-bottom:14px"><div class="muted">О чём напишешь сегодня?</div>
<div style="margin-top:12px;display:flex;gap:8px"><span class="react">💡 Подскажи</span><span class="react">@ подруга</span><span class="react" style="margin-left:auto;background:linear-gradient(90deg,#FF6B8B,#9B6BFF);color:#fff">Запостить</span></div></div>
<div class="card" style="margin-bottom:12px"><div class="small muted" style="margin-bottom:6px">сегодня, 20:41</div>
я официально самый медленный человек в спортзале. тренер сказал «не торопись», и я восприняла это слишком лично 🐢
<div style="margin-top:10px"><span class="react">😂 412</span><span class="react">🔥 96</span><span class="react">💀 31</span><span class="small muted" style="float:right;margin-top:4px">👁 8,4k · 💬 57</span></div></div>
<div class="card"><div class="small muted" style="margin-bottom:6px">вчера</div>покрасилась в розовый. мама сказала «ну ты хотя бы довольна?» 🌸
<div style="margin-top:10px"><span class="react">❤️ 238</span><span class="react">😂 77</span><span class="small muted" style="float:right;margin-top:4px">👁 5,1k · 💬 34</span></div></div>
</div>{tab(0)}''')
# 4 Тред
def cm(a,g,n,h,t,extra=''): return f'<div style="display:flex;gap:10px;margin-bottom:14px">{ava(a,g,34,14)}<div style="flex:1"><b>{n}</b> <span class="muted small">{h}</span>{extra}<div style="margin-top:2px">{t}</div></div></div>'
S['04_thread']=page(f'''
<div class="top"><span style="font-size:22px">‹</span><b>Комментарии</b><div class="pill">👁 8 412 ↑</div></div>
<div style="padding:14px 16px">
<div class="card" style="margin-bottom:16px">я официально самый медленный человек в спортзале. тренер сказал «не торопись», и я восприняла это слишком лично 🐢</div>
{cm("М","g1","Мира","@mira.blesk","ну хоть кто-то в этом зале честный 😭 репост",' <span class="small" style="color:#FF6B8B">★ звезда</span>')}
{cm("К","g5","Катя","@katya.fit","это же я!!! мы одна команда черепах")}
{cm("Д","g6","Диман","@kritik_42","зачем вообще идти в зал если так")}
{cm("С","g4","Соня","@sonya.sunny","@kritik_42 затем, что ей нравится. Лиза, ты лучшая 💛",' <span class="small" style="color:#FF6B8B">★ звезда</span>')}
<div class="muted small" style="display:flex;gap:8px;align-items:center">{ava("Е","g3",24,11)} Ева печатает…</div>
</div>
<div style="position:absolute;bottom:30px;left:16px;right:16px" class="card muted">Ответить в треде…</div>''')
# 5 Итог хода
S['05_turn_result']=page(f'''
'''+S['03_my_channel'].split('</div>',1)[1].rsplit('</body>',1)[0]+f'''<div style="position:absolute;inset:0;background:rgba(8,8,12,.72)"></div>
<div style="position:absolute;left:12px;right:12px;bottom:24px;background:#1A1A24;border-radius:26px;padding:22px">
<div class="muted small">СТАТИСТИКА ПОСТА</div>
<div style="font-size:40px;font-weight:800;margin:6px 0" class="up">+2 140 <span style="font-size:18px;color:#F2F1F7">подписчиков</span></div>
<div class="small muted" style="margin-bottom:16px">«Самоирония лучше любого фильтра»</div>
<div style="display:flex;gap:10px;margin-bottom:16px">
<div class="card" style="flex:1;text-align:center"><div style="font-size:20px;font-weight:700">8,4k</div><div class="small muted">охват</div></div>
<div class="card" style="flex:1;text-align:center"><div style="font-size:20px;font-weight:700">539</div><div class="small muted">реакций</div></div>
<div class="card" style="flex:1;text-align:center"><div style="font-size:20px;font-weight:700" class="up">+4%</div><div class="small muted">Мира</div></div></div>
<div class="card" style="margin-bottom:16px"><div class="small muted">ЛУЧШИЙ КОММЕНТАРИЙ</div><div style="margin-top:4px">«ну хоть кто-то в этом зале честный 😭 репост» · Мира</div></div>
<div style="display:flex;gap:10px"><button class="btn btn2" style="flex:1">↺ Переиграть</button><button class="btn" style="flex:1.4">Отправить в чат</button></div>
</div>''')
# 6 Личка
def bub(t,me=False,time=''): return f'<div style="display:flex;justify-content:{"flex-end" if me else "flex-start"};margin-bottom:8px"><div style="max-width:78%;padding:9px 12px;border-radius:18px;{"background:linear-gradient(135deg,#FF6B8B,#9B6BFF);border-bottom-right-radius:6px" if me else "background:#1E1E29;border-bottom-left-radius:6px"}">{t}<span class="small" style="opacity:.6;margin-left:6px">{time}</span></div></div>'
S['06_dm']=page(f'''
<div class="top"><span style="font-size:22px">‹</span>{ava("М","g1",36,15)}<div><b>Мира</b><div class="small" style="color:#FF6B8B">в сети · настроение: заинтригована</div></div></div>
<div style="padding:16px">
<div class="muted small" style="text-align:center;margin-bottom:12px">Мира помнит: розовые волосы, спортзал, Катя</div>
{bub("слушай, твой пост про зал разлетелся. ты теперь официально лицо всех черепах 🐢",False,"21:02")}
{bub("ахах я сама в шоке",True,"21:03")}
{bub("у меня идея. давай коллаб? я снимаю, как тренирую тебя, ты комментируешь",False,"21:03")}
{bub("а если я опозорюсь?",True,"21:05")}
{bub("вот именно поэтому это будет лучший контент месяца 😏",False,"21:05")}
<div class="muted small" style="margin-top:8px;display:flex;gap:6px;align-items:center"><span style="color:#6BFFB8">Отношения с Мирой +3%</span></div>
</div>
<div style="position:absolute;bottom:30px;left:16px;right:16px;display:flex;gap:8px"><div class="card muted" style="flex:1">Сообщение…</div><div class="card">💡</div></div>''')
# 7 Лимит
S['07_limit']=page(f'''
<div class="top">{ava("Л","g1",36,15)}<div><b>Лиза пишет</b><div class="muted small">4 487 подписчиков</div></div><div class="pill">⚡ 0</div></div>
<div style="padding:22px 20px;text-align:center">
{ava("М","g1",84,34,"margin:20px auto 16px")}
<div style="font-size:22px;font-weight:800;margin-bottom:8px">Мира ответит утром</div>
<div class="muted" style="margin-bottom:24px">Она прочитала про коллаб и сказала «надо подумать 👀». Действия на сегодня закончились. Утром будут новые, и Мира ответит.</div>
<div class="card" style="text-align:left;margin-bottom:10px;border:1px solid #FF6B8B55"><b>Позвать подругу</b><div class="small muted">+10 действий тебе и ей, когда она сделает первый пост</div></div>

</div>
<div style="position:absolute;bottom:34px;left:20px;right:20px"><button class="btn">Позвать подругу</button></div>''')
# 8 Уведомление подруге в Telegram
S['08_mention_push']=page(f'''
<div class="top"><span style="font-size:22px">‹</span>{ava("Р","g2",36,15)}<div><b>Реки</b><div class="muted small">бот</div></div></div>
<div style="padding:16px">
<div style="background:#1E1E29;border-radius:18px;border-bottom-left-radius:6px;padding:12px;max-width:88%">
<b>Про тебя написали в Реках 👀</b>
<div class="card" style="margin:10px 0;background:#262634"><div class="small muted">Лиза пишет · @liza.writes</div>
<div style="margin-top:4px">мы с @katya.fit единственные, кто в зале приходит ради сплетен, а не ради пресса 😂</div>
<div class="small muted" style="margin-top:6px">👁 3,2k · 😂 214 · 💬 28</div></div>
<div class="small muted">Мира и ещё 27 человек уже обсуждают. Твой ответ увидят все.</div>
</div>
<div style="max-width:88%;margin-top:6px;display:flex;flex-direction:column;gap:6px">
<div class="card" style="text-align:center;padding:10px;color:#9BB8FF">Посмотреть</div>
<div class="card" style="text-align:center;padding:10px;color:#9BB8FF">Завести свой канал</div></div>
</div>''')
# 9 Карточка для сторис (9:16)
S['09_story_card']=f'''<!doctype html><html><head><meta charset="utf-8"><style>{CSS}
body{{background:linear-gradient(160deg,#2A1236,#0E0E13 55%,#12203A)}}</style></head><body>
<div style="padding:70px 30px;text-align:center">
<div class="brand" style="font-size:30px">Реки</div>
{ava("Л","g1",110,44,"margin:40px auto 16px")}
<div style="font-size:26px;font-weight:800">Лиза пишет</div>
<div class="muted">@liza.writes</div>
<div style="font-size:64px;font-weight:900;margin:24px 0 0;background:linear-gradient(90deg,#FF6B8B,#FFB86B);-webkit-background-clip:text;color:transparent">10 000</div>
<div style="font-size:18px;margin-bottom:30px">подписчиков за 5 дней</div>
<div class="card" style="text-align:left"><div class="small muted">ЛУЧШИЙ ПОСТ</div><div style="margin-top:4px">я официально самый медленный человек в спортзале 🐢</div><div class="small muted" style="margin-top:6px">👁 8,4k · Мира сделала репост</div></div>
<div class="muted small" style="margin-top:34px">а ты сколько наберёшь? · t.me/reki_appbot</div>
</div></body></html>'''
# 10 Вехи
steps=[('1 000','✓','Первые фанаты',True),('10 000','✓','Мира зовёт на коллаб',True),('100 000','24%','Премия Рек',False),('1 000 000','','Легенда',False)]
rows=''.join(f'<div class="card" style="display:flex;align-items:center;gap:12px;margin-bottom:10px;{"" if d else "opacity:.8"}"><div style="width:34px;height:34px;border-radius:50%;display:flex;align-items:center;justify-content:center;{"background:linear-gradient(135deg,#FF6B8B,#9B6BFF)" if d else "background:#262634"};font-size:13px;font-weight:700">{m if m else "🔒"}</div><div style="flex:1"><b>{n}</b><div class="small muted">{t}</div></div></div>' for n,m,t,d in steps)
S['10_milestones']=page(f'''
<div class="top"><b style="font-size:18px">Путь к миллиону</b><div class="pill">🔥 5 дней подряд</div></div>
<div style="padding:16px">
<div class="card" style="margin-bottom:16px"><div class="muted small">СЕЙЧАС</div><div style="font-size:30px;font-weight:800">12 480</div><div style="height:8px;background:#262634;border-radius:4px;margin-top:10px"><div style="width:24%;height:8px;border-radius:4px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF)"></div></div><div class="small muted" style="margin-top:6px">до 100 000 осталось 87 520</div></div>
{rows}
<div class="card" style="margin-top:6px"><b>Серия дней</b><div style="display:flex;gap:6px;margin-top:10px">{"".join(f'<div style="flex:1;height:28px;border-radius:8px;{"background:linear-gradient(135deg,#FF6B8B,#9B6BFF)" if i<5 else "background:#262634"}"></div>' for i in range(7))}</div><div class="small muted" style="margin-top:6px">7 дней подряд: +10 действий и рамка профиля</div></div>
</div>{tab(3)}''')

# 11 Лента
def fpost(a,g,n,h,t,txt,r,extra=''):
    return f'<div class="card" style="margin-bottom:12px"><div style="display:flex;gap:10px;align-items:center;margin-bottom:8px">{ava(a,g,36,15)}<div><b>{n}</b> <span class="muted small">{h}</span><div class="small muted">{t}</div></div></div>{extra}{txt}<div style="margin-top:10px">{r}</div></div>'
S['11_feed']=page(f"""
<div class="top"><b style="font-size:18px">Лента</b><div class="pill">3 канала</div></div>
<div style="padding:14px 16px">
{fpost("М","g1","Мира","@mira.blesk","12 мин назад","репостнула к себе. читайте, пока она не удалила от стеснения 😏",'<span class="react">🔥 2,1k</span><span class="react">😂 840</span><span class="small muted" style="float:right;margin-top:4px">👁 190k</span>','<div style="border-left:3px solid #FF6B8B;padding:6px 10px;margin-bottom:8px;background:#20202B;border-radius:0 10px 10px 0"><div class="small" style="color:#FF6B8B;font-weight:600">Лиза пишет · в реках 🌊</div><div class="small">я официально самый медленный человек в спортзале…</div></div>')}
{fpost("Е","g3","Ева","@eva.noir","вчера, 02:14","иногда самый громкий пост это тот, который ты не выложила.",'<span class="react">🖤 14k</span><span class="react">🌙 3,2k</span><span class="small muted" style="float:right;margin-top:4px">👁 1,1M</span>')}
{fpost("С","g4","Соня","@sonya.sunny","вчера, 18:30","девочки, кто сегодня тоже весь день в пледе? показывайте свои чашки ☕️💛",'<span class="react">🥹 9,4k</span><span class="react">☕️ 2k</span><span class="small muted" style="float:right;margin-top:4px">👁 640k</span>')}
</div>{tab(1)}""")
# 12 Я
S['12_me']=page(f"""
<div style="padding:10px 16px;text-align:center">{ava("Л","g1",76,30,"margin:4px auto 8px")}<b style="font-size:20px">Лиза пишет</b><div class="muted small">@liza.writes · 😂 Юмор</div>
<div style="display:flex;gap:8px;margin:14px 0">{"".join(f'<div class="card" style="flex:1;padding:10px"><div style="font-weight:800;font-size:18px">{v}</div><div class="small muted">{l}</div></div>' for v,l in [("12 480","подписчиков"),("🔥 5","дней подряд"),("2/4","вехи")])}</div></div>
<div style="padding:0 16px">
<div class="card" style="margin-bottom:12px"><div style="display:flex;justify-content:space-between"><b>До 100 000</b><span class="small muted">24%</span></div><div style="height:8px;background:#262634;border-radius:4px;margin-top:10px"><div style="width:24%;height:8px;border-radius:4px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF)"></div></div><div class="small muted" style="margin-top:6px">Откроется: Премия Рек и новая звезда</div></div>
<div class="card" style="margin-bottom:12px;background:linear-gradient(135deg,#2A1A2E,#1E1A33)"><b>Позови подругу: +10 действий обеим</b><div class="small muted" style="margin:4px 0 10px">Она заводит свой канал, вы видите друг друга в ленте</div><button class="btn" style="padding:11px;font-size:15px">Отправить ссылку</button></div>
<div class="card" style="margin-bottom:12px;display:flex;align-items:center;gap:10px"><div style="font-size:22px">✨</div><div style="flex:1"><b>Реки+</b><div class="small muted">Больше действий и оформление канала</div></div><span class="react">349 ₽/мес</span></div>
<div class="card" style="padding:4px 14px">{"".join(f'<div style="padding:11px 0;{"border-bottom:1px solid #262634" if i<2 else ""};{"color:#FF6B8B" if i==2 else ""}">{t}</div>' for i,t in enumerate(["Правила и данные","Уведомления бота","Удалить аккаунт"]))}</div>
</div>{tab(3)}""")
# 13 Состояния
def st(title,body): return f'<div class="card" style="margin-bottom:12px"><div class="muted small" style="margin-bottom:8px">{title}</div>{body}</div>'
S['13_states']=page(f"""
<div class="top"><b style="font-size:18px">Состояния</b></div>
<div style="padding:14px 16px">
{st("ЗАГРУЗКА ПОСТА",'<div style="height:12px;width:80%;background:#262634;border-radius:6px;margin-bottom:8px"></div><div style="height:12px;width:55%;background:#262634;border-radius:6px;margin-bottom:10px"></div><div class="small muted">👁 12… 48… 131… · Мира печатает…</div>')}
{st("ОШИБКА ГЕНЕРАЦИИ",'<b>Звёзды задумались 🤔</b><div class="small muted" style="margin:4px 0 10px">Действие вернулось. Попробуй ещё раз через минуту.</div><button class="btn btn2" style="padding:10px;font-size:14px">Повторить</button>')}
{st("ПУСТОЙ КАНАЛ",'<b>Твой канал пока пустой</b><div class="small muted" style="margin:4px 0 10px">Мира уже подписалась и ждёт. Черновик готов:</div><div class="small" style="background:#20202B;border-radius:10px;padding:10px;margin-bottom:10px">«завела канал. если через неделю тут будет 1000 человек, я съем лимон на камеру 🍋»</div><button class="btn" style="padding:10px;font-size:14px">Запостить</button>')}
{st("ПОСТ НЕ ПРОШЁЛ",'<b>Этот пост не опубликуем</b><div class="small muted" style="margin-top:4px">Похоже, в нём номер телефона. Звёздам он ни к чему. Действие вернулось.</div>')}
{st("ФЕЙКОВАЯ ДВЕРЬ",'<b>Реки+ ещё в работе ✨</b><div class="small muted" style="margin-top:4px">Спасибо, что заглянула! Бот напишет, когда подписка появится. А пока держи +5 действий.</div>')}
</div>""")
# UI-кит
sw=[("#0E0E13","Фон"),("#181821","Карточка"),("#22222D","Кнопка 2"),("#F2F1F7","Текст"),("#8E8BA3","Вторичный"),("#FF6B8B","Коралл"),("#9B6BFF","Фиолет"),("#6BFFB8","Рост")]
KIT=f"""<!doctype html><html><head><meta charset="utf-8"><style>{CSS} body{{width:1200px;height:900px;padding:40px}} h2{{font-size:13px;letter-spacing:.1em;color:#8E8BA3;margin:0 0 14px}} .col{{flex:1}}</style></head><body>
<div style="display:flex;align-items:center;gap:14px;margin-bottom:30px"><img src="../../logo/C_monogram_star.svg" width="56"><div class="brand" style="font-size:34px">Реки · UI-кит</div><div class="muted" style="margin-left:auto">390×844 pt, тёмная тема, отступ экрана 16, радиус карточки 18, кнопки 14</div></div>
<div style="display:flex;gap:40px">
<div class="col"><h2>ЦВЕТА</h2><div style="display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin-bottom:30px">{"".join(f'<div><div style="height:60px;border-radius:12px;background:{c};border:1px solid #2A2A36"></div><div class="small" style="margin-top:6px">{n}</div><div class="small muted">{c}</div></div>' for c,n in sw)}</div>
<div style="height:40px;border-radius:12px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF);margin-bottom:6px"></div><div class="small muted" style="margin-bottom:30px">Градиент бренда: главная кнопка, активные чипсы, вехи, прогресс</div>
<h2>ТЕКСТ (SF Pro / системный)</h2>
<div style="font-size:26px;font-weight:800">Заголовок экрана 26/800</div><div style="font-size:20px;font-weight:700;margin:6px 0">Название 20/700</div><div>Основной текст поста 15/400</div><div class="small muted" style="margin-top:6px">Подпись и время 12/400, вторичный цвет</div></div>
<div class="col"><h2>КНОПКИ</h2><button class="btn" style="margin-bottom:10px">Главная</button><button class="btn btn2" style="margin-bottom:24px">Вторичная</button>
<h2>ЧИПСЫ И РЕАКЦИИ</h2><div style="margin-bottom:10px"><span class="chip on">😂 Юмор</span><span class="chip">💄 Бьюти</span></div><div style="margin-bottom:24px"><span class="react">😂 412</span><span class="react">🔥 96</span><span class="react">💡 Подскажи</span></div>
<h2>АВАТАРКИ</h2><div style="display:flex;gap:10px;margin-bottom:24px">{"".join(ava(a,g,44,18) for a,g in [("Л","g1"),("С","g4"),("Е","g3"),("М","g1"),("к","g5"),("х","g6")])}</div>
<h2>РОСТ</h2><div style="font-size:22px;font-weight:800"><span class="up">+842</span> · <span class="down">−37</span></div></div>
<div class="col"><h2>КАРТОЧКА ПОСТА</h2><div class="card"><div class="small muted" style="margin-bottom:6px">сегодня, 20:41</div>текст поста до 500 символов<div style="margin-top:10px"><span class="react">😂 412</span><span class="small muted" style="float:right;margin-top:4px">👁 8,4k · 💬 57</span></div></div>
<h2 style="margin-top:24px">ШАПКА</h2><div class="top" style="border:1px solid #22222C;border-radius:14px">{ava("Л","g1",36,15)}<div><b>Лиза пишет</b><div class="muted small">2 347 подписчиков</div></div><div class="pill">⚡ 11</div></div>
<h2 style="margin-top:24px">ТАББАР</h2><div style="position:relative;height:78px;border-radius:14px;overflow:hidden">{tab(0)}</div></div>
</div></body></html>"""
CH='/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
SIZE={}
S.pop('10_milestones',None)  # вехи вошли в экран «Я»
S['ui-kit']=KIT; SIZE['ui-kit']='1200,900'
for k,h in S.items():
    f=f'{D}/{k}.html'; open(f,'w').write(h)
    png=f'{OUT}/{k}.png'
    if os.path.exists(png): os.remove(png)
    pr=subprocess.Popen([CH,'--headless=new','--user-data-dir=/tmp/reki-chrome-'+k,'--no-first-run','--disable-gpu','--hide-scrollbars','--force-device-scale-factor='+('2' if k in SIZE else '3'),'--window-size='+SIZE.get(k,'390,844'),f'--screenshot={png}',f'file://{f}'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    import time
    for _ in range(120):
        time.sleep(0.5)
        if os.path.exists(png) and os.path.getsize(png)>1000: time.sleep(1); break
    pr.kill(); print(k, os.path.exists(png))
print('ok',len(S))

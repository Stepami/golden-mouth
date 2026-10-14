# 15.10.2026: approved narrative goals; guidance has no completion or rewards.
goal_evening = 'Раскрыть тайну исчезающих улиц и найти тех, кто помнит город таким же, как ты.'
goal_school = 'Разгадать тайну меняющейся школы и найти друзей, которым можно доверять.'
step_evening = 'Опубликуй находку на фотографии и спроси жителей, кто узнаёт исчезнувшую улицу.'
step_school = 'Расскажи об уроке, отменённом сто лет назад, и выясни, кому ещё пришло приглашение.'

def story_guidance(goal, step=None):
    return '<details class="card" style="margin-bottom:16px"><summary><b>Твой путь</b> <span class="small muted">в этой истории</span></summary><p>' + goal + '</p>' + ('<b>Первый шаг</b><p>' + step + '</p>' if step else '') + '<p class="small muted">Можно идти своим путём — это твоя история.</p></details>'

S['01-6_home'] = release_page('Моя история · Вечерело · ⚡ 10', world_cover_html('evening') + '<div class="card"><span class="small muted">Твой путь</span><h1 style="font-size:22px">' + goal_evening + '</h1><p class="small muted">Сегодня всё начинается</p><p>На твоей фотографии появилась улица, снесённая сорок лет назад.</p><div style="padding:12px;background:#251d32;border-radius:14px"><b>Первый шаг</b><p>' + step_evening + '</p></div></div><p class="small muted">Можно начать своими словами — это твоя история.</p>', 'Написать первый пост')
S['02-1_compose'] = release_page('Новый пост · ⚡ 10', story_guidance(goal_school, step_school) + release_field('Твой пост', 'Мой сохранённый черновик') + '<p class="small muted">О чём написать</p><div class="card">В расписании появился урок, который отменили сто лет назад…</div><div class="card">Мой первый магический эксперимент закончился…</div><p class="small muted">Идею можно дописать своими словами.</p>', 'Опубликовать')
S['03-5_stage'] = release_page('Моя история · Школа чародеев · ⚡ 10', story_guidance(goal_school) + '<div class="card"><b>Навыки · из 10</b><p>смелость · юмор · в тему</p></div><p>Позвать друга</p><div class="card"><span class="small muted">15 окт. · в реках</span><p>Кто ещё получил приглашение на этот урок?</p></div>', 'Новый пост')
S['01-3b_world_overview'] = S['01-3b_world_overview'].replace('<h2>Кого встретишь</h2>', '<div class="card"><span class="small muted">Твой путь</span><p>' + goal_school + '</p></div><h2>Кого встретишь</h2>')

S['01-1_invite'] = release_page('реки', '<h1 style="font-size:38px;line-height:1.1">Твой пост может<br>взорвать ленту.</h1><p class="muted">Пиши посты от лица персонажа, получай ответы героев и развивай свою историю.</p><div class="card"><b>Гоша · Школа чародеев</b><p>В расписании появился урок, отменённый сто лет назад. Почему в списке моё имя? 👀</p><p class="small muted">✨ 24　👀 18　💬 3</p><b>Георгина · ученица школы</b><p>Моё тоже. Встретимся у библиотеки? Покажу запись с этого урока… она появилась вчера.</p></div><p>Выбери мир или создай свой.</p><p class="small muted">Фанаты и события виртуальные. Посты остаются внутри игры.</p>', 'Начать играть', False)

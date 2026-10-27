# #215: one mechanical difficulty, selected independently of the world's setting.
difficulty_picker = '<section><h2>Сложность</h2><span class="chip">Легко</span><span class="chip on">Средне</span><span class="chip">Сложно</span><p class="small muted">Чем сложнее, тем быстрее рост подписчиков и выше риск провала. Изменение действует на следующие действия.</p></section>'
marker = '<h1>Сколько драмы добавим?</h1>'
assert marker in S['01-4b_world_setup']
S['01-4b_world_setup'] = S['01-4b_world_setup'].replace(marker, difficulty_picker + marker, 1)
for name in ('05-11_world_edit', '05-12_world_advanced'):
    if name in S:
        S[name] = S[name].replace('<h1>', difficulty_picker + '<h1>', 1)

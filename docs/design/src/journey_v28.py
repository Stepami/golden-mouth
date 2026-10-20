# #189: user-approved hierarchy and separation of current-world settings.
edit = '<div class="card" style="text-align:center">Редактировать мир</div><p class="small muted">Мир можно будет отредактировать позже</p>'
assert edit in S['01-4b_world_setup']
S['01-4b_world_setup'] = S['01-4b_world_setup'].replace(edit, '', 1)
intro = '<p class="muted">Выбери настроение истории. Его можно изменить позже.</p>'
assert intro in S['01-4b_world_setup']
S['01-4b_world_setup'] = S['01-4b_world_setup'].replace(intro, intro + '<div style="margin:16px 0"><div class="card" style="text-align:center;background:#22222D;font-weight:700">Редактировать мир</div><p class="small muted" style="margin-top:8px">Мир можно будет отредактировать позже</p></div>', 1)
settings = '<div class="card" style="text-align:center;margin:12px 0">Настройки мира</div>'
assert settings in S['05-5_collection']
S['05-5_collection'] = S['05-5_collection'].replace(settings, '', 1)
invite = '<p>Пригласить друга →</p></div>'
assert invite in S['05-5_collection']
S['05-5_collection'] = S['05-5_collection'].replace(invite, invite + '<div class="card" style="text-align:center;margin:12px 0 18px;background:#22222D;font-weight:700">Настройки мира</div>', 1)

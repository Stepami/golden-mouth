# #189: visible world settings and contextual editor return, 20.10.2026.
marker = '<h1>Сколько драмы добавим?</h1>'
assert marker in S['01-4b_world_setup']
S['01-4b_world_setup'] = S['01-4b_world_setup'].replace(marker, '<div class="card" style="text-align:center">Редактировать мир</div><p class="small muted">Мир можно будет отредактировать позже</p>' + marker, 1)
marker = '<p>Этот мир: 209 / 500</p>'
assert marker in S['05-5_collection']
S['05-5_collection'] = S['05-5_collection'].replace(marker, marker + '<div class="card" style="text-align:center;margin:12px 0">Настройки мира</div>', 1)
import re
S['05-8_settings'] = re.sub(r'<div class="why"[^>]*><span>📜 Как устроен мир</span>.*?</div>', '', S['05-8_settings'])

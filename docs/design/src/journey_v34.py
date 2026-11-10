# #268: profile linking replaces the idea card on the profile screen.
old_idea = '<div class="card" style="margin-top:14px"><b>🎯 Задания дня · 0/3</b><p class="small muted">Поделись наблюдением о мире</p><p>Открыть →</p></div>'
link_button = '<button class="btn" style="width:100%;box-sizing:border-box;margin-top:14px;background:#22222D">🔑 Привязать профиль</button>'
assert old_idea in S['05-5_collection']
S['05-5_collection'] = S['05-5_collection'].replace(old_idea, link_button, 1)

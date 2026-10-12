# #87: общая энергия и AI-ход треда, 12.10.2026.
# Старые слои остаются историей; этот слой задаёт текущую стоимость и состояние ожидания.
for key in S:
    for old, new in {
        '+5 постов': '+5 энергии', '+10 постов': '+10 энергии',
        '10 постов в день': '10 энергии в день', '20 постов в день': '20 энергии в день',
        'Посты на сегодня всё': 'Энергия на сегодня закончилась',
        'ответы в тредах ·': 'чтение тредов ·',
        'ответы на комментарии к своим постам ·': 'чтение тредов ·',
    }.items():
        S[key] = S[key].replace(old, new)

post_text = 'первый день трейни: перепутала право и лево три раза. хореограф сказал «зато уверенно» 🫠'
root_comment = cmt('М', 'g1', 'Мира', 'лидер BLESK', 'я на первой репетиции упала со сцены. «уверенно» это комплимент, поверь', True)
reply = cmt('Л', 'g1', 'Лиза · трейни', '', 'серьёзно? со сцены?? 😳')
ai_reply = cmt('М', 'g1', 'Мира', 'лидер BLESK', 'прямо в оркестровую яму. теперь тоже смеюсь 🙃', True)
head = '<div class="top"><span style="font-size:24px">‹</span><b style="flex:1">Реакции</b><span class="pill">⚡ 8</span></div>'
form = '<div style="position:absolute;bottom:30px;left:16px;right:16px;display:flex;gap:8px"><div class="card muted" style="flex:1">Ответ · 1 энергия</div><div class="card" style="padding:14px 18px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF)">➤</div></div>'
body = f'<div style="padding:16px"><div class="card">{post_text}<div style="margin-top:8px"><span class="react">😂 48</span><span class="react">🫶 21</span></div></div><p class="small" style="color:#FFD66B">💬 Ответ — 1 энергия · первые 3 ответа дают +9 подписчиков</p>{root_comment}<div style="margin-left:14px">{reply}<p class="small" style="margin-left:44px;color:#FF6B8B">💛 Мира</p></div><div style="margin-left:28px">{ai_reply}</div></div>'
S['02-6_thread'] = P(head + body + form)
pending = f'<div style="padding:16px"><div class="card">{post_text}</div><p class="small" style="color:#FFD66B">💬 Ответ — 1 энергия · первые 3 ответа дают +9 подписчиков</p>{root_comment}<div class="card" role="status"><div class="small muted">Мира готовит ответ…</div><div style="height:4px;border-radius:4px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF);margin:14px 0"></div><p>прямо в оркестровую яму. теперь тоже смеюсь 🙃</p><div class="small muted">Ответ ещё не опубликован</div></div></div>'
S['02-6b_thread_pending'] = P(head.replace('⚡ 8', '⚡ 9') + pending + form.replace('Ответ · 1 энергия', 'серьёзно? со сцены?? 😳'))

# #297: every saved post gets the same next actions, without waiting for AI.
def publication297(delta, reason, reply='Здесь есть о чём поспорить.', state='ready'):
    reaction = reply if state == 'ready' else 'Персонаж готовит ответ…' if state == 'pending' else 'Ответ персонажа пока не загрузился.'
    number = ('+' if delta > 0 else '−' if delta < 0 else '') + str(abs(delta))
    return P(f'''<div class="top"><b>Канал</b></div><div style="position:absolute;bottom:0;left:0;right:0;padding:22px 20px;background:#1A1A24;border-radius:24px 24px 0 0"><div style="display:flex;justify-content:space-between;color:#A9A6BD;font-size:14px;margin-bottom:20px"><span>Пост опубликован</span><span>×</span></div><div style="display:flex;gap:10px;align-items:center">{ava('М','',40)}<div><b>Персонаж</b><div class="small muted">Мир · AI-персонаж</div></div></div><p style="line-height:1.5;margin:12px 0 20px">{reaction}</p><div style="font-size:32px;font-weight:800;color:{'#6BFFB8' if delta>0 else '#FF6B8B' if delta<0 else '#F2F1F7'}">{number} <span style="font-size:16px;font-weight:500">подписчиков</span></div><p class="small muted" style="margin-top:6px">{reason}</p><details style="margin:16px 0"><summary style="color:#C9A8FF">Разбор поста</summary><p class="small">В тему · Старт канала ×2</p></details><button class="btn" style="width:100%;box-sizing:border-box;margin-bottom:8px">Новый пост</button><button class="btn" style="width:100%;box-sizing:border-box;background:#22222D">Комментировать</button><button style="display:block;margin:8px auto 0;min-height:44px;background:none;border:0;color:#C9A8FF;font-size:15px">Поделиться</button></div>''')
S['02-3_result'] = publication297(38, 'Канал растёт')
S['02-4_flop'] = publication297(-4, 'Грустный тон')
S['02-5_viral'] = publication297(620, 'Пост залетел')
S['02-3a_result_waiting'] = publication297(38, 'Канал растёт', state='pending')
S['02-3b_result_error'] = publication297(38, 'Канал растёт', state='failed')

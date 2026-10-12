# #87: продолжение треда без ограничения глубины, 12.10.2026.
reply_action = '<div class="small" style="margin:-8px 0 12px 44px;color:#C3A8FF">Ответить</div>'
S['02-6_thread'] = S['02-6_thread'].replace(root_comment, root_comment + reply_action).replace(ai_reply, ai_reply + reply_action)

# #243: ML result is saved before the optional first star comment.
S['02-2_live'] = release_page(
    'Новый пост',
    '<div class="card"><div class="small muted">готовим публикацию</div>'
    '<p>На моей фотографии появилась улица, которой вчера не было.</p></div>'
    '<div class="card" role="status" style="margin-top:16px"><b>Оцениваем пост</b>'
    '<div style="height:4px;border-radius:4px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF);margin:14px 0"></div>'
    '<p class="small muted">Итог появится после сохранения. Ответ героя придёт отдельно.</p></div>',
    'Публикуем пост',
)

S['02-2b_publish_error'] = release_page(
    'Публикация',
    '<div class="card"><h1>Проверим публикацию</h1>'
    '<p class="muted">Не получилось получить подтверждение. Текст сохранён.</p>'
    '<p class="small muted">Повторная проверка не создаст второй пост.</p></div>',
    'Проверить публикацию',
)

for key in ['02-3_result', '02-4_flop', '02-5_viral']:
    S[key] = S[key].replace(
        '<details',
        '<p class="small muted" role="status">Итог сохранён · герой готовит ответ</p><details',
        1,
    )

S['06-4_rules'] = S['06-4_rules'].replace(
    'Отказ не расходует доступный пост; черновик остаётся.',
    'Если пост не прошёл проверку до публикации, энергия сохраняется. '
    'После публикации её итог сохраняется, даже если ответ героя ещё не готов.',
)

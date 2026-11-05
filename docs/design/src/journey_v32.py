# #243: the visible post is saved before evaluation and the first star comment.
S['02-2_live'] = release_page(
    'Моя история',
    '<div class="card"><div class="small muted">опубликовано · оценка готовится</div>'
    '<p>На моей фотографии появилась улица, которой вчера не было.</p></div>'
    '<div class="card" role="status" style="margin-top:16px">'
    '<b>Звезда готовит ответ</b>'
    '<div style="height:4px;border-radius:4px;background:linear-gradient(90deg,#FF6B8B,#9B6BFF);margin:14px 0"></div>'
    '<p class="small muted">Пост уже в канале. Итог появится после оценки.</p></div>',
    'Публикация готовится',
)

S['02-2b_publish_error'] = release_page(
    'Публикация',
    '<div class="card"><h1>Пост опубликован</h1>'
    '<p class="muted">Оценка и первый ответ ещё не готовы. Пост остаётся в канале, энергия уже потрачена.</p>'
    '<p class="small muted">Продолжи, чтобы получить ответ и итог.</p></div>',
    'Продолжить публикацию',
)

S['06-4_rules'] = S['06-4_rules'].replace(
    'Отказ не расходует доступный пост; черновик остаётся.',
    'Если текст не прошёл проверку до публикации, энергия сохраняется. '
    'После публикации пост остаётся в канале, даже если ответ игры ещё не готов.',
)

# #245: saved ML result, followed by the shared comment generation flow.
S['02-2_live'] = release_page(
    'Моя история',
    '<div class="card"><div class="small muted">только что опубликовано</div>'
    '<p>На моей фотографии появилась улица, которой вчера не было.</p>'
    '<b>👁 84</b> <span class="react">🤔 1</span></div>'
    '<div class="card" style="margin-top:14px"><b>Изабелла Лебедева</b>'
    '<p class="muted">Печатает...</p></div>'
    '<p class="small muted">Появляются реакции…</p>',
    'Смотрим реакцию',
)

S['02-2b_publish_error'] = release_page(
    'Публикация',
    '<div class="card"><h1>Пост не опубликован</h1>'
    '<p class="muted">Так не получится, попробуй по-другому. Черновик сохранён.</p>'
    '<span class="chip">Правила публикаций</span></div>',
    'Изменить текст',
)

S['02-4_flop'] = S['02-4_flop'].replace('−18', '−1').replace(
    'Пост не зашёл', 'Часть подписчиков ушла'
).replace('Поддержка без давления на выбор', 'Опыт за пост засчитан').replace(
    'color:#6BFFB8', 'color:#FF6B8B'
).replace('<p class="muted">подписчиков</p>', '<p class="muted">подписчик</p>')

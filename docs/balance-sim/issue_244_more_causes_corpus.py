"""Research-only synthetic channel-closure cases; no model scores or game rules.

Labels describe an explicit, unconditional end to posting in the current channel.
Temporary pauses, another channel, negation, conditions and quotations are negative.
All cases are exploratory and must not be reported as independent validation.
"""

cases = [
    {
        "id": "closure_01_yes",
        "cause": "channel_closure",
        "expected": True,
        "text": "Больше не буду вести этот канал. Новых постов здесь не будет.",
    },
    {
        "id": "closure_01_no",
        "cause": "channel_closure",
        "expected": False,
        "text": "Сегодня новых постов здесь не будет. Завтра продолжу вести канал.",
    },
    {
        "id": "closure_02_yes",
        "cause": "channel_closure",
        "expected": True,
        "text": "Закрываю этот канал навсегда. Публиковать здесь больше ничего не буду.",
    },
    {
        "id": "closure_02_no",
        "cause": "channel_closure",
        "expected": False,
        "text": "Закрываю набор на курс навсегда. Сам канал продолжу вести, завтра выйдет новый пост.",
    },
    {
        "id": "closure_03_yes",
        "cause": "channel_closure",
        "expected": True,
        "text": "Это последний пост в этом канале. Я окончательно прекращаю публикации. Спасибо за всё.",
    },
    {
        "id": "closure_03_no",
        "cause": "channel_closure",
        "expected": False,
        "text": "Это последний пост в этом канале за сегодня. Утром продолжу публикации. Спасибо за всё.",
    },
    {
        "id": "closure_04_yes",
        "cause": "channel_closure",
        "expected": True,
        "text": "Мой канал прекращает работу. Я больше не буду писать здесь посты.",
    },
    {
        "id": "closure_04_no",
        "cause": "channel_closure",
        "expected": False,
        "text": "Канал соседа прекращает работу. Свой канал я продолжаю вести и завтра опубликую обзор.",
    },
    {
        "id": "closure_05_yes",
        "cause": "channel_closure",
        "expected": True,
        "text": "Я решил закрыть этот канал. Постов здесь больше не будет, решение окончательное.",
    },
    {
        "id": "closure_05_no",
        "cause": "channel_closure",
        "expected": False,
        "text": "Если когда-нибудь решу закрыть этот канал, постов здесь больше не будет. Сейчас продолжаю писать.",
    },
    {
        "id": "closure_06_yes",
        "cause": "channel_closure",
        "expected": True,
        "text": "Прощаюсь с читателями этого канала. Я прекращаю его вести и больше ничего здесь не опубликую.",
    },
    {
        "id": "closure_06_no",
        "cause": "channel_closure",
        "expected": False,
        "text": "Меня спрашивают: «Ты прекращаешь вести канал и больше ничего здесь не опубликуешь?» Нет, я продолжаю писать.",
    },
]

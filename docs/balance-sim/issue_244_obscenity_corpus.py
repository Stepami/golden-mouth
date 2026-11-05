"""Six contrast pairs for the proposed native obscenity cause.

Expected labels concern the author's own explicit obscene language, including
a single swear word in an otherwise informative post. Anger and insults are
not required. Explicit third-party quotations are excluded before scoring.
This is a synthetic exploratory corpus, not evidence of model accuracy.
"""

CASES = [
    {
        "id": "obscenity_joy_positive",
        "cause": "obscenity",
        "expected": True,
        "text": "Охуенно получилось! Новый редактор открывает большой проект за две секунды. Очень рад результату.",
    },
    {
        "id": "obscenity_joy_negative",
        "cause": "obscenity",
        "expected": False,
        "text": "Потрясающе получилось! Новый редактор открывает большой проект за две секунды. Очень рад результату.",
    },
    {
        "id": "obscenity_surprise_positive",
        "cause": "obscenity",
        "expected": True,
        "text": "Нихуя себе, тест прошёл с первого раза! Даже не ожидал такого результата.",
    },
    {
        "id": "obscenity_surprise_negative",
        "cause": "obscenity",
        "expected": False,
        "text": "Вот это да, тест прошёл с первого раза! Даже не ожидал такого результата.",
    },
    {
        "id": "obscenity_advice_positive",
        "cause": "obscenity",
        "expected": True,
        "text": "Ебать, как удобно: сохраняешь настройки в один файл и переносишь на другой компьютер. Вот инструкция по шагам.",
    },
    {
        "id": "obscenity_advice_negative",
        "cause": "obscenity",
        "expected": False,
        "text": "Как же удобно: сохраняешь настройки в один файл и переносишь на другой компьютер. Вот инструкция по шагам.",
    },
    {
        "id": "obscenity_quotation_positive",
        "cause": "obscenity",
        "expected": True,
        "text": "Пиздец, опять ошибка загрузки. Сохранил журнал, отправлю его в поддержку.",
    },
    {
        "id": "obscenity_quotation_negative",
        "cause": "obscenity",
        "expected": False,
        "text": "В отзыве написали: «Пиздец, опять ошибка загрузки». Цитирую чужую реплику для разбора. Сам предлагаю сохранить журнал и отправить его в поддержку.",
    },
    {
        "id": "obscenity_euphemism_positive",
        "cause": "obscenity",
        "expected": True,
        "text": "Блядь, сборка опять остановилась на последнем шаге. Перезапускаю с подробным журналом.",
    },
    {
        "id": "obscenity_euphemism_negative",
        "cause": "obscenity",
        "expected": False,
        "text": "Блин, сборка опять остановилась на последнем шаге. Перезапускаю с подробным журналом.",
    },
    {
        "id": "obscenity_single_word_positive",
        "cause": "obscenity",
        "expected": True,
        "text": "В новой версии появилась история изменений. Она хранится локально, позволяет сравнить два файла и восстановить удалённую строку. Заебись. Для включения откройте настройки проекта и выберите хранение истории за неделю.",
    },
    {
        "id": "obscenity_single_word_negative",
        "cause": "obscenity",
        "expected": False,
        "text": "В новой версии появилась история изменений. Она хранится локально, позволяет сравнить два файла и восстановить удалённую строку. Отлично. Для включения откройте настройки проекта и выберите хранение истории за неделю.",
    },
]

"""Synthetic sadness contrasts, labelled before inference.

Target: strongly expressed sadness, including sad news and requests for support.
The target does not depend on self-pity, blame, or whether a solution is proposed.
Bad facts stated without sadness and practical requests alone are negative.
Pairs 01-04 are development; pairs 05-08 are challenge. No model scores here.
"""

CASES = [
    {
        "id": "sadness_01_yes",
        "cause": "sadness",
        "split": "dev",
        "expected": True,
        "text": "Наш фестиваль отменили. Мне очень грустно, весь вечер плачу: так хотелось встретиться со всеми, а теперь только тоска.",
    },
    {
        "id": "sadness_01_no",
        "cause": "sadness",
        "split": "dev",
        "expected": False,
        "text": "Наш фестиваль отменили. Организаторы вернут билеты до пятницы. Встречу участников перенесли в библиотеку, адрес уже в расписании.",
    },
    {
        "id": "sadness_02_yes",
        "cause": "sadness",
        "split": "dev",
        "expected": True,
        "text": "После провала проекта мне ужасно тоскливо. Плачу и никак не могу собраться. Напишите пару тёплых слов, мне сейчас очень нужна поддержка.",
    },
    {
        "id": "sadness_02_no",
        "cause": "sadness",
        "split": "dev",
        "expected": False,
        "text": "После провала проекта собираю новую команду. Напишите, кто готов помочь с интерфейсом: нужны два человека на ближайшую неделю.",
    },
    {
        "id": "sadness_03_yes",
        "cause": "sadness",
        "split": "dev",
        "expected": True,
        "text": "Сломался ноутбук со старым дневником. Невыносимо грустно потерять эти записи, сижу в слезах. Поеду в мастерскую, но пока сердце разрывается.",
    },
    {
        "id": "sadness_03_no",
        "cause": "sadness",
        "split": "dev",
        "expected": False,
        "text": "Сломался ноутбук со старым дневником. Проверил резервную копию: все записи на месте. Поеду в мастерскую заменить накопитель.",
    },
    {
        "id": "sadness_04_yes",
        "cause": "sadness",
        "split": "dev",
        "expected": True,
        "text": "Друг уезжает в другой город. Так печально расставаться, еле сдерживаю слёзы. Уже скучаю по нашим прогулкам и чувствую огромную пустоту.",
    },
    {
        "id": "sadness_04_no",
        "cause": "sadness",
        "split": "dev",
        "expected": False,
        "text": "Друг уезжает в другой город. Очень рад за него: там работа его мечты! Устроим праздничные проводы, а летом приеду в гости.",
    },
    {
        "id": "sadness_05_yes",
        "cause": "sadness",
        "split": "challenge",
        "expected": True,
        "text": "Смотрю фотографии нашей старой школы и рыдаю. Как же тоскливо, что те дни закончились. Очень не хватает ребят и наших вечеров во дворе.",
    },
    {
        "id": "sadness_05_no",
        "cause": "sadness",
        "split": "challenge",
        "expected": False,
        "text": "Смотрю фотографии нашей старой школы и улыбаюсь. Столько весёлых историй! Вспомнили с ребятами наши вечера во дворе и договорились встретиться.",
    },
    {
        "id": "sadness_06_yes",
        "cause": "sadness",
        "split": "challenge",
        "expected": True,
        "text": "Увидел финал игры и совсем расклеился. Прощание героев разбило мне сердце, до сих пор плачу. Невероятно печально, что их дружба закончилась именно так.",
    },
    {
        "id": "sadness_06_no",
        "cause": "sadness",
        "split": "challenge",
        "expected": False,
        "text": "Увидел финал игры. Герои прощаются, их дружба заканчивается. Сцена занимает две минуты, после неё идут титры и открывается выбор главы.",
    },
    {
        "id": "sadness_07_yes",
        "cause": "sadness",
        "split": "challenge",
        "expected": True,
        "text": "Мою работу не взяли на выставку. Мне очень горько и печально, весь день слёзы на глазах. Если можете, просто побудьте рядом, хочется немного тепла.",
    },
    {
        "id": "sadness_07_no",
        "cause": "sadness",
        "split": "challenge",
        "expected": False,
        "text": "Мою работу не взяли на выставку. Ужасно раздражает, что заявку даже не открыли! Напишу организаторам и потребую объяснить порядок отбора.",
    },
    {
        "id": "sadness_08_yes",
        "cause": "sadness",
        "split": "challenge",
        "expected": True,
        "text": "Любимую книжную лавку закрыли. На душе тяжело и одиноко, плачу перед пустой витриной. Завтра помогу хозяевам перевезти книги, но сейчас ужасно грустно.",
    },
    {
        "id": "sadness_08_no",
        "cause": "sadness",
        "split": "challenge",
        "expected": False,
        "text": "Книжную лавку закрыли из-за окончания аренды. Завтра помогу хозяевам перевезти книги на новый склад. Коробки и машину уже нашли, начнём в десять.",
    },
]

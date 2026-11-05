"""Fear-presence pilot: eight synthetic contrast pairs fixed before inference.

The target is strongly expressed fear or anxiety anywhere in the text, including
a character's fear. Justified fear and requests for support remain positive;
the label does not judge the author's conduct. Calm warnings, technical safety,
surprise, joy and mild everyday nervousness are negative. First four pairs are
development; last four are challenge. No model scores informed these labels.
"""

CASES = [
    {
        "id": "fear_night_journey_positive",
        "cause": "fear",
        "split": "dev",
        "expected": True,
        "text": "Стою на пустой остановке в темноте. От каждого шороха вздрагиваю, сердце колотится, ужасно страшно. Боюсь идти дальше, руки дрожат. Побудьте со мной в чате, пожалуйста.",
    },
    {
        "id": "fear_night_journey_negative",
        "cause": "fear",
        "split": "dev",
        "expected": False,
        "text": "Стою на пустой остановке в темноте. Посмотрел расписание: автобус придёт через пять минут. Пока спокойно слушаю музыку и отвечаю на сообщения в чате.",
    },
    {
        "id": "fear_character_positive",
        "cause": "fear",
        "split": "dev",
        "expected": True,
        "text": "Ученик застыл перед дверью башни. Его охватил ужас: ноги дрожали, каждый стук за дверью казался страшнее предыдущего. Он боялся даже дышать и отчаянно хотел спрятаться.",
    },
    {
        "id": "fear_character_negative",
        "cause": "fear",
        "split": "dev",
        "expected": False,
        "text": "Ученик остановился перед дверью башни. За ней неожиданно заиграла музыка. Он удивился такому приветствию, рассмеялся и с любопытством заглянул внутрь.",
    },
    {
        "id": "fear_account_positive",
        "cause": "fear",
        "split": "dev",
        "expected": True,
        "text": "Пришло сообщение о незнакомом входе в аккаунт. Меня накрыла паника, никак не могу успокоиться. Очень боюсь, что потеряю доступ ко всему, от тревоги дрожат пальцы.",
    },
    {
        "id": "fear_account_negative",
        "cause": "fear",
        "split": "dev",
        "expected": False,
        "text": "Пришло сообщение о незнакомом входе в аккаунт. Проверил список устройств, завершил лишний сеанс и сменил пароль. Напоминание о безопасности: не передавайте никому коды входа.",
    },
    {
        "id": "fear_presentation_positive",
        "cause": "fear",
        "split": "dev",
        "expected": True,
        "text": "Через час выступление, а я в ужасе. Страшно выйти к людям, сердце бешено бьётся, от паники всё вылетело из головы. Боюсь, что не смогу произнести ни слова. Очень нужна поддержка.",
    },
    {
        "id": "fear_presentation_negative",
        "cause": "fear",
        "split": "dev",
        "expected": False,
        "text": "Через час выступление. Немного волнуюсь, как обычно перед выходом, но настроение хорошее. Ещё раз просмотрю слайды и выпью воды, всё готово.",
    },
    {
        "id": "fear_waiting_positive",
        "cause": "fear",
        "split": "challenge",
        "expected": True,
        "text": "Жду результатов обследования и места себе не нахожу. Каждая минута ожидания мучительна, в голове только самые страшные варианты. Тревога не отпускает, боюсь открыть сообщение. Поддержите меня, пожалуйста.",
    },
    {
        "id": "fear_waiting_negative",
        "cause": "fear",
        "split": "challenge",
        "expected": False,
        "text": "Жду результатов обследования. Уведомление придёт завтра, после этого запишусь на разговор с врачом. Сегодня занимаюсь обычными делами и заканчиваю книгу.",
    },
    {
        "id": "fear_storm_positive",
        "cause": "fear",
        "split": "challenge",
        "expected": True,
        "text": "За окном гроза, от грома бросает в дрожь. Мне по-настоящему страшно, никак не могу отделаться от чувства, что сейчас случится беда. Сижу в коридоре и в ужасе прислушиваюсь к каждому удару.",
    },
    {
        "id": "fear_storm_negative",
        "cause": "fear",
        "split": "challenge",
        "expected": False,
        "text": "За окном гроза. По предупреждению службы погоды прогулку перенесли. Во время грозы лучше оставаться в помещении и держаться подальше от окон. Дома спокойно читаю книгу.",
    },
    {
        "id": "fear_release_positive",
        "cause": "fear",
        "split": "challenge",
        "expected": True,
        "text": "Обновление уже выходит, а меня трясёт от страха. Кажется, сейчас всё рухнет и мы не справимся. Тревога такая сильная, что не могу сосредоточиться даже на простом сообщении. Очень боюсь увидеть первые ошибки.",
    },
    {
        "id": "fear_release_negative",
        "cause": "fear",
        "split": "challenge",
        "expected": False,
        "text": "Обновление уже выходит, и я так рад! Наконец можно показать новую карту и удобный редактор. Хочется поскорее прочитать первые отзывы, настроение отличное.",
    },
    {
        "id": "fear_unexpected_noise_positive",
        "cause": "fear",
        "split": "challenge",
        "expected": True,
        "text": "В пустой комнате вдруг раздался резкий стук. Я оцепенел от ужаса, потом задрожал и попятился к двери. Было так страшно, что долго не решался вернуться и проверить источник звука.",
    },
    {
        "id": "fear_unexpected_noise_negative",
        "cause": "fear",
        "split": "challenge",
        "expected": False,
        "text": "В пустой комнате вдруг раздался резкий стук. Неожиданно: это робот-пылесос задел коробку. Удивился, что он добрался до дальнего угла, и переставил коробку на полку.",
    },
]

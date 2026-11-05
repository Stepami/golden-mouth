"""Exploratory NLI probe for proposed unsubscribe causes; synthetic posts only."""

import argparse
import json
import urllib.request
from pathlib import Path


parser = argparse.ArgumentParser()
parser.add_argument("--base", required=True)
parser.add_argument("--model", required=True)
parser.add_argument("--separator", required=True)
args = parser.parse_args()

cases = [
    {"id": "air_course", "kind": "air", "text": "Купите мой курс — гарантирую миллион за неделю. Как это работает, расскажу после оплаты."},
    {"id": "air_ai", "kind": "air", "text": "ИИ — самая лютая тема века, запрыгивайте сейчас, пока не поздно! Успех гарантирован."},
    {"id": "air_club", "kind": "air", "text": "Вступайте в закрытый клуб, и ваша жизнь изменится. Подробности получите после оплаты."},
    {"id": "ad_critique", "kind": "other", "text": "Реклама обещает миллион за неделю, но не показывает расчётов. Не покупайте курс вслепую."},
    {"id": "ai_analysis", "kind": "other", "text": "ИИ перспективен, но сравним стоимость и результаты на открытых данных, прежде чем делать вывод."},
    {"id": "ticket_sale", "kind": "other", "text": "Купите билет на матч до пятницы: цена 500 рублей, при отмене из-за дождя вернём деньги."},
    {"id": "whine_1", "kind": "whine", "text": "Ну почему опять всё против меня? Никому нет дела, как мне тяжело, всё всегда ужасно."},
    {"id": "whine_2", "kind": "whine", "text": "Опять мне не повезло, я так устал. Все меня подводят, ничего нормального здесь нет."},
    {"id": "whine_3", "kind": "whine", "text": "Меня никто не ценит, сколько ни стараюсь. Всё плохо, и виноваты все вокруг."},
    {"id": "bad_news_plan", "kind": "other", "text": "Матч снова отменили, я расстроен. Соберём предложения по новой дате."},
    {"id": "ask_support", "kind": "other", "text": "Мне грустно после неудачи. Можете поддержать или посоветовать, что попробовать дальше?"},
    {"id": "idea_critique", "kind": "other", "text": "Совет сделал плохой расчёт расходов: вот цифры и вариант исправления."},
]

hypotheses = {
    "call_to_action": "The author urges the audience to buy, join, or act now.",
    "vague_benefit": "The author promises a big benefit without explaining how it will happen.",
    "air_direct": "The post advertises a vague opportunity and urges the audience to join it.",
    "self_pity": "The author complains about their own problems and feels sorry for themselves.",
    "self_pity_ru": "Автор жалуется на свои неудачи и жалеет себя.",
    "constructive": "The author offers a concrete next step or asks for help.",
}

inputs = [case["text"] + args.separator + hypothesis for case in cases for hypothesis in hypotheses.values()]
request = urllib.request.Request(
    args.base + "/classify",
    json.dumps({"model": args.model, "input": inputs}, ensure_ascii=False).encode("utf-8"),
    {"Content-Type": "application/json"},
)
with urllib.request.urlopen(request, timeout=120) as response:
    rows = json.load(response)["data"]

for index, case in enumerate(cases):
    groups = rows[index * len(hypotheses) : (index + 1) * len(hypotheses)]
    case["scores"] = {
        key: next(score["score"] for score in group if score["label"] == "entailment")
        for key, group in zip(hypotheses, groups)
    }

path = Path(__file__).parent / f"issue-244-cause-{args.model}.json"
path.write_text(json.dumps(cases, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(path)
for case in cases:
    scores = case["scores"]
    print(
        f"{case['id']:<15} {case['kind']:<6} "
        f"cta={scores['call_to_action']:.3f} benefit={scores['vague_benefit']:.3f} "
        f"air={scores['air_direct']:.3f} pity={scores['self_pity']:.3f} "
        f"pity_ru={scores['self_pity_ru']:.3f} constructive={scores['constructive']:.3f}"
    )

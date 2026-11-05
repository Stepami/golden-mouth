"""Compare synthetic posts with separate anchors from the actual presets."""

import json
import re
import urllib.request
from pathlib import Path


folder = Path(__file__).parent
source = (folder.parents[1] / "backend/src/Reki.Infrastructure/Mongo/Migrations/2_5_1_RefreshWorldContent.Data.cs").read_text(encoding="utf-8")
worlds = ("evening", "sorcerers", "houses", "it")
anchors = {}
for world, chunk in zip(worlds, source.split("new WorldInterface")[1:]):
    header, tail = chunk.split("},", 1)
    fields = dict(re.findall(r'^\s*(Description|Summary|FirstContext) = "(.*)",$', header, re.MULTILINE))
    arrays = re.findall(r'\[(.*?)\]', tail, re.DOTALL)[:4]
    literals = [re.findall(r'"((?:\\.|[^"\\])*)"', array) for array in arrays]
    anchors[world] = {
        "summary": [fields["Summary"]],
        "description": [fields["Description"]],
        "first_context": [fields["FirstContext"]],
        "post_ideas": literals[2],
        "demo_posts": literals[3],
        "combined": ["\n".join(fields.values())],
    }

posts = json.loads((folder / "issue-244-corpus.json").read_text(encoding="utf-8"))["posts"]
posts += [
    {"id": "e_on2", "world": "evening", "case": "holdout_on", "text": "Яков рассказал у моря старую историю о холодных людях и договоре с их семьёй."},
    {"id": "e_off2", "world": "evening", "case": "holdout_off", "text": "Сегодня научился печь хлеб и выбираю форму для выпечки."},
    {"id": "s_on2", "world": "sorcerers", "case": "holdout_on", "text": "После тролля друзья спорят, кто должен объяснить преподавателю случившееся."},
    {"id": "s_off2", "world": "sorcerers", "case": "holdout_off", "text": "Поменял колёса на велосипеде и проверил тормоза."},
    {"id": "h_on2", "world": "houses", "case": "holdout_on", "text": "За северной Стеной движутся мёртвые. Домам пора обсудить общий ответ."},
    {"id": "h_off2", "world": "houses", "case": "holdout_off", "text": "Купил комнатный кактус и пересадил его в новый горшок."},
    {"id": "i_on2", "world": "it", "case": "holdout_on", "text": "Результат Смены стоит повторить на открытых данных и сравнить стоимость запуска."},
    {"id": "i_off2", "world": "it", "case": "holdout_off", "text": "Выбираю новую удочку для летней рыбалки."},
    {"id": "i_food", "world": "it", "case": "holdout_ambiguous", "text": "Сегодня придумал рецепт двойной котлеты для бургера."},
]
references = [(world, group, text) for world, groups in anchors.items() for group, texts in groups.items() for text in texts]
inputs = ["categorize: " + text for _, _, text in references]
inputs += ["categorize: " + post["text"] for post in posts]
request = urllib.request.Request(
    "http://127.0.0.1:7998/embeddings",
    json.dumps({"model": "frida", "input": inputs}, ensure_ascii=False).encode("utf-8"),
    {"Content-Type": "application/json"},
)
with urllib.request.urlopen(request, timeout=120) as response:
    vectors = [item["embedding"] for item in json.load(response)["data"]]

reference_vectors = {}
for (world, group, _), vector in zip(references, vectors):
    reference_vectors.setdefault(world, {}).setdefault(group, []).append(vector)


def similarity(first, second):
    return sum(a * b for a, b in zip(first, second))


for post, vector in zip(posts, vectors[len(references) :]):
    groups = reference_vectors[post["world"]]
    scores = {name: max(similarity(vector, anchor) for anchor in values) for name, values in groups.items()}
    core = max(scores[name] for name in ("summary", "description", "first_context"))
    all_anchors = max(core, scores["post_ideas"], scores["demo_posts"])
    print(
        f"{post['id']:<3} {post['case']:<17} combined={scores['combined']:.3f} "
        f"core={core:.3f} summary={scores['summary']:.3f} "
        f"description={scores['description']:.3f} first={scores['first_context']:.3f} "
        f"ideas={scores['post_ideas']:.3f} "
        f"demo={scores['demo_posts']:.3f} all={all_anchors:.3f}"
    )

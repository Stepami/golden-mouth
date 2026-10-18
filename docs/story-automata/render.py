#!/usr/bin/env python3
"""Validate the draft dictionaries and render their review tables (standard library only)."""
import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def table(headers, rows):
    def cell(value):
        return str(value).replace('|', '\\|').replace('\n', '<br>')
    return '\n'.join(['| ' + ' | '.join(map(cell, headers)) + ' |',
                      '| ' + ' | '.join('---' for _ in headers) + ' |'] +
                     ['| ' + ' | '.join(map(cell, row)) + ' |' for row in rows])


def validate(data):
    worlds = {w['key']: w for w in data['worlds']}
    assert set(worlds) == {'evening', 'sorcerers', 'houses', 'it'}
    assert len(data['stories']) == 16
    assert len({s['story_id'] for s in data['stories']}) == 16
    for key, world in worlds.items():
        actor_ids = {a[0] for a in world['actors']}
        assert len(actor_ids) == 3
        assert {s['player_role'] for s in data['stories'] if s['world_key'] == key} == actor_ids | {'own'}
    for s in data['stories']:
        roster = {a[0] for a in worlds[s['world_key']]['actors']}
        assert set(s['actors']) == roster - {s['player_role']}
        states = {v['id']: v for v in s['states']}
        events = {e['code']: e for e in s['events']}
        assert len(states) == len(s['states']) and len(events) == len(s['events'])
        assert s['start_state'] in states and 'OTHER' in events
        assert set(s['final_states']) == {k for k,v in states.items() if v['terminal']}
        transitions = {}
        bindings = {}
        assert len({t['id'] for t in s['transitions']}) == len(s['transitions'])
        for t in s['transitions']:
            pair = (t['source'], t['event'])
            assert pair not in transitions, (s['story_id'], pair)
            assert t['source'] in states and t['destination'] in states and t['event'] in events
            assert not states[t['source']]['terminal'] and t['event'] != 'OTHER'
            transitions[pair] = t
            event = events[t['event']]
            assert event['actor'] == 'player' or event['actor'] in s['actors']
            if event['target'] != 'channel':
                assert event['target'] in bindings, (s['story_id'], event['target'])
                target_kind = next(e['kind'] for e in s['events'] if e['bind'] == event['target'])
                assert target_kind == ('player_post' if event['kind'] == 'npc_comment' else 'npc_comment')
            assert event['bind'] and event['bind'] not in bindings
            bindings[event['bind']] = t['destination']
        reached = {s['start_state']}
        while True:
            expanded = reached | {t['destination'] for t in s['transitions'] if t['source'] in reached}
            if expanded == reached:
                break
            reached = expanded
        assert reached == set(states), s['story_id']
        # A binding must exist on every incoming path, not merely somewhere earlier in the file.
        dominators = {q: ({q} if q == s['start_state'] else set(states)) for q in states}
        while True:
            updated = dict(dominators)
            for q in states:
                if q == s['start_state']:
                    continue
                predecessors = [t['source'] for t in s['transitions'] if t['destination'] == q]
                updated[q] = {q} | set.intersection(*(dominators[p] for p in predecessors))
            if updated == dominators:
                break
            dominators = updated
        for t in s['transitions']:
            target = events[t['event']]['target']
            if target != 'channel':
                assert bindings[target] in dominators[t['source']], (s['story_id'], target)

        # Total matrix: every absent pair is a side-effect-free self loop.
        matrix = {(q,e): transitions[(q,e)]['destination'] if (q,e) in transitions else q
                  for q in states for e in events}
        assert len(matrix) == len(states)*len(events)
        for final in s['final_states']:
            assert all(matrix[(final,e)] == final for e in events)
        for case in s['checks']:
            q = s['start_state']
            for event in case['events']:
                assert event in events
                q = matrix[(q,event)]
            assert q == case['final'], (s['story_id'], case['name'], q)
        for t in s['transitions']:
            # A repeat of an accepted category in its destination cannot advance it again.
            assert matrix[(t['destination'],t['event'])] == t['destination']
        for e in s['events']:
            assert e['criterion'].strip()
        for state in s['states']:
            assert all(state[k].strip() for k in ('name','fact','next','hint'))
    return worlds


def render_story(s, world):
    names = {a[0]:a[2] for a in world['actors']}
    name = lambda actor: 'Игрок' if actor == 'player' else names.get(actor, actor)
    role = 'Свой персонаж' if s['player_role'] == 'own' else name(s['player_role'])
    transitions = {(t['source'],t['event']):t for t in s['transitions']}
    kinds = {'player_post':'Сохранённый пост игрока','npc_comment':'Сохранённый комментарий героя под связанным постом',
             'player_reply':'Сохранённый ответ игрока на связанный комментарий',
             'npc_reply':'Сохранённый ответ героя на связанный комментарий','other':'Другое событие'}
    lines = [f'<a id="{s["story_id"]}"></a>', '', f'## {s["story_id"]}: {s["title"]}', '', f'{world["name"]} · {role}. Черновик для обсуждения в [#192](https://github.com/Stepami/golden-mouth/issues/192).',
             '', 'Биографии и общие условия обработки событий приведены выше в этом документе.',
             '', '### Паспорт', '', table(['Поле','Значение'], [
                 ['story_id',s['story_id']],['version',s['version']],['world_key',s['world_key']],
                 ['player_role',f'{s["player_role"]} — {role}'],['actors',', '.join(f'{a} — {name(a)}' for a in s['actors'])],
                 ['Завязка',s['premise']],['Цель',s['goal']],['Начало',s['start_state']],['Исходы',', '.join(s['final_states'])]]),
             '', '### Состояния', '', 'Факты предыдущих этапов сохраняются; в строке указан новый факт. Подсказки — предлагаемые тексты, интерфейс их показа здесь не задаётся.', '',
             table(['ID','Название','Новый факт истории','Дальше','Подсказка игроку','Конечное'],[
                 [q['id'],q['name'],q['fact'],q['next'],q['hint'],'Да' if q['terminal'] else 'Нет'] for q in s['states']]),
             '', '### События', '', 'Каждое условие включает общие проверки мира, автора, связи с сохранённым объектом и смысла из README. Для всех строк, кроме OTHER, проверка смысла и сюжетная привязка ещё требуют серверной реализации.', '',
             table(['Код','Сохранённый факт','Автор','Связь','Критерий по смыслу','Сохранить'],[
                 [e['code'],kinds[e['kind']],name(e['actor']),e['target'],e['criterion'],e['bind'] or '—'] for e in s['events']]),
             '', '### Переходы', '', table(['ID','Из','Событие','В','Память','Результат для игрока'],[
                 [t['id'],t['source'],t['event'],t['destination'],t['remember'],t['effect']] for t in s['transitions']]),
             '', '### Полная матрица', '', '`—` — сохранить состояние без действий. Коды вне разрешённого перехода не засчитываются задним числом.', '',
             table(['Состояние']+[e['code'] for e in s['events']], [
                 [q['id']]+[(f'{transitions[(q["id"],e["code"])]["destination"]} / {transitions[(q["id"],e["code"])]["id"]}' if (q['id'],e['code']) in transitions else f'{q["id"]} / —') for e in s['events']] for q in s['states']]),
             '', '### Граф', '', 'Петли без действий из полной матрицы опущены для читаемости.', '', '```mermaid', 'stateDiagram-v2', f'    [*] --> {s["start_state"]}']
    lines += [f'    {q["id"]}: {q["name"]}' for q in s['states']]
    lines += [f'    {t["source"]} --> {t["destination"]}: {t["event"]}' for t in s['transitions']]
    lines += [f'    {f} --> [*]' for f in s['final_states']]
    lines += ['```','','### Прогоны для проверки','',
              table(['Сценарий','Начало','События / факты','Ожидаемое состояние'],[
                  [c['name'],'S0',' → '.join(c['events']),c['final']] for c in s['checks']]+[
                  ['Не тот герой','S1',f'Текст E02 от другого автора → OTHER','S1'],
                  ['Не тот пост','S1','Текст E02 под другим постом → OTHER','S1'],
                  ['Не тот родитель','Перед player_reply','Нужный текст с другим parentCommentId → OTHER','То же состояние'],
                  ['Фраза без действия','Перед END_1/END_2','Пересказ чужого решения без своего предложения → OTHER','То же состояние'],
                  ['Оба несовместимых итога','Перед END_1/END_2','Подтверждены оба решения либо ни одно → OTHER','То же состояние; нужна ясная формулировка'],
                  ['Нет подтверждения смысла','Любое','Классификация не завершена / ошибка','Событие ожидает проверки, продвижения нет'],
                  ['Повтор доставки','После любого T','Тот же идентификатор факта','Без повторного эффекта'],
                  ['После завершения','F1 / F2','Любые новые события','То же F1 / F2; один итог']]),
              '', 'Критерии по смыслу нужно проверить отдельно на примерах и контрпримерах. Автоматическая проверка справочника проверяет ссылки и переходы, а не качество классификации или реплик.', '']
    return '\n'.join(lines)


def render_presets(data):
    lines=['## Предлагаемые пресеты','', 'Черновик к #192 от 18.10.2026. Это самостоятельные истории с жанровыми ориентирами; изменения работающего каталога не применены. Сходство атмосферы не подтверждает правовую допустимость конкретного исполнения. До публикации проверить также названия, внешность, обложки и рекламные материалы.','',
           'Сохраняем технические ключи для сопоставления. Это не решение переименовать героев в существующих мирах: новые биографии и сюжеты относятся только к новой согласованной версии пресета.','']
    for w in data['worlds']:
        lines += [f'### {w["name"]} (`{w["key"]}`)','',table(['Поле','Предложение'],[
            ['Жанровый ориентир',w['reference']],['Что сохраняем от жанра',w['genre']],['Описание мира',w['description']],
            ['Первая ситуация',w['context']],['Первый вопрос','Что ты расскажешь об этом в первом посте?'],
            ['Свой персонаж','Своя биография и голос. Участие добровольное; герой не получает заранее назначенную судьбу, родство или симпатию.']]),'',
            '#### Границы мира','']+['- '+r for r in w['rules']]+['', '#### Три готовых героя','',
            table(['Ключ','Сейчас в каталоге','Предлагаемое имя','Роль','Биография и конфликт','Голос'],w['actors']), '']
    lines += ['### Общие ограничения','',
              '- Не использовать узнаваемые эмблемы, костюмы, внешность актёров, цитаты и сцены оригиналов. Обложки и аватары требуют отдельной сверки перед выпуском.',
              '- Возрастная рамка и правила безопасности действующей игры сохраняются. Никаких обязательных романтических отношений или сцен жестокости.',
              '- Отдельный конфликт каждого героя важнее комплекта «главный герой — умная подруга — смешной друг». Выбранный герой становится игроком и исключается из NPC.',
              '- Новые способности и должности объясняют сюжет, но не дают бонусов энергии, подписчиков, отношений или иных наград.',
              '- Старые никнеймы IT-героев не показывать как ники новых героев. Поля публичных никнеймов, обложек и аватаров до выпуска нужно заполнить и согласовать отдельно; стабильный ключ здесь нужен только для сопоставления.','']
    return '\n'.join(lines)


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true', help='validate data and compare rendered documents')
    args=parser.parse_args()
    data=json.loads((ROOT/'catalog.json').read_text())
    worlds=validate(data)
    intro=(ROOT/'introduction.txt').read_text()
    content=[intro, render_presets(data)]
    for story in data['stories']:
        content.append(render_story(story,worlds[story['world_key']]))
    files={'stories.md':'\n\n'.join(content)}
    for name,body in files.items():
        if args.check:
            assert (ROOT/name).read_text()==body, f'Regenerate {name}'
        else:
            (ROOT/name).write_text(body)
    print(f'OK: {len(worlds)} worlds, {len(data["stories"])} stories, complete matrices, references and both ending paths; rendered documents match')


if __name__=='__main__':
    main()

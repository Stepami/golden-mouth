# Серия дней: заходы и отток за 30 дней. Коэффициенты допущены (не замер), сравниваем варианты между собой.
# Запуск: python3 game_sim4.py
import random, statistics as st
TYPES = {'казуал': .45, 'средний': .68, 'вовлечённый': .85}   # базовая вероятность зайти в день
V = {
  'сейчас': dict(rewards={3: .10, 7: .12}, calendar=False, rested_after=1, rested_pull=.12),
  'лестница': dict(rewards={3: .10, 7: .12, 14: .10, 30: .10}, calendar=False, rested_after=1, rested_pull=.12),
  'лестница+календарь': dict(rewards={3: .10, 7: .12, 14: .10, 30: .10}, calendar=True, rested_after=1, rested_pull=.12),
  'всё + отдых с 2 дней': dict(rewards={3: .10, 7: .12, 14: .10, 30: .10}, calendar=True, rested_after=2, rested_pull=.12),
  'итог: сводка с 1 дня, ×2 с 2 дней': dict(rewards={3: .10, 7: .12, 14: .10, 30: .10}, calendar=True, rested_after=2, rested_pull=.12, summary_pull=.08),
}
def run(base, v, r, days=30):
    streak, skip, played, best, churn_day = 0, 0, 0, 0, None
    for d in range(1, days + 1):
        p = base
        if v['calendar'] and streak >= 2: p += .04 + .01 * min(streak, 7)       # не хочется рвать видимую цепочку
        nxt = streak + 1
        if any(nxt < k <= nxt + 1 for k in v['rewards']): p += v['rewards'][nxt + 1] * .5   # награда завтра: анонс
        if nxt in v['rewards']: p += v['rewards'][nxt]                         # награда сегодня
        # канал отдохнул: после пропуска тянет вернуться, но если бонус с 1 дня, пропуск одного дня выгоден
        if skip >= v['rested_after']: p += v['rested_pull']
        elif skip >= 1 and v.get('summary_pull'): p += v['summary_pull']   # «пока тебя не было» без ×2
        if skip == 0 and streak >= 1 and v['rested_after'] == 1: p -= .03       # «завтра отдохнувший канал даст ×2»
        if skip >= 4: p *= .35                                                  # долгий пропуск: почти ушёл
        if r.random() < min(.97, p):
            played += 1; streak = streak + 1 if skip == 0 else 1; skip = 0; best = max(best, streak)
        else:
            skip += 1                                                            # серия на паузе, не сгорает: при возврате продолжается
            if skip >= 7 and churn_day is None: churn_day = d
    return played, best, churn_day
for name, base in TYPES.items():
    print(f'== {name}')
    for vn, v in V.items():
        R = [run(base, v, random.Random(s)) for s in range(3000)]
        print(f'  {vn:22} дней из 30: {st.mean(x[0] for x in R):4.1f}  серия 7+: {sum(x[1] >= 7 for x in R)/len(R):4.0%}  '
              f'серия 14+: {sum(x[1] >= 14 for x in R)/len(R):4.0%}  ушли (7 дней без захода): {sum(x[2] is not None for x in R)/len(R):4.0%}')

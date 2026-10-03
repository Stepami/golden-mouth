# Аудит «как в Sims»: миры с настоящей сложностью, стремление, ритм уровней, провалы, переезд в новый мир.
# Запуск: python3 game_sim3.py before|after
import random, statistics as st, sys
MODE = sys.argv[1] if len(sys.argv) > 1 else 'before'
W = {  # мир: множитель роста, база провалов, с какого числа подписчиков хейт, частота хейта
  'Сцена': dict(k=1.0, flop=.15, hate_from=5000, hate=(3, 4), viral=1.0),
  'Стрим': dict(k=1.0, flop=.15, hate_from=5000, hate=(3, 4), viral=1.0),
  'Айти':  dict(k=1.0, flop=.15, hate_from=5000, hate=(3, 4), viral=1.0)}
P = dict(side_xp=.35, lvl=[0, 8, 25, 60], carry=.5, flop_mult=1.0, pity_sessions=None, near_miss=False, wants=False)
if MODE == 'after':
    W = {'Сцена': dict(k=1.0, flop=.10, hate_from=5000, hate=(4, 5), viral=1.0),
         'Стрим': dict(k=1.1, flop=.15, hate_from=3000, hate=(3, 4), viral=1.2),
         'Айти':  dict(k=1.25, flop=.20, hate_from=1000, hate=(2, 3), viral=1.4)}
    P.update(side_xp=.15, carry=1.0, pity_sessions=3, wants=True)
    if len(sys.argv) > 2: P['lvl'] = [0, 8, 20, 36, 56, 80, 110, 150, 200, 270]   # 10 уровней, приём на 3, 6, 9
def lv(x, t=None): t = t or P['lvl']; return max(i + 1 for i, v in enumerate(t) if x >= v)
def run(world, skill=.55, ownp=.5, ppd=10, show=1.0, days=14, seed=0):
    w = W[world]; r = random.Random(seed)
    subs, xp = 120., {'main': 0, 's1': 0, 's2': 0}
    since_v, flops, total, sessions_no_v, next_hate = 0, 0, 0, 0, None
    o = dict(lvl_days=[], flops=0, posts=0, virals=0, hate=0, drop=False, best_streak=0, wants=0)
    for d in range(1, days + 1):
        if d > 1 and r.random() > show: continue
        v_today = False; s0 = subs
        for i in range(1, ppd + 1):
            total += 1; o['posts'] += 1
            L = sum(lv(x) for x in xp.values()) / 3
            Lmax = len(P['lvl'])
            q = min(1, skill * .8 + (L - 1) / (Lmax - 1) * .21 + r.uniform(-.15, .15))   # вклад навыка как доля шкалы
            # желания дня: 3 маленькие цели; выполненное даёт +качество (знаешь, что писать) и опыт
            want = P['wants'] and i <= 3 and r.random() < .7
            if want: q = min(1, q + .08); o['wants'] += 1
            own = r.random() < ownp
            mult = 1 if i <= 5 else (.7 if i <= 10 else .25)
            rate = w['k'] * (.5 + q) * subs ** .6 / subs
            since_v += 1
            v = total == 3 or since_v >= 15 or r.random() < (.03 + .06 * q) * w['viral']
            if P['pity_sessions'] and sessions_no_v >= P['pity_sessions'] and i == 1: v = True
            f = not v and flops < 2 and r.random() < w['flop'] * (1.4 - q) * P['flop_mult']
            g = rate * 3 if v else (-rate * .3 if f else rate)
            if v: since_v = 0; o['virals'] += 1; v_today = True
            if g > 0: g *= (1.3 if own else 1) * (2 if subs < 1000 else 1)
            subs = max(120, subs * (1 + g * mult)); flops = flops + 1 if f else 0; o['flops'] += f
            if i <= 10:
                for k2 in xp:
                    b4 = lv(xp[k2]); xp[k2] += (1 if k2 == 'main' else P['side_xp']) * (1.3 if own else 1) * (2 if i <= 3 else 1) * (1.3 if want else 1)
                    if lv(xp[k2]) > b4: o['lvl_days'].append(d)
        sessions_no_v = 0 if v_today else sessions_no_v + 1
        if subs >= w['hate_from']:
            if next_hate is None: next_hate = d
            if d >= next_hate: o['hate'] += 1; subs *= 1.07; next_hate = d + r.randint(*w['hate'])
        subs *= 1.05
        if s0 < 10000 <= subs and P['carry'] < 1: subs *= P['carry']; o['drop'] = True
    o['subs'] = subs
    gaps = sorted(set(o['lvl_days'])); o['lvl_gap'] = max([b - a for a, b in zip([0] + gaps, gaps + [days])])
    o['same_day_lvls'] = max([o['lvl_days'].count(x) for x in set(o['lvl_days'])] or [0])
    return o
print('режим', MODE)
for world in W:
    R = [run(world, seed=s) for s in range(400)]
    m = lambda f: st.median(f(x) for x in R)
    print(f'{world:6} средний: д14 {m(lambda x:x["subs"])/1000:>6.1f}k  провалов/день {m(lambda x:x["flops"])/14:.1f}  залётов {m(lambda x:x["virals"]):>3}  хейт {m(lambda x:x["hate"]):>2}  '
          f'макс. дней без уровня {m(lambda x:x["lvl_gap"]):>3}  уровней в один день {m(lambda x:x["same_day_lvls"]):>2}  падение при переезде {sum(x["drop"] for x in R)/len(R):.0%}')
for name, kw in [('казуал', dict(ppd=3, show=.6, skill=.4, ownp=.3)), ('слабый автор', dict(skill=.3, ownp=.1))]:
    R = [run('Сцена', seed=s, **kw) for s in range(400)]
    m = lambda f: st.median(f(x) for x in R)
    print(f'{name:12} Сцена: д14 {m(lambda x:x["subs"])/1000:>5.1f}k  залётов {m(lambda x:x["virals"]):>2}  провалов/день {m(lambda x:x["flops"])/14:.1f}  макс. дней без уровня {m(lambda x:x["lvl_gap"])}')
# заруба между мирами: равные игроки в разных мирах, счёт = средняя оценка постов (качество), шкалы нормированы по миру
import itertools
for a, b in itertools.combinations(W, 2):
    wins = 0
    for s in range(1000):
        r1, r2 = random.Random(s), random.Random(s + 9999)
        qa = st.mean(min(1, .44 + r1.uniform(-.15, .15)) for _ in range(15)); qb = st.mean(min(1, .44 + r2.uniform(-.15, .15)) for _ in range(15))
        wins += qa > qb
    print(f'заруба {a} vs {b}: {wins/1000:.0%} (качество нормировано по миру)')

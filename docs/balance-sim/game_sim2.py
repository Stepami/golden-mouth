# Симуляция v3: прогрессия контента, отношения, коллекция, лимит, возврат, заруба.
# База: правила v2 (game_sim.py). Запуск: python3 game_sim2.py base|c1|c2
import random, statistics as st, sys, math
MODE = sys.argv[1] if len(sys.argv) > 1 else 'base'
P = dict(posts=10, plus=30, decay=[(5, 1.0), (10, .7), (99, .25)], pow=.6, k=1.0, starter=2.0, rested=2.0, own=1.3,
         viral_base=.03, viral_q=.06, pity=15, viral_mult=3, flop_base=.15, flop_cap=2,
         lvl=[0, 5, 14, 30], rel_per_post=.25, rel_dm=.5, stars_new_day=3,
         cards_world=12, hate_from=5000, hate_every=(2, 3), world_at=10000, carry=.3,
         # возврат: базовая вероятность зайти завтра и что на неё влияет
         ret_base=.55, ret_viral=.06, ret_new=.08, ret_hook=.10, ret_flop=-.05, ret_hate=-.04, ret_quiet=-.08, ret_limit=.04)
if MODE in ('c1', 'c2'):   # цикл 1
    P.update(lvl=[0, 8, 25, 60], xp_cap=10, rel_per_post=.1, rel_dm=.25, carry=.5, novelty_floor=True, hook=True)
if MODE == 'c2':           # цикл 2
    P.update(hate_every=(3, 4), plus=20, rel_day_cap=1.0, comeback=True, first3_xp=2.0, first3_rel=2.0)

A = {'казуал': dict(ppd=3, skill=.4, ownp=.3, plus=False, show=.6),
     'средний': dict(ppd=10, skill=.55, ownp=.5, plus=False, show=.85),
     'вовлечённый': dict(ppd=10, skill=.7, ownp=.8, plus=False, show=1),
     'Реки+': dict(ppd=99, skill=.6, ownp=.6, plus=True, show=1),
     'слабый автор': dict(ppd=10, skill=.3, ownp=.2, plus=False, show=.85)}

def lv(x): return max(i + 1 for i, t in enumerate(P['lvl']) if x >= t)

def run(a, days=14, seed=0, retention=False):
    r = random.Random(seed)
    subs, xp, rel = 120., {'b': 0, 'h': 0, 't': 0}, [0., 0., 0.]
    since_v, flops, total, next_hate, last, worlds = 0, 0, 0, None, 1, 1
    cards, out = 0, dict(lvl={}, rel3=None, cards={}, limit_day=None, world2=None, quiet=0, alive=days, hate=0, flopday=0)
    p_ret = 1.0
    for d in range(1, days + 1):
        if retention:
            if d > 1 and r.random() > p_ret: out['alive'] = d - 1; break
        elif d > 1 and r.random() > a['show']: continue
        day_rel = rel[:]
        new = 0; hate_today = False; viral_today = False; flop_today = 0
        rested = d > 1 and last < d - 1; last = d
        cap = P['plus'] if a['plus'] else P['posts']
        n = min(a['ppd'], cap)
        if n >= cap and out['limit_day'] is None: out['limit_day'] = d
        for i in range(1, n + 1):
            total += 1
            L = sum(lv(x) for x in xp.values()) / 3
            q = min(1, a['skill'] * .8 + (L - 1) * .07 + r.uniform(-.15, .15))
            own = r.random() < a['ownp']
            mult = next(m for lim, m in P['decay'] if i <= lim)
            rate = P['k'] * (.5 + q) * subs ** P['pow'] / subs
            since_v += 1
            v = total == 3 or since_v >= P['pity'] or r.random() < P['viral_base'] + P['viral_q'] * q
            f = not v and flops < P['flop_cap'] and r.random() < P['flop_base'] * (1.4 - q)
            g = rate * P['viral_mult'] if v else (-rate * .3 if f else rate)
            if v: since_v = 0; viral_today = True
            if g > 0:
                g *= P['own'] if own else 1
                if rested and i <= 3: g *= P['rested']
                if subs < 1000: g *= P['starter']
            subs = max(120, subs * (1 + g * mult)); flops = flops + 1 if f else 0; flop_today += f
            for k2 in xp:
                if P.get('xp_cap') and i > P['xp_cap']: break   # опыт только с первых 10 постов дня
                b4 = lv(xp[k2]); xp[k2] += (1 if k2 == 'h' else .35) * (1.3 if own else 1) * (P.get('first3_xp', 1) if i <= 3 else 1)
                if lv(xp[k2]) > b4: new += 1; out['lvl'].setdefault(f'{k2}{lv(xp[k2])}', d)
            st_i = 0 if total % 3 else 1
            if d >= P['stars_new_day'] or st_i == 0:
                rel[st_i] = min(5, rel[st_i] + P['rel_per_post'] * (1 + q) * (P.get('first3_rel', 1) if i <= 3 else 1))
        rel[0] = min(5, rel[0] + P['rel_dm'])
        if P.get('rel_day_cap'):
            for j in range(3): rel[j] = min(rel[j], day_rel[j] + P['rel_day_cap'])
        if P.get('comeback') and rested: new += 1   # «пока тебя не было»: сводка и бесплатный пост-событие
        if out['rel3'] is None and max(rel) >= 3: out['rel3'] = d
        if subs >= P['hate_from']:
            if next_hate is None: next_hate = d
            if d >= next_hate:
                hate_today = True; out['hate'] += 1; subs *= 1 + r.uniform(.04, .1); xp['b'] += 3
                next_hate = d + r.randint(*P['hate_every']); new += 1
        subs *= 1 + r.uniform(.03, .08)
        if worlds == 1 and subs >= P['world_at']:
            worlds = 2; out['world2'] = d; new += 1
            subs = max(120, subs * P['carry'])   # переезд с частью подписчиков
        # карточки: моменты и отношения
        got = 0
        if viral_today and cards < P['cards_world']: got += 1
        if hate_today: got += 1
        if max(rel) >= 3 and 'rel' not in out['cards']: out['cards']['rel'] = d; got += 1
        if P.get('novelty_floor') and new + got == 0: got += 1        # открытие дня
        cards = min(P['cards_world'], cards + got)
        out['cards'][d] = cards
        new += got
        if new == 0: out['quiet'] += 1
        if flop_today >= 2: out['flopday'] += 1
        if retention:
            p = P['ret_base'] + P['ret_viral'] * viral_today + P['ret_new'] * min(new, 2) + P['ret_hook'] * bool(P.get('hook')) \
                + P['ret_flop'] * (flop_today >= 2) + P['ret_hate'] * hate_today + P['ret_quiet'] * (new == 0) + P['ret_limit'] * (n >= cap)
            p_ret = max(.05, min(.95, p))
    out['subs'] = subs; out['xp'] = {k2: lv(x) for k2, x in xp.items()}
    return out

def report():
    print('режим', MODE)
    for name, a in A.items():
        R = [run(a, seed=s) for s in range(400)]
        med = lambda f: st.median(f(o) for o in R)
        lday = lambda key: st.median(o['lvl'].get(key, 99) for o in R)
        print(f"{name:13} юмор2:{lday('h2'):>4} юмор3:{lday('h3'):>4} юмор4:{lday('h4'):>4} смел2:{lday('b2'):>4} | "
              f"отнош+3:{med(lambda o:o['rel3'] or 99):>4} 2-й мир:{med(lambda o:o['world2'] or 99):>4} лимит:{med(lambda o:o['limit_day'] or 99):>4} | "
              f"карт д7:{med(lambda o:o['cards'].get(7, max([v for k,v in o['cards'].items() if isinstance(k,int) and k<=7] or [0]))):>4} д14:{med(lambda o:max([v for k,v in o['cards'].items() if isinstance(k,int)] or [0])):>4} "
              f"тихих:{med(lambda o:o['quiet']):>4} хейт:{med(lambda o:o['hate']):>3}")
    print('возврат (доля живых): ', end='')
    for name, a in A.items():
        R = [run(a, seed=s, retention=True) for s in range(1000)]
        d1 = sum(o['alive'] >= 2 for o in R) / len(R); d7 = sum(o['alive'] >= 7 for o in R) / len(R)
        print(f"{name} D1 {d1:.0%} D7 {d7:.0%} | ", end='')
    print()
report()

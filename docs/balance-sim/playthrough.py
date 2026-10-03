# Прохождение по дням: что видит игрок. Правила как в game_sim2.py c2. Запуск: python3 playthrough.py
import sys; sys.argv = ['x', 'c2']
exec(open('game_sim2.py').read().split('def report')[0])
import random
def diary(name, a, days=10, seed=7, skip=()):
    r = random.Random(seed)
    subs, xp, rel = 120., {'b': 0, 'h': 0, 't': 0}, [0., 0., 0.]
    since_v, flops, total, next_hate, last, worlds, cards = 0, 0, 0, None, 1, 1, 0
    print(f'\n=== {name}')
    for d in range(1, days + 1):
        if d in skip: print(f' д{d:>2}: не зашёл'); continue
        ev = []; rested = d > 1 and last < d - 1; last = d
        if rested: ev.append('«пока тебя не было» + отдых ×2')
        cap = P['plus'] if a['plus'] else P['posts']; n = min(a['ppd'], cap); s0 = subs; day_rel = rel[:]; v_n = f_n = 0
        for i in range(1, n + 1):
            total += 1; L = sum(lv(x) for x in xp.values()) / 3
            q = min(1, a['skill'] * .8 + (L - 1) * .07 + r.uniform(-.15, .15)); own = r.random() < a['ownp']
            mult = next(m for lim, m in P['decay'] if i <= lim); rate = P['k'] * (.5 + q) * subs ** P['pow'] / subs
            since_v += 1; v = total == 3 or since_v >= P['pity'] or r.random() < P['viral_base'] + P['viral_q'] * q
            f = not v and flops < P['flop_cap'] and r.random() < P['flop_base'] * (1.4 - q)
            g = rate * P['viral_mult'] if v else (-rate * .3 if f else rate)
            if v: since_v = 0; v_n += 1
            f_n += f
            if g > 0: g *= (P['own'] if own else 1) * (P['rested'] if rested and i <= 3 else 1) * (P['starter'] if subs < 1000 else 1)
            subs = max(120, subs * (1 + g * mult)); flops = flops + 1 if f else 0
            for k2 in xp:
                if i > 10: break
                b4 = lv(xp[k2]); xp[k2] += (1 if k2 == a.get('main', 'h') else .35) * (1.3 if own else 1) * (2 if i <= 3 else 1)
                if lv(xp[k2]) > b4: ev.append(f'{ {"b":"смелость","h":"юмор","t":"в тему"}[k2]} ур.{lv(xp[k2])}')
            j = 0 if total % 3 else 1; rel[j] = min(5, rel[j] + P['rel_per_post'] * (1 + q) * (2 if i <= 3 else 1))
        rel[0] = min(5, rel[0] + P['rel_dm'])
        for j in range(3): rel[j] = min(rel[j], day_rel[j] + 1)
        if max(rel) >= 3 > max(day_rel): ev.append('карточка звезды'); cards += 1
        if v_n: ev.append(f'залёт ×{v_n}'); cards = min(12, cards + 1)
        if f_n: ev.append(f'провалов {f_n}')
        if subs >= 5000:
            if next_hate is None: next_hate = d
            if d >= next_hate: ev.append('ХЕЙТ-ВОЛНА'); subs *= 1.07; xp['b'] += 3; next_hate = d + r.randint(3, 4); cards += 1
        subs *= 1.05
        for m in (1000, 5000, 10000, 100000):
            if s0 < m <= subs: ev.append(f'этап {m//1000}k')
        if worlds == 1 and subs >= 10000: worlds = 2; subs *= .5; ev.append('НОВЫЙ МИР (½ подписчиков)')
        print(f' д{d:>2}: {n:>2} постов  {int(s0):>7} → {int(subs):>7}  {", ".join(ev)}')
A['парень Айти'] = dict(ppd=10, skill=.55, ownp=.5, plus=False, show=.85, main='b')
diary('Лиза, средняя, Сцена', A['средний'])
diary('Аня, казуал, заходит через день', A['казуал'], skip=(2, 4, 5, 8))
diary('Макс, Айти-тусовка, смелость', A['парень Айти'])
diary('Реки+', A['Реки+'])
diary('Слабый автор, только подсказки', A['слабый автор'])

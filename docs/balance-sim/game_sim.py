# Симуляция игры «Реки» по правилам docs/game-flow.md (golden-mouth). Запуск: python3 game_sim.py [v1|v2]
import random, statistics as st, sys, math
V = sys.argv[1] if len(sys.argv) > 1 else 'v1'

# ---------- параметры ----------
P = dict(
  posts_free=10, posts_plus=30, decay=[(5, 1.0), (10, .8), (99, .6)],
  rate_lo=.05, rate_hi=.15, own_words=1.5, slow_k=1.0,      # рост за пост, замедление с размером
  viral_base=.05, viral_q=.10, viral_mult=5, pity=10, flop_base=.15,
  xp_per_post=1.0, lvl=[0, 6, 18, 36],                        # опыт до уровней 1..4 (на навык)
  event_gain=(.05, .15), hate_from=5000, hate_every=(2, 3), hate_gain=(.08, .2),
  world_need={'Сцена': None, 'Стрим': None, 'Академия': ('humor', 2), 'Айти': ('brave', 3)},
)
if V == 'v2':  # правки по итогам v1
  P.update(slow_k=1.6, rate_lo=.04, rate_hi=.11, lvl=[0, 5, 14, 30], decay=[(5, 1.0), (10, .7), (99, .4)],
           viral_mult=3.5, world_need={'Сцена': None, 'Стрим': None, 'Академия': ('humor', 2), 'Айти': None})
  P['flop_cap'] = 2   # не больше 2 провалов подряд
  P.update(starter=2.0, own_words=1.3, decay=[(5, 1.0), (10, .7), (99, .25)])
  P['rested'] = 2.0   # «отдохнувший канал»: после пропуска первые 3 поста ×2
  P.update(pow=float(sys.argv[2]) if len(sys.argv) > 2 else .6, k=float(sys.argv[3]) if len(sys.argv) > 3 else 1.0,
           viral_base=.03, viral_q=.06, pity=15, viral_mult=3, event_gain=(.03, .08), hate_gain=(.04, .1))

ARCH = {  # постов в день, вероятность зайти в день, мастерство 0..1, доля своих слов, Реки+, друзей зовёт за всё время, навык-уклон
  'казуал':        dict(ppd=3,  show=.6,  skill=.4, own=.3, plus=False, inv=0, bias='humor'),
  'средний':       dict(ppd=10, show=.85, skill=.55, own=.5, plus=False, inv=1, bias='humor'),
  'вовлечённый':   dict(ppd=10, show=1,   skill=.7, own=.8, plus=False, inv=3, bias='brave'),
  'Реки+':         dict(ppd=30, show=1,   skill=.6, own=.6, plus=True,  inv=0, bias='humor'),
  'только подсказки': dict(ppd=10, show=.9, skill=.45, own=0, plus=False, inv=0, bias='topic'),
  'парень в Айти': dict(ppd=10, show=.85, skill=.55, own=.5, plus=False, inv=1, bias='brave'),
  'зовущий друзей':dict(ppd=10, show=.9,  skill=.55, own=.5, plus=False, inv=12, bias='humor'),
}

def level(xp):
  return max(i + 1 for i, t in enumerate(P['lvl']) if xp >= t)

def run(a, days=30, seed=None):
  rnd = random.Random(seed)
  subs, xp = 120.0, {'brave': 0, 'humor': 0, 'topic': 0}
  since_viral, flops_row, posts_total, next_hate = 0, 0, 0, None
  last_day = 1
  log = dict(milestones={}, unlock={}, quiet=0, flop_streak=0, virals=0, hates=0, posts=0, lvl_day={})
  invites_left = a['inv']
  for d in range(1, days + 1):
    if rnd.random() > a['show'] and d > 1: continue
    got_new = False
    rested = P.get('rested') and d > 1 and last_day < d - 1
    last_day = d
    cap = P['posts_plus'] if a['plus'] else P['posts_free']
    if invites_left and rnd.random() < .5: cap += 5; invites_left -= 1
    n = min(a['ppd'] if a['ppd'] < 30 else cap, cap)
    for i in range(1, n + 1):
      posts_total += 1; log['posts'] += 1
      lv = sum(level(x) for x in xp.values()) / 3
      q = min(1, a['skill'] * .8 + (lv - 1) * .07 + rnd.uniform(-.15, .15))
      own = rnd.random() < a['own']
      mult = next(m for lim, m in P['decay'] if i <= lim)
      if P.get('pow'):
        rate = P['k'] * (.5 + q) * subs ** P['pow'] / subs   # прирост ~ subs^pow
      else:
        size = 1 + P['slow_k'] * max(0, math.log10(subs / 1000)) if subs > 1000 else 1
        rate = (P['rate_lo'] + (P['rate_hi'] - P['rate_lo']) * q) / size
      since_viral += 1
      viral = posts_total == 3 or since_viral >= P['pity'] or rnd.random() < P['viral_base'] + P['viral_q'] * q
      flop = not viral and rnd.random() < P['flop_base'] * (1.4 - q)
      if 'flop_cap' in P and flops_row >= P['flop_cap']: flop = False
      if viral: g = rate * P['viral_mult']; since_viral = 0; log['virals'] += 1
      elif flop: g = -rate * .3
      else: g = rate
      if own and g > 0: g *= P['own_words']
      if rested and i <= 3 and g > 0: g *= P['rested']
      if P.get('starter') and subs < 1000 and g > 0: g *= P['starter']   # быстрый старт до 1k
      subs = max(120, subs * (1 + g * mult))
      flops_row = flops_row + 1 if flop else 0
      log['flop_streak'] = max(log['flop_streak'], flops_row)
      # опыт: главный навык игрока + немного остальным
      for k in xp:
        before = level(xp[k])
        xp[k] += P['xp_per_post'] * (1 if k == a['bias'] else .35) * (1.3 if own else 1)
        if level(xp[k]) > before: got_new = True; log['lvl_day'].setdefault(f'{k}{level(xp[k])}', d)
    # событие дня
    subs *= 1 + rnd.uniform(*P['event_gain'])
    if subs >= P['hate_from']:
      if next_hate is None: next_hate = d
      if d >= next_hate:
        subs *= 1 + rnd.uniform(*P['hate_gain']); log['hates'] += 1; xp['brave'] += 3
        next_hate = d + rnd.randint(*P['hate_every']); got_new = True
    for m in (1000, 5000, 10000, 100000, 1000000):
      if subs >= m and m not in log['milestones']: log['milestones'][m] = d; got_new = True
    for w, need in P['world_need'].items():
      if w not in log['unlock'] and (need is None or level(xp[need[0]]) >= need[1] or a['plus']):
        log['unlock'][w] = d; got_new = got_new or d > 1
    if not got_new and d <= 14: log['quiet'] += 1
  log['subs'] = subs
  return log

def agg(name, a, N=400):
  L = [run(a, seed=s) for s in range(N)]
  med = lambda f: st.median([f(l) for l in L])
  def mday(m): 
    v = [l['milestones'].get(m, 99) for l in L]; return st.median(v)
  def uday(w):
    v = [l['unlock'].get(w, 99) for l in L]; return st.median(v)
  print(f"{name:17} 1k:{mday(1000):>3} 5k:{mday(5000):>3} 10k:{mday(10000):>3} 100k:{mday(100000):>3} 1M:{mday(1000000):>3} | "
        f"Акад:{uday('Академия'):>3} Айти:{uday('Айти'):>3} | тихих дней из 14:{med(lambda l:l['quiet']):>4} "
        f"провалов подряд max:{med(lambda l:l['flop_streak']):>3} залётов:{med(lambda l:l['virals']):>4} хейт:{med(lambda l:l['hates']):>3} "
        f"d30:{med(lambda l:l['subs'])/1000:>7.0f}k")
print('версия', V, '(медиана по 400 игрокам, день события; 99 = не дошёл за 30 дней)')
for k, a in ARCH.items(): agg(k, a)

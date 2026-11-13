"""Compare an ordinary non-viral post with a repost using the current balance."""
import json
from pathlib import Path

balance = json.loads((Path(__file__).parents[1] / 'config/balance.json').read_text())
growth = balance['growth']
print('difficulty | subscribers | energy index | ordinary quality=0 | repost')
for name, difficulty in balance['difficulties'].items():
    for subscribers in (1, 120, 1000, 10000, 1000000):
        for index in (1, 6, 11):
            decay = next(item['x'] for item in growth['decay'] if item['from'] <= index <= item['to'])
            base = growth['k'] * difficulty['k'] * .5 * subscribers ** growth['power'] * decay
            ordinary = max(1, round(base * (growth['starter_x'] if subscribers < growth['starter_below_subs'] else 1)))
            repost = max(1, round(base * balance['repost']['growth_multiplier']))
            assert repost <= ordinary, (name, subscribers, index)
            print(f'{name} | {subscribers} | {index} | {ordinary} | {repost}')
# Reposts spend energy but grant no skill/crystal awards. This is a comparison of
# growth per action, not a model of player retention or likelihood of a flop.

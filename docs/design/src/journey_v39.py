# Примеры шкалы серии на канале и первом экране.
def streak39(unit, labels, completed):
    blocks = ''
    for i, label in enumerate(labels):
        style = ('background:linear-gradient(90deg,#FF6B8B,#9B6BFF)' if i < completed
                 else 'border:2px dashed #FF6B8B' if i == completed else 'background:#20202B')
        yearly = unit == 'Месяцы'
        size = 'aspect-ratio:1/1;' if yearly else 'height:30px;'
        radius = 5 if yearly else 9
        font_size = 12 if yearly else 14
        label_style = 'font-size:clamp(8px,2.4vw,10px);line-height:1;white-space:nowrap;' if yearly else ''
        flame = '🔥' if i < completed else ''
        blocks += (f'<div style="flex:1;min-width:0;text-align:center"><div style="{size}border-radius:{radius}px;display:flex;'
                   f'align-items:center;justify-content:center;font-size:{font_size}px;{style}">{flame}</div>'
                   f'<div class="small muted" style="margin-top:3px;{label_style}">{label}</div></div>')
    duration = '2 месяца' if unit == 'Месяцы' else '1 неделя'
    return (f'<div class="card" style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;'
            f'margin-bottom:8px"><b>Серия {duration}</b><span class="small" style="color:#FFD66B">ещё день — и бонус</span></div>'
            f'<div class="small muted" style="margin-bottom:6px">{unit}</div><div style="display:flex;gap:6px">{blocks}</div></div>')

marker39 = '<div style="padding:18px 20px">'
for key, card in [
    ('03-5_stage', streak39('Недели', ['1', '2', '3', '4'], 1)),
    ('01-6_home', streak39('Месяцы', ['окт', 'ноя', 'дек', 'янв', 'фев', 'мар', 'апр', 'май', 'июн', 'июл', 'авг', 'сен'], 2)),
]:
    assert marker39 in S[key]
    S[key] = S[key].replace(marker39, marker39 + card, 1)

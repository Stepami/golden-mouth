# Пример серии после двух лет по 360 дней.
years40 = ''
for i, label in enumerate(['2025', '2026', '2027']):
    style = ('background:linear-gradient(90deg,#FF6B8B,#9B6BFF)' if i < 2
             else 'border:2px dashed #FF6B8B')
    flame = '🔥' if i < 2 else ''
    years40 += (f'<div style="flex:1;min-width:0;text-align:center"><div style="height:30px;width:30px;margin:0 auto;'
                f'aspect-ratio:1/1;border-radius:9px;display:flex;align-items:center;justify-content:center;'
                f'font-size:14px;{style}">{flame}</div><div class="small muted" style="margin-top:3px">{label}</div></div>')

card40 = ('<div class="card" style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;'
          'margin-bottom:8px"><b>Серия 2 года</b><span class="small" style="color:#FFD66B">ещё день — и бонус</span></div>'
          '<div class="small muted" style="margin-bottom:6px">Годы</div><div style="display:flex;gap:6px">' + years40 + '</div></div>')

previous40 = streak39('Недели', ['1', '2', '3', '4'], 1)
assert previous40 in S['03-5_stage']
S['03-5_stage-years'] = S['03-5_stage'].replace(previous40, card40, 1)

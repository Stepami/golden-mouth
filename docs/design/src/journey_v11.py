# Правки v11 (#69): аватарка собирается из ника, отдельного выбора на 01-4 нет.

_head, _rest = S['01-4_channel'].split('<div style="margin:12px 0 8px" class="muted small">АВАТАРКА</div>')
_rest = _rest.split('<div style="margin:12px 0 8px" class="muted small">НАЗВАНИЕ И НИК</div>', 1)[1]
S['01-4_channel'] = (_head
                     + '<div style="margin:12px 0 8px" class="muted small">НАЗВАНИЕ И НИК</div>'
                     + '<div class="muted small" style="margin-bottom:8px">аватарка соберётся из ника: поменяешь ник, поменяется и она</div>'
                     + _rest)

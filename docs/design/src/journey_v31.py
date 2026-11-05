# #250: invite one friend is the primary way to unlock a custom world.
import re

unlock_card = '''<div class="card" style="margin:20px 0">
<b>✦ Создай свой мир</b>
<div style="display:flex;justify-content:space-between;gap:12px;margin-top:16px;font-size:20px;font-weight:800"><span>Пригласи 1 друга</span><span style="color:#C3A8FF">0/1</span></div>
<div style="height:8px;background:#262634;border-radius:4px;margin:14px 0 10px"></div>
<p class="small muted">Мир откроется, когда друг опубликует первый пост.</p>
<button class="btn" style="width:100%;box-sizing:border-box">Пригласить друга</button>
<p class="small muted" style="margin:12px 0 0">Или набери 500 подписчиков в одном мире.</p>
</div>'''

for code in ('01-3_world', '05-5_collection'):
    S[code], count = re.subn(r'<div class="card"[^>]*><h2>[✦✨] Создай свой мир</h2>.*?</div>', unlock_card, S[code], count=1)
    assert count == 1, code

# The invitation action belongs to the card, rather than a duplicate fixed footer.
for code in ('01-3_world', '05-5_collection'):
    S[code] = re.sub(r'<div style="position:fixed;bottom:0;.*?</div>', '', S[code], count=1)

# #291: current UI Web Push, independent of the redesign.
import re
settings = S['05-8_settings']
settings = re.sub(r'(<div style="display:flex[^>]*><div style="flex:1">🔔 Уведомления</div><span class="muted">).*?(</span></div>)', r'\1›\2', settings)
settings, count = re.subn(r'<div style="display:flex[^>]*><div style="flex:1">🌙 Тихие часы</div><span class="muted">.*?</span></div>', '', settings)
assert count == 1, 'Quiet hours row must exist before #291'
assert 'до 3 в день' not in settings
settings = settings.replace('🎙 Голос канала', 'Установить приложение</div><span class="muted">›</span></div><div style="display:flex;align-items:center;gap:10px;padding:12px 0;border-bottom:1px solid #262634"><div style="flex:1">🎙 Голос канала', 1)
S['05-8_settings'] = settings
S['05-8a_push'] = P('''<div class="top"><span>‹</span><b>Настройки</b></div><div class="pad"><div class="card">🔔 Уведомления</div></div><div style="position:absolute;bottom:0;left:0;right:0;padding:24px 20px;background:#1C1C26;border-radius:24px 24px 0 0"><h2>Уведомления</h2><p>Браузер запросит разрешение по нажатию кнопки. Изменить его позже можно в настройках браузера или устройства.</p><button class="btn" style="width:100%;box-sizing:border-box;margin-bottom:10px">Разрешить уведомления</button><button class="btn" style="width:100%;box-sizing:border-box;background:#22222D">Закрыть</button></div>''')

S['01-3a_push_offer'] = P('<div class="top"><b>Миры</b></div><div class="pad"><div class="card"><b>Установить приложение</b><p class="small muted">Значок «Рек» появится рядом с другими приложениями.</p></div><div class="card"><b>Включить уведомления</b><p class="small muted">Напомним о твоём мире, даже когда игра закрыта.</p><button class="btn" style="width:100%;box-sizing:border-box">Разрешить</button><button class="btn" style="width:100%;box-sizing:border-box;margin-top:8px;background:#22222D">Позже</button></div><h2>Продолжить историю</h2></div>')

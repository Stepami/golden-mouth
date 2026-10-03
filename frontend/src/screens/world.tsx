import { useState } from 'react'
import { api } from '../api/client'
import { ownWorld, presetRules } from '../api/mock'
import type { Drama } from '../api/types'
import { usePlayer, useStore } from '../app/store'
import { useApi } from '../app/useApi'
import { useNav } from '../app/nav'
import { BottomCTA, Chip, Segmented, TopBar } from '../ui'
import { register } from '.'

/** 05-7b: свой мир. Игрок описывает мир голосом или текстом, AI собирает правила, игрок правит любое.
 *  Правило «без тем 18+» видно, но не правится (слой безопасности в коде, #9). */
function OwnWorld() {
  const nav = useNav()
  const [preset, setPreset] = useState(ownWorld.presets[0])
  const [desc, setDesc] = useState(ownWorld.description)
  const [rules, setRules] = useState(ownWorld.rules)
  const [edit, setEdit] = useState<number | null>(null)
  const [listening, setListening] = useState(false)
  const voice = () => {
    type SR = { lang: string; onresult: (e: { results: { 0: { 0: { transcript: string } } } }) => void; onend: () => void; start: () => void }
    const W = window as unknown as { SpeechRecognition?: new () => SR; webkitSpeechRecognition?: new () => SR }
    const Ctor = W.SpeechRecognition ?? W.webkitSpeechRecognition
    if (!Ctor) return
    const r = new Ctor(); r.lang = 'ru-RU'; setListening(true)
    r.onresult = e => setDesc(d => `${d} ${e.results[0][0].transcript}`.trim()); r.onend = () => setListening(false); r.start()
  }
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Свой мир" right="открыт · 10k" />
      <div className="pad">
        <div className="label">Начни с готового</div>
        <div style={{ marginBottom: 10 }}>{ownWorld.presets.map(p => <Chip key={p} idea={p === preset} onClick={() => setPreset(p)}>{p}</Chip>)}</div>
        <div className="card row" style={{ marginBottom: 10 }}>
          <textarea value={desc} onChange={e => setDesc(e.target.value)} rows={3} style={{ flex: 1, fontSize: 15, lineHeight: 1.4 }} maxLength={600} />
          <button onClick={voice} aria-label="Надиктовать" style={{ width: 44, height: 44, borderRadius: '50%', border: 'none', background: listening ? '#2E9E6F' : 'var(--grad)', fontSize: 20, flex: 'none' }}>🎙</button>
        </div>
        <div className="card" style={{ marginBottom: 10 }}>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 4 }}><b>Правила мира</b><span className="small" style={{ color: 'var(--lilac)' }}>AI собрал из описания</span></div>
          <div className="small muted" style={{ marginBottom: 4 }}>Звёзды и события следуют им. Поправь любое</div>
          {rules.map((r, i) => (
            <div key={i} className="row" style={{ alignItems: 'flex-start', padding: '7px 0', borderBottom: i < rules.length - 1 ? '1px solid var(--line2)' : 'none', fontSize: 14 }}>
              <span style={{ color: 'var(--up)' }}>✓</span>
              {edit === i ? <input autoFocus value={r.text} onChange={e => setRules(x => x.map((y, j) => j === i ? { ...y, text: e.target.value } : y))} onBlur={() => setEdit(null)} style={{ flex: 1 }} />
                : <span style={{ flex: 1 }}>{r.text}</span>}
              {r.locked ? <span className="small muted" title="Фильтр 14+ работает всегда">🔒</span> : <button onClick={() => setEdit(i)} className="muted small" style={{ background: 'none', border: 'none' }}>✎</button>}
            </div>
          ))}
        </div>
        <div className="small muted" style={{ textAlign: 'center' }}>Можно дописать голосом: «пусть физрук будет главным злодеем»</div>
      </div>
      <BottomCTA main="Создать мир" disabled={!desc.trim()} onMain={async () => { await api.saveWorld({ ...ownWorld, description: desc, rules }); nav.replace('01-5_first_star') }} />
    </div>
  )
}

register({ '05-7b_own_world': { component: OwnWorld, title: 'Свой мир' } })

/** 05-11: правила готового мира видны игроку (Кумен: AI не прячет, по каким правилам играет).
 *  В готовом мире правится только градус драмы; свой мир правится целиком на 05-7b. */
function WorldRulesScreen() {
  const nav = useNav(); const p = usePlayer(); const { patch } = useStore()
  const r = useApi(() => api.worldRules(p.world), presetRules)
  const [drama, setDrama] = useState<Drama>(p.settings.drama)
  const setD = (d: Drama) => { setDrama(d); patch({ settings: { ...p.settings, drama: d } }); api.settings({ drama: d }) }
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title={`Как устроена ${r.title === 'Сцена' ? 'Сцена' : r.title}`} />
      <div className="pad">
        <div className="muted" style={{ marginBottom: 12 }}>Звёзды, фанаты и события следуют этим правилам. Ничего скрытого: это всё, что знает AI о мире</div>
        <div className="card" style={{ marginBottom: 12 }}>
          {r.rules.map((x, i) => <div key={i} className="row" style={{ alignItems: 'flex-start', padding: '8px 0', borderBottom: i < r.rules.length - 1 ? '1px solid var(--line2)' : 'none', fontSize: 14 }}><span style={{ color: 'var(--up)' }}>✓</span><span style={{ flex: 1 }}>{x.text}</span></div>)}
        </div>
        <div className="card">
          <b>🌩 Градус драмы</b><div className="small muted" style={{ margin: '2px 0 8px' }}>Это можно менять. Фильтр 14+ работает всегда</div>
          <Segmented value={drama} onChange={setD} options={[{ id: 'soft', label: 'мягко' }, { id: 'mid', label: 'средне' }, { id: 'hot', label: 'остро' }, { id: 'off', label: 'без хейта' }]} />
        </div>
        <div className="small muted" style={{ textAlign: 'center', marginTop: 12 }}>Хочешь свои правила? Создай свой мир: откроется на 10k или с первым другом</div>
      </div>
    </div>
  )
}
register({ '05-11_world_rules': { component: WorldRulesScreen, title: 'Как устроен мир' } })

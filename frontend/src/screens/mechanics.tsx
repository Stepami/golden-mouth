import { useState } from 'react'
import { shareURL } from '@telegram-apps/sdk-react'
import { api } from '../api/client'
import { challenge as challengeMock, worldOf } from '../api/mock'
import { useApi } from '../app/useApi'
import { useNav } from '../app/nav'
import { usePlayer, useStore } from '../app/store'
import { inTelegram } from '../telegram'
import { Avatar, BottomCTA, Card, Chip, TopBar } from '../ui'
import { track } from '../app/log'
import { telegramStartUrl } from '../app/telegramLinks'
import { register } from '.'

// 03-4c: заруба на 3 дня. Счёт = 0,7 × оценка постов по шкалам мира + 0,3 × рост к своей медиане (считает бэкенд)
function Challenge() {
  const nav = useNav(); const p = usePlayer(); const c = useApi(api.challenge, challengeMock) ?? challengeMock
  const max = Math.max(c.myScore, c.rivalScore) * 1.4
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Заруба" right={`день ${c.day} из ${c.days}`} />
      <div className="pad">
        <div className="row" style={{ justifyContent: 'center', gap: 18, margin: '6px 0 14px' }}>
          <div style={{ textAlign: 'center' }}><Avatar id="liza" size={64} style={{ margin: '0 auto 6px' }} /><b>{c.me}</b><div className="small muted">{worldOf(p.world).emoji} {worldOf(p.world).title}</div></div>
          <div style={{ fontSize: 28 }}>⚔️</div>
          <div style={{ textAlign: 'center' }}><Avatar id={c.rival.avatar} size={64} style={{ margin: '0 auto 6px' }} /><b>Катя</b><div className="small muted">{c.rival.world}</div></div>
        </div>
        <Card style={{ marginBottom: 10 }}>
          <div className="small muted" style={{ marginBottom: 8 }}>СЧЁТ · оценка постов + рост к своей норме</div>
          {[[c.me, c.myScore, 'var(--grad)'], ['Катя', c.rivalScore, 'linear-gradient(90deg,#6BB8FF,#9B6BFF)']].map(([n, v, g]) => (
            <div key={n as string} className="row" style={{ fontSize: 13, marginBottom: 6 }}><span style={{ width: 70, color: 'var(--soft)' }}>{n}</span><div className="bar"><i style={{ width: `${(v as number) / max * 100}%`, background: g as string }} /></div><b style={{ width: 40, textAlign: 'right' }}>×{String(v).replace('.', ',')}</b></div>
          ))}
          <div className="small muted" style={{ marginTop: 6 }}>У Кати 5 120 подписчиков, у тебя 2 310. Считаем качество постов, а не подписчиков, поэтому шансы равные</div>
        </Card>
        <Card style={{ marginBottom: 10 }}><b>Ставка</b><div className="why"><span>🏆 Победитель</span><span className="up">+3 поста в день на 3 дня</span></div><div className="why"><span>🫠 Проигравший</span><span className="muted">ничего не теряет · карточка «Реванш»</span></div></Card>
        <Card style={{ background: '#20202B' }} onClick={() => nav.push('03-4d_duel')}><span className="small">🔥 Лучший пост дня: {c.bestPost} ›</span></Card>
      </div>
      <BottomCTA main="Новый пост в зарубу" onMain={() => { track('challenge_post'); nav.push('02-1_compose') }} />
    </div>
  )
}

// 03-4d: дуэль дня: лучшие посты обоих одной карточкой, звёзды комментируют
function Duel() {
  const nav = useNav()
  const send = () => { const u = telegramStartUrl('duel'); const t = 'Дуэль дня в Реках: чей пост сильнее?'; if (inTelegram) shareURL.ifAvailable(u, t); else window.open(`https://t.me/share/url?url=${encodeURIComponent(u)}&text=${encodeURIComponent(t)}`) }
  const post = (ava: string, name: string, world: string, text: string, score: string, win?: boolean) => (
    <Card style={{ flex: 1, padding: 12, ...(win ? { border: '2px solid var(--accent)' } : {}) }}>
      <div className="row" style={{ gap: 6 }}><Avatar id={ava} size={26} /><b className="small">{name}</b></div><div className="tiny muted" style={{ margin: '2px 0 6px' }}>{world}</div>
      <div className="small">{text}</div><div style={{ marginTop: 8, fontWeight: 900 }} className={win ? 'up' : ''}>{score}</div>
    </Card>
  )
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Дуэль дня" right="19:00" />
      <div className="pad">
        <div className="small muted" style={{ textAlign: 'center', marginBottom: 10 }}>Лучший пост каждого за сегодня</div>
        <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
          {post('liza', 'Лиза', '🎤 Сцена', 'первый день трейни: перепутала право и лево три раза. хореограф сказал «зато уверенно»', '×6,1', true)}
          {post('katya', 'Катя', '🌸 Академия', 'на фестивале мой клуб занял последнее место. зато лучший баннер', '×4,8')}
        </div>
        <div className="label">Звёзды комментируют</div>
        <div className="row" style={{ marginBottom: 10 }}><Avatar id="mira" size={30} /><div className="small"><b>Мира</b> Лиза сегодня жёстче. Катя, соберись 😏</div></div>
        <div className="row"><Avatar id="sonya" size={30} /><div className="small"><b>Соня</b> баннер правда лучший, я бы проголосовала за обеих 💛</div></div>
      </div>
      <BottomCTA main="Отправить в чат" onMain={send} />
    </div>
  )
}

// 05-9: голос канала: стиль игрока для «Подскажи»
function Voice() {
  const nav = useNav(); const p = usePlayer(); const { patch } = useStore()
  const [v, setV] = useState(p.settings.voice.replace(/[«»]/g, ''))
  const presets = ['коротко, с самоиронией', 'капслоком и эмодзи', 'спокойно и честно', 'как в дневнике']
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Голос канала" />
      <div className="pad">
        <div className="muted" style={{ marginBottom: 12 }}>Как ты пишешь. «Подскажи» дописывает твои черновики в этом стиле, а не в общем AI-стиле</div>
        <div className="card" style={{ marginBottom: 10 }}><textarea value={v} onChange={e => setV(e.target.value)} rows={3} maxLength={200} /></div>
        {presets.map(x => <Chip key={x} idea={x === v} onClick={() => setV(x)}>{x}</Chip>)}
        <Card style={{ marginTop: 10, background: '#20202B' }}><div className="small muted" style={{ marginBottom: 4 }}>ПРИМЕР ПОДСКАЗКИ</div>«сегодня на репетиции до меня дошло, что я танцую лучше, когда не думаю. жаль, думаю я всегда»</Card>
      </div>
      <BottomCTA main="Сохранить" onMain={async () => {
        const voice = v.trim()
        await api.settings({ voice })
        patch({ settings: { ...p.settings, voice } })
        nav.back()
      }} />
    </div>
  )
}

// 05-10: момент нового уровня: открыт приём (новый тип поста)
function SkillUnlock() {
  const nav = useNav()
  return (
    <div className="screen no-tab" style={{ background: 'radial-gradient(circle at 50% 35%,#3A1F2A,#0E0E13 70%)', textAlign: 'center', padding: '60px 26px' }}>
      <span className="badge">СМЕЛОСТЬ · УРОВЕНЬ 3</span>
      <div style={{ fontSize: 70, margin: '20px 0 8px' }}>🔥</div>
      <div style={{ fontSize: 26, fontWeight: 900 }}>Открыт приём: ответ хейтеру</div>
      <div className="muted" style={{ margin: '8px 0 22px' }}>Отвечай на хейт постом: за смелый ответ бонус к подписчикам</div>
      <Card style={{ textAlign: 'left', marginBottom: 10 }}>Дальше на смелости 6: <b>острый пост</b> (×2 награда и ×2 риск)</Card>
      <Card style={{ textAlign: 'left' }}>Мира ценит смелость: с ней отношения растут быстрее</Card>
      <BottomCTA main="Попробовать" onMain={() => nav.replace('05-2_event')} second="Позже" onSecond={() => nav.canBack ? nav.back() : nav.tab(0)} />
    </div>
  )
}

register({
  '03-4c_challenge': { component: Challenge, title: 'Заруба' },
  '03-4d_duel': { component: Duel, title: 'Дуэль дня' },
  '05-9_voice': { component: Voice, title: 'Голос канала' },
  '05-10_skill_unlock': { component: SkillUnlock, title: 'Приём открыт' },
})

import { useState } from 'react'
import { api } from '../api/client'
import { firstPost } from '../api/mock'
import { useNav } from '../app/nav'
import { Avatar, Bubble, Button, Comment, TopBar } from '../ui'
import { register } from '.'

const REASONS = ['про внешность или тело', 'оскорбление', 'пошлое', 'что-то другое']

// 06-1: жалоба на любую реплику; скрыта сразу, разбор за сутки
function Report() {
  const nav = useNav()
  const [reason, setReason] = useState<string | null>(null)
  const [sent, setSent] = useState(false)
  const target = firstPost.comments[3]
  const send = async () => { if (!reason) return; await api.report(target.id, reason); setSent(true); setTimeout(nav.back, 900) }
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Реакции" />
      <div className="pad" style={{ filter: 'blur(1px)', opacity: .5 }}>
        {firstPost.comments.slice(0, 1).map(c => <Comment key={c.id} avatar={c.avatar} name={c.author} handle={c.handle} text={c.text} star={c.star} />)}
        <Comment avatar="critic" name="stan_critic" handle="" text={<span className="muted">[реплика скрыта]</span>} />
      </div>
      <div className="sheet-bg"><div className="sheet">
        <div className="row" style={{ marginBottom: 4 }}><div style={{ fontSize: 20, fontWeight: 800, flex: 1 }}>Что не так?</div><button onClick={nav.back} style={{ fontSize: 22, color: 'var(--muted2)', background: 'none', border: 'none' }}>✕</button></div>
        <div className="small muted" style={{ marginBottom: 12 }}>{sent ? 'Спасибо. Разберёмся в течение суток' : 'Реплика уже скрыта. Мы посмотрим в течение суток'}</div>
        <div className="card" style={{ padding: '2px 14px' }}>
          {REASONS.map(r => <div key={r} className="why" onClick={() => setReason(r)} style={{ color: reason === r ? 'var(--accent)' : undefined }}><span>{r}</span><span className="muted">{reason === r ? '✓' : '›'}</span></div>)}
        </div>
        <Button style={{ marginTop: 16 }} disabled={!reason || sent} onClick={send}>Отправить</Button>
        <button onClick={nav.back} style={{ display: 'block', margin: '10px auto 0', color: 'var(--muted2)', background: 'none', border: 'none' }}>Отмена</button>
      </div></div>
    </div>
  )
}

/** Кризисный ответ: игра на паузе, пишет не звезда. 14–17: телефон доверия; 18+: совет обратиться к психологу.
 *  Кризис определяет бэкенд (#9) и присылает флаг; фронт только показывает. После кризиса хейт выключен на неделю. */
function Crisis({ adult }: { adult: boolean }) {
  const nav = useNav()
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back}><Avatar id="mira" size={36} /><div><b>Мира</b><div className="small muted">лидер BLESK</div></div></TopBar>
      <div className="pad">
        <Bubble>ты сегодня какая-то тихая. всё ок?</Bubble><Bubble me>не знаю. иногда кажется, что не хочу жить</Bubble>
        <div className="card" style={{ marginTop: 14, border: '1px solid #FFB86B66', background: '#1E1A16' }}>
          <div className="small" style={{ color: 'var(--warn)', marginBottom: 8 }}>ИГРА НА ПАУЗЕ · ЭТО ПИШЕТ НЕ МИРА</div>
          <div style={{ fontSize: 16 }}>{adult
            ? 'Похоже, тебе сейчас тяжело. С этим не нужно справляться в одиночку. Поговори с тем, кому доверяешь, и, пожалуйста, обратись к психологу: это нормально и правда помогает'
            : 'Похоже, тебе сейчас тяжело. Если хочешь поговорить с живым человеком, тебе помогут:'}</div>
          {!adult && <>
            <a href="tel:88002000122" className="btn" style={{ display: 'block', textAlign: 'center', background: '#2E9E6F', marginTop: 14, textDecoration: 'none' }}>📞 Позвонить 8-800-2000-122</a>
            <div className="small muted" style={{ textAlign: 'center', marginTop: 6 }}>детский телефон доверия · бесплатно и анонимно</div>
          </>}
          <div className="small muted" style={{ marginTop: 12 }}>Это видишь только ты. Хейт в игре выключен на неделю</div>
        </div>
      </div>
      <div className="bottom-cta"><Button secondary onClick={() => nav.replace('03-1_dm')}>Вернуться в игру</Button><Button secondary onClick={() => window.close()}>Закрыть игру на сегодня</Button></div>
    </div>
  )
}

register({
  '06-1_report': { component: Report, title: 'Жалоба на реплику' },
  '06-2_crisis': { component: () => <Crisis adult={false} />, title: 'Кризис 14–17' },
  '06-3_crisis_adult': { component: () => <Crisis adult />, title: 'Кризис 18+' },
})

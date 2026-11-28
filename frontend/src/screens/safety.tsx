import { useEffect, useState } from 'react'
import { miniApp } from '@tma.js/sdk'
import { api } from '../api/client'
import { useNav } from '../app/nav'
import { Avatar, Bubble, Button, TopBar } from '../ui'
import { inTelegram } from '../telegram'
import { track } from '../app/log'
import { register } from '.'

// Report submission is hidden until backend processing is available.
function Report() {
  const nav = useNav()
  return <div className="screen no-tab"><TopBar title="Реакции" onBack={nav.back} /><div className="pad"><p>Этот раздел пока недоступен.</p><Button onClick={nav.back}>Назад</Button></div></div>
}

/** Кризисный ответ: игра на паузе, пишет не звезда. 14–17: телефон доверия; 18+: совет обратиться к психологу.
 *  Кризис определяет бэкенд (#9) и присылает флаг; фронт только показывает. После кризиса хейт выключен на неделю. */
function Crisis({ adult }: { adult: boolean }) {
  const nav = useNav()
  useEffect(() => { track('crisis_shown', { adult }) }, [adult])
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back}><Avatar seed="mira" mode="default" size={36} /><div><b>Мира</b><div className="small muted">лидер BLESK</div></div></TopBar>
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
      <div className="bottom-cta"><Button secondary onClick={() => nav.replace('03-1_dm')}>Вернуться в игру</Button><Button secondary onClick={() => inTelegram ? miniApp.close.ifAvailable() : nav.replace('01-6_home')}>Закрыть игру на сегодня</Button></div>
    </div>
  )
}

function PublicationRules() {
  const nav = useNav()
  return <div className="screen no-tab publication-rules"><TopBar onBack={nav.back} title="Правила публикаций" /><div className="pad">
    <h1>Твоя история — с уважением</h1><div className="publication-guidelines"><p>✨ Пиши о находках, чувствах и приключениях.</p><p>🤝 Спорь с идеями, береги людей.</p><p>🌿 Выбирай детали, подходящие твоему возрасту.</p></div><p className="small muted">Если тексту понадобится правка, подскажем. Черновик сохранится, энергия останется.</p>
    <Button onClick={nav.back}>Вернуться</Button>
  </div></div>
}
register({
  '06-4_rules': { component: PublicationRules, title: 'Правила публикаций' },
  '06-1_report': { component: Report, title: 'Жалоба на реплику' },
  '06-2_crisis': { component: () => <Crisis adult={false} />, title: 'Кризис 14–17' },
  '06-3_crisis_adult': { component: () => <Crisis adult />, title: 'Кризис 18+' },
})

import { useState } from 'react'
import { useNav } from '../app/nav'
import { Button, Sheet } from '.'

const examples = [
  ['🔥 Смелость', 'Выскажи своё мнение', '«Я вижу это иначе. Вот что заставило меня так подумать…»'],
  ['😂 Юмор', 'Найди смешную деталь', '«Кажется, у этого дня свои планы. Со мной их не согласовали».'],
  ['🎯 В тему', 'Продолжи то, что обсуждают', 'Обратись к персонажу, назови событие из его поста и задай свой вопрос.'],
] as const

export function SkillHelp({ onClose, initialExample = null, editor = false }: { onClose: () => void; initialExample?: number | null; editor?: boolean }) {
  const nav = useNav()
  const [selected, setSelected] = useState<number | null>(initialExample)
  const example = selected === null ? undefined : examples[selected]
  if (example) return <Sheet title={example[0]} className="skill-explanation-sheet" onClose={onClose}><section className="skill-explanation"><button className="text-button" onClick={() => setSelected(null)}>← Все навыки</button><h3>{example[1]}</h3><p>{example[2]}</p><Button onClick={() => { onClose(); if (!editor) nav.push('02-1_compose') }}>{editor ? 'Вернуться к тексту' : 'Попробовать в посте'}</Button></section></Sheet>
  return <Sheet title="Как поднять навыки" className="skill-help-sheet" onClose={onClose}>
    <ul className="skill-help-list"><li><b>🔥 Смелость</b><span>Делись опытом и отвечай по делу</span></li><li><b>😂 Юмор</b><span>Находи смешное в событиях мира</span></li><li><b>🎯 В тему</b><span>Пиши о героях и событиях своего мира</span></li></ul>
    <p className="small muted" style={{ margin: '12px 0' }}>Опыт за пост дают первые 10 единиц дневного расхода энергии; ответы в треде тоже расходуют этот бюджет. Первые 3 единицы дают ×2 опыта, выполненное желание — ×1,3. Главный навык получает весь опыт, остальные — по 15%. Даже неудачный пост помогает расти.</p>
    <div className="skill-example-links">{examples.map((example,index) => <button key={example[0]} onClick={() => setSelected(index)}>{example[0]}<span>Пример →</span></button>)}</div><Button onClick={onClose}>{editor ? 'Вернуться к тексту' : 'Понятно'}</Button>
  </Sheet>
}

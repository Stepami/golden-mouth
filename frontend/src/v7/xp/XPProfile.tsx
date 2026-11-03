import { useState } from 'react';
import type { Player } from '../../api/types';
import { readLocal, saveLocal } from '../backend';
import { Button, Progress } from '../ui';
import { allocate, formatXP, sampleWallet, upgradeCost, validWallet, type XPWallet } from './wallet';
const names = ['Смелость', 'Юмор', 'В тему'];
const ids = ['brave', 'humor', 'topic'] as const;
const zero = (): [number, number, number] => [0, 0, 0];
export function XPProfile({ player }: { player: Player }) {
  const storageKey = `reki:v7:${player.id}:xp-preview:${player.worldInstanceId ?? player.world}`;
  const [wallet, setWallet] = useState<XPWallet | undefined>(() => {
    const saved = readLocal<unknown>(storageKey, undefined); return validWallet(saved) ? saved : undefined;
  });
  const [points, setPoints] = useState(zero);
  const [message, setMessage] = useState('');
  const proposed = wallet ? allocate(wallet, points)! : undefined;
  const spent = wallet && proposed ? wallet.available - proposed.available : 0;
  const save = (next: XPWallet) => { setWallet(next); return saveLocal(storageKey, next); };
  const open = () => { const saved = readLocal<unknown>(storageKey, undefined); if (validWallet(saved)) { setWallet(saved); setPoints(zero()); setMessage('Открыто сохранённое пробное распределение.'); } else start(); };
  const start = () => { setPoints(zero()); setMessage(save(sampleWallet()) ? 'Доступно 36 пробных XP.' : 'Проба открыта. Не удалось сохранить её на устройстве.'); };
  const change = (index: number, delta: number) => {
    if (!wallet) return;
    setPoints(old => { const next = [...old] as typeof old; next[index] += delta; return allocate(wallet, next) ? next : old; });
    setMessage('');
  };
  const apply = () => {
    if (!wallet || !spent) return;
    const next = allocate(wallet, points); if (!next) return;
    const saved = save(next); setPoints(zero());
    setMessage(saved ? `Распределено ${formatXP(spent)} XP. Изменения сохранены только в пробе.` : 'Распределение применено в пробе, но не сохранено на устройстве.');
  };
  return <section className="section stack xp-profile" aria-label="XP персонажа">
    <div className="xp-summary"><h2>Опыт персонажа</h2>{wallet && <span className="caption xp-preview-label">Локальная проба</span>}
      <p className="xp-balance" aria-live="polite"><strong>{wallet ? formatXP(wallet.total) : '—'}</strong><span aria-hidden="true"> / </span><strong>{wallet ? formatXP(wallet.available) : '—'}</strong><span> XP</span></p>
      <p className="caption muted">Всего за персонажа / доступно для навыков</p>
      <p className="caption muted">Общий XP накапливается и не уменьшается при улучшении навыков.</p>
    </div>
    <p className="caption muted">{wallet ? 'Пробные XP и уровни не влияют на игру. Показана новая шкала 1–100.' : 'XP-кошелёк ещё не подключён. Ниже — текущие навыки из игры. В локальной пробе доступно 36 XP для проверки распределения.'}</p>
    {ids.map((id, index) => {
      const level = proposed?.levels[index] ?? player.skills[id].level;
      const nextPoints = [...points] as typeof points; nextPoints[index]++;
      const canAdd = !!wallet && !!allocate(wallet, nextPoints);
      return <article className="xp-skill stack" key={id}>
        <div className="row between"><strong>{names[index]}</strong><span>{wallet ? `${wallet.levels[index]}${points[index] ? ` → ${level}` : ''} / 100` : `${level} / 10`}</span></div>
        <Progress label={wallet ? `${names[index]}: пробный уровень` : `${names[index]}: до следующего уровня`} value={wallet ? level : player.skills[id].progress} max={wallet ? 100 : 1} />
        {wallet && <><div className="xp-skill-actions"><Button variant="secondary" disabled={!points[index]} aria-label={`Убрать улучшение: ${names[index]}`} onClick={() => change(index, -1)}>−</Button><span>+{points[index]}</span><Button variant="secondary" disabled={!canAdd} aria-label={`Улучшить: ${names[index]}`} onClick={() => change(index, 1)}>+</Button></div>
          <p className="caption muted">{level >= 100 ? 'Максимальный уровень' : `Следующий уровень: ${formatXP(upgradeCost(level))} XP${!canAdd ? ' · не хватает XP' : ''}`}</p></>}
      </article>;
    })}
    {wallet ? <><p className="caption" aria-live="polite">К распределению: {formatXP(spent)} XP · останется {formatXP(proposed!.available)} XP</p><Button disabled={!spent} onClick={apply}>Применить улучшения</Button><Button variant="secondary" disabled={!spent} onClick={() => { setPoints(zero()); setMessage('Выбор отменён. XP не потрачены.'); }}>Отменить выбор</Button><Button variant="secondary" onClick={() => { setWallet(undefined); setPoints(zero()); setMessage('Показаны настоящие навыки персонажа.'); }}>Показать игровые навыки</Button><button className="link-button caption" onClick={start}>Начать пробу заново</button></> : <Button onClick={open}>Открыть пробу распределения XP</Button>}
    <p role="status" className="caption">{message}</p>
  </section>;
}

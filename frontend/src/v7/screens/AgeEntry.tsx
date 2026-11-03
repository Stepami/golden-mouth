import { useEffect, useId, useRef, useState } from 'react';
import { Button, Notice } from '../ui';

const ages = Array.from({ length: 92 }, (_, index) => index + 8);
export function AgeEntry({ initialAge = 18, minimumAge, requireConsent, onBack, onContinue }: {
  initialAge?: number; minimumAge?: number; requireConsent: boolean;
  onBack: (age: number) => void; onContinue: (age: number) => void;
}) {
  const [age, setAge] = useState(initialAge);
  const [agreed, setAgreed] = useState(false);
  const [error, setError] = useState('');
  const wheel = useRef<HTMLDivElement>(null);
  const prefix = useId();
  const rowHeight = () => wheel.current?.querySelector('button')?.getBoundingClientRect().height || 48;
  useEffect(() => { if (wheel.current) wheel.current.scrollTop = (initialAge - 8) * rowHeight(); }, [initialAge]);
  const select = (value: number) => {
    setAge(value); setError('');
    wheel.current?.scrollTo({ top: (value - 8) * rowHeight(), behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth' });
  };
  const next = () => {
    if (minimumAge !== undefined && age < minimumAge) { setError(`Сейчас начать игру можно с ${minimumAge} лет.`); return; }
    onContinue(age);
  };
  return <section className="age-entry">
    <span className="brand-small">реки®</span>
    <button className="back-button" onClick={() => onBack(age)}>Назад</button>
    <div className="stack"><p className="caption muted">Знакомство</p><h1 className="display">Сколько<br />тебе лет?</h1><p className="muted">Дальше — настроим подборку миров.</p></div>
    <div className="age-entry-wheel">
      <div className="age-entry-selection" aria-hidden="true" />
      <div ref={wheel} className="age-entry-options" role="listbox" tabIndex={0} aria-label="Возраст" aria-activedescendant={`${prefix}-${age}`}
        onScroll={() => { if (wheel.current) { setAge(Math.max(8, Math.min(99, 8 + Math.round(wheel.current.scrollTop / rowHeight())))); setError(''); } }}
        onKeyDown={event => {
          if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return;
          event.preventDefault(); select(event.key === 'Home' ? 8 : event.key === 'End' ? 99 : Math.max(8, Math.min(99, age + (event.key === 'ArrowDown' ? 1 : -1))));
        }}>
        {ages.map(value => <button id={`${prefix}-${value}`} key={value} role="option" tabIndex={-1} aria-selected={value === age} onClick={() => select(value)}>{value}<span className="caption">{value === age ? ' лет' : ''}</span></button>)}
      </div>
    </div>
    {requireConsent && <label className="age-entry-consent"><input type="checkbox" checked={agreed} onChange={event => setAgreed(event.target.checked)} /><span>Даю согласие на обработку персональных данных.</span></label>}
    {error && <Notice title="Пока не получится продолжить" tone="error">{error}</Notice>}
    <Button size="lg" className="full age-entry-continue" disabled={requireConsent && !agreed} onClick={next}>Продолжить ↗</Button>
  </section>;
}

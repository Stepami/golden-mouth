import { useEffect, useState } from 'react';
import { avatarUri } from '../../ui/avatarRenderer';
import { Button, Chip } from '../ui';
import skillCatalog from '../skillCatalog.json' with { type: 'json' };
export type HeroOption = { id: string; name: string; role: string; goal: string };
export async function heroPortrait(id: string, seed: string, worldId: string, style: string) {
  const photos: Record<string, string> = { harry: 'harry', hermione: 'hermione', ron: 'ron' };
  return photos[id] ? `/assets/avatars/${photos[id]}.jpg` : avatarUri(seed, worldId, undefined, style);
}
export function HeroPicker({ options, selected, onChoose, cover, worldId, style, seed, customAvatar, removed, skills, goal }: {
  options: HeroOption[]; selected: string | null; onChoose: (id: string) => void;
  cover: string; worldId: string; style: string; seed: string; customAvatar?: string; removed: boolean; skills: string[]; goal: string;
}) {
  const index = Math.max(0, options.findIndex(hero => hero.id === selected));
  useEffect(() => { if (!selected && options[0]) onChoose(options[0].id); }, [selected, options[0]?.id]);
  const hero = options[index]; const [portrait, setPortrait] = useState('');
  useEffect(() => { let active = true; setPortrait(''); if (!removed) void heroPortrait(hero.id, seed, worldId, style).then(src => { if (active) setPortrait(src); }).catch(() => {}); return () => { active = false; }; }, [hero.id, seed, worldId, style, removed]);
  const turn = (delta: number) => onChoose(options[(index + delta + options.length) % options.length].id);
  return <section className="hero-picker stack" aria-label="Выбор персонажа" aria-roledescription="карусель" onKeyDown={e => { if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') { e.preventDefault(); turn(e.key === 'ArrowLeft' ? -1 : 1); } }}>
    <div className="hero-portrait-stage" style={{ backgroundImage: `linear-gradient(transparent, rgba(0,0,0,.65)), url("${cover}")` }}>
      <button className="hero-turn" aria-label="Предыдущий персонаж" onClick={() => turn(-1)}>←</button>
      <div className="hero-portrait" key={`${hero.id}:${seed}:${customAvatar ? 'uploaded' : 'generated'}`} tabIndex={0} aria-label={`Аватар: ${hero.name}`} onPointerMove={e => { const r = e.currentTarget.getBoundingClientRect(); e.currentTarget.style.setProperty('--portrait-turn', `${((e.clientX-r.left)/r.width-.5)*30}deg`); }} onPointerLeave={e => e.currentTarget.style.setProperty('--portrait-turn','0deg')}>
        {!removed && (customAvatar || portrait) ? <img src={customAvatar || portrait} alt={hero.name} /> : <span>{hero.name.split(' ').map(s => s[0]).slice(0,2).join('')}</span>}
      </div>
      <button className="hero-turn" aria-label="Следующий персонаж" onClick={() => turn(1)}>→</button>
    </div>
    <p className="caption muted" aria-live="polite">Персонаж {index + 1} из {options.length}</p>
    <h2>{hero.name}</h2><p className="caption muted">{hero.role}</p>
    <p><strong>Твоя цель в истории</strong><br />{goal || hero.goal}</p>
    <p className="caption"><strong>Навыки для прокачки</strong><br />{skills.length ? skills.join(' · ') : 'Выбери три навыка ниже'}</p>
  </section>;
}
export function SkillPicker({ selected, onChange }: { selected: string[]; onChange: (value: string[]) => void }) {
  const [query, setQuery] = useState('');
  return <section className="stack"><h2>Выбери навыки для прокачки {selected.length}/3</h2>
    <details className="skill-catalog"><summary>Открыть каталог навыков</summary><div className="stack">
      <label>Найти навык<input type="search" value={query} onChange={e => setQuery(e.target.value)} /></label>
      <div className="skill-options">{skillCatalog.filter(name => name.toLocaleLowerCase('ru').includes(query.toLocaleLowerCase('ru'))).map(name => <Chip key={name} selected={selected.includes(name)} disabled={selected.length >= 3 && !selected.includes(name)} onClick={() => onChange(selected.includes(name) ? selected.filter(s => s !== name) : [...selected, name])}>{name}</Chip>)}</div>
    </div></details><p className="caption muted">{selected.join(' · ') || 'Пока ничего не выбрано'}</p><p className="caption muted">Выбор сохранится на этом устройстве. На игровые шкалы он пока не влияет.</p>
  </section>;
}
export function AvatarEditor({ onGenerate, onUpload, onRemove }: { onGenerate: () => void; onUpload: (src: string) => void; onRemove: () => void }) {
  const [error, setError] = useState('');
  return <section className="stack"><h2>Твой аватар</h2><div className="row avatar-actions"><Button variant="secondary" onClick={() => { setError(''); onGenerate(); }}>Сгенерировать другой</Button>
    <label className="avatar-upload">Загрузить свою<input type="file" accept="image/png,image/jpeg,image/webp" onChange={e => { const file = e.target.files?.[0]; if (!file) return; if (!['image/png','image/jpeg','image/webp'].includes(file.type) || file.size > 1024 * 1024) { setError('Выбери PNG, JPG или WebP до 1 МБ.'); return; } const reader = new FileReader(); reader.onload = () => { setError(''); onUpload(String(reader.result)); }; reader.onerror = () => setError('Не удалось прочитать изображение.'); reader.readAsDataURL(file); e.target.value = ''; }} /></label>
    <Button variant="ghost" onClick={onRemove}>Удалить аватар</Button></div><p className="caption muted">Изображение останется на этом устройстве.</p>{error && <p role="alert">{error}</p>}</section>;
}

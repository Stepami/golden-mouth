import {eveningLooks,customLook} from '../avatarBuilder';
import {syncBackButton} from '../../telegram';
import {useEffect, useRef, useState} from 'react';
import {Button, Chip, Dialog, Icon, TextField, ScreenHeading} from '../ui';
import {AvatarBuilder} from './AvatarBuilder';
import {heroPortraitSource, type HeroOption} from './HeroPicker';
import {recommendRoles, skillDirections} from '../story/roles';
import {SkillArt} from '../ui/GameArt';
import './heroStudio.css';

export function HeroStudio({options, selected, onChoose, name, channel, goal, onName, onChannel, skills, onSkills, seed, avatar, removed, onLook, onUpload, onRemove, address, onAddress, worldId, worldTitle, style, busy, canEnter, onEnter, onBack, onAppearance, startWithHero=false}: {
 options:HeroOption[]; selected:string|null; onChoose:(id:string)=>void;
 name:string; channel:string; goal:string; onName:(value:string)=>void; onChannel:(value:string)=>void;
 skills:string[]; onSkills:(value:string[])=>void; seed:string; avatar?:string; removed:boolean;
 onLook:(seed:string)=>void; onUpload:(src:string)=>void; onRemove:()=>void;
 address:string; onAddress:(value:'she'|'he'|'nick')=>void; worldId:string; worldTitle:string; style:string;
 onAppearance:(description:string|undefined)=>void;
 busy:boolean; canEnter:boolean; onEnter:()=>void; onBack:()=>void; startWithHero?:boolean;
}) {
 const [panel,setPanel]=useState<'skills'|'look'>(startWithHero?'look':'skills');
 useEffect(()=>syncBackButton(!busy,()=>panel==='look'&&!startWithHero?setPanel('skills'):onBack()),[panel,busy,onBack,startWithHero]);
 const [sheet,setSheet]=useState<'identity'|'goal'|'skills'|null>(null);
 const [query,setQuery]=useState(''),[directions,setDirections]=useState<string[]>([]),[uploadError,setUploadError]=useState('');
 const root=useRef<HTMLElement>(null);
 const uploadInput=useRef<HTMLInputElement>(null);
 useEffect(()=>{root.current?.closest('.app-body')?.scrollTo({top:0});},[panel]);
 const swipe=useRef<{x:number;y:number}|null>(null);
 const index=Math.max(0,options.findIndex(hero=>hero.id===selected));
 const custom=selected==='custom';
 useEffect(()=>{if(!selected&&options[0])onChoose(options[0].id);},[selected,options[0]?.id]);
 const portrait=heroPortraitSource(selected||options[0].id,seed,worldId,style);
 const displayedPortrait=custom&&avatar?avatar:portrait;
 const turn=(delta:number)=>onChoose(options[(index+delta+options.length)%options.length].id);
 const toggle=(skill:string)=>{onSkills(skills.includes(skill)?skills.filter(s=>s!==skill):skills.length<3?[...skills,skill]:skills);};
 const availableSkills=[...new Map(options.flatMap(o=>o.skills??[]).map(s=>[s.name,s])).values()];
 const recommended=recommendRoles(options,skills,directions)[0];
 const choices=(names:typeof availableSkills)=>names.map(skill=><button type="button" key={skill.name} className={`practice-choice${skills.includes(skill.name)?' is-selected':''}`} aria-pressed={skills.includes(skill.name)} disabled={skills.length===3&&!skills.includes(skill.name)} onClick={()=>toggle(skill.name)}><SkillArt name={skill.name}/><span className="practice-choice-copy"><strong>{skill.name}</strong><small>{skill.description}</small></span><span className="practice-check" aria-hidden="true">{skills.includes(skill.name)&&<Icon name="check"/>}</span></button>);
 return <section className="hero-studio" ref={root}>
  <ScreenHeading title={panel==='skills'?'Твои навыки':'Твой персонаж'} back={()=>{if(!busy){if(panel==='look'&&!startWithHero)setPanel('skills');else onBack();}}}/>
  <fieldset disabled={busy}>
   {!startWithHero&&<><div className="studio-step" aria-live="polite"><span>Шаг {panel==='skills'?1:2} из 2</span><span>{worldTitle}</span></div><div className="studio-step-track" aria-hidden="true"><i className="is-done"/><i className={panel==='look'?'is-done':''}/></div></>}
   {panel==='look'&&<>
   <div className="studio-stage">
    <div className="studio-portrait" tabIndex={0} aria-label={`${name}. Выбор персонажа стрелками влево и вправо`} onKeyDown={e=>{if(e.key==='ArrowLeft'||e.key==='ArrowRight'){e.preventDefault();turn(e.key==='ArrowLeft'?-1:1);}}}
     onPointerDown={e=>{if(busy||e.button!==0)return;swipe.current={x:e.clientX,y:e.clientY};e.currentTarget.setPointerCapture(e.pointerId);}}
     onPointerUp={e=>{const start=swipe.current;swipe.current=null;if(start&&Math.abs(e.clientX-start.x)>48&&Math.abs(e.clientX-start.x)>Math.abs(e.clientY-start.y))turn(e.clientX<start.x?1:-1);}} onPointerCancel={()=>{swipe.current=null;}}>
     {custom&&removed?<span className="studio-initials">{name.slice(0,2)}</span>:<img key={displayedPortrait} src={displayedPortrait} alt="" draggable={false}/>}
    </div>
   </div>
   <div className="studio-identity"><button disabled={!custom} onClick={()=>setSheet('identity')}><h2>{name}</h2>{custom&&<Icon name="compose"/>}</button>{!custom&&options[index].role!==name&&<p>{options[index].role}</p>}</div>
   <div className="studio-roster" aria-label="Персонажи">{options.map((hero,i)=><button key={hero.id} aria-label={hero.id==='custom'?'Создать своего персонажа':hero.name} aria-pressed={i===index} onClick={()=>onChoose(hero.id)}>{hero.id==='custom'?<Icon name="compose"/>:<img src={heroPortraitSource(hero.id,hero.id,worldId,style)} alt=""/>}<span>{hero.id==='custom'?'Свой персонаж':hero.name}</span></button>)}</div>
   {options[index].motivation&&<p className="studio-motivation" aria-live="polite">{options[index].motivation}</p>}
   <button className="studio-goal" onClick={()=>setSheet('goal')}><span><small>Твоя цель</small><strong>{goal||options[index].goal}</strong></span></button><div className="studio-role-skills" aria-label="Навыки персонажа">{options[index].skills?.map(skill=><span key={skill.name}><SkillArt name={skill.name}/>{skill.name}</span>)}</div>
   </>}
   {panel==='skills'?<div className="studio-panel">
    <div className="studio-panel-heading"><h3>Что хочешь прокачать?</h3><button className="link-button" onClick={()=>setSheet('skills')}>Все навыки</button></div>
    <p className="studio-help">Выбери направления — подберём персонажа.</p>
    <div className="practice-grid">{skillDirections.map(direction=><button type="button" key={direction.name} aria-pressed={directions.includes(direction.name)} className={`practice-choice${directions.includes(direction.name)?' is-selected':''}`} onClick={()=>setDirections(values=>values.includes(direction.name)?values.filter(v=>v!==direction.name):[...values,direction.name])}><SkillArt name={direction.name}/><span className="practice-choice-copy"><strong>{direction.name}</strong></span></button>)}</div>
    {skills.length>0&&<div className="studio-selected-skills">{skills.map(skill=><Chip key={skill} selected onClick={()=>toggle(skill)}>{skill} ×</Chip>)}</div>}
    {(skills.length>0||directions.length>0)&&recommended&&<div className="practice-example" aria-live="polite"><strong>Тебе может подойти {recommended.name}</strong><p>{recommended.skills?.map(s=>s.name).join(' · ')}</p></div>}
    <details className="practice-explainer"><summary>Как это работает<Icon name="chevron"/></summary><p>У каждого персонажа уже есть три навыка, связанные с его историей. Выбор помогает найти героя, а его навыки развиваются через новые поступки в постах и ответах. Можно выбрать любого персонажа.</p></details>
   </div>:custom?<div className="studio-panel">
    <AvatarBuilder worldId={worldId} seed={seed} onLook={onLook} onRender={src=>{onUpload(src);onAppearance(eveningLooks.find(look=>look.id===customLook(seed,worldId).id)?.description);}}/>
    <div className="studio-upload"><Button type="button" variant="secondary" onClick={()=>uploadInput.current?.click()}>Загрузить фото</Button><input ref={uploadInput} className="sr-only" type="file" accept="image/png,image/jpeg,image/webp" onChange={e=>{const file=e.target.files?.[0];if(!file)return;if(!['image/png','image/jpeg','image/webp'].includes(file.type)||file.size>1024*1024){setUploadError('Выбери PNG, JPG или WebP до 1 МБ.');return;}const reader=new FileReader();reader.onload=()=>{onUpload(String(reader.result));onAppearance(undefined);setUploadError('');};reader.onerror=()=>setUploadError('Не удалось прочитать фото.');reader.readAsDataURL(file);e.target.value='';}}/><Button type="button" variant="ghost" onClick={onRemove}>Без фото</Button></div>{uploadError&&<p role="alert">{uploadError}</p>}
    {custom&&<div className="studio-address"><span>Обращение</span><div className="row">{([['she','Она'],['he','Он'],['nick','По нику']] as const).map(([id,label])=><Chip key={id} selected={address===id} onClick={()=>onAddress(id)}>{label}</Chip>)}</div></div>}
   </div>:null}

  </fieldset>
  <div className="studio-footer"><Button busy={busy} disabled={busy||(panel==='look'&&!canEnter)} onClick={()=>{if(panel==='skills'){if(recommended)onChoose(recommended.id);setPanel('look');}else onEnter();}}>{panel==='skills'?'Выбрать персонажа':'Начать историю'}</Button></div>
  {sheet&&<Dialog title={sheet==='identity'?'Имя и канал':sheet==='goal'?'Твоя цель':'Все навыки'} onClose={()=>setSheet(null)}><fieldset className="studio-sheet" disabled={busy}>
   {sheet==='identity'?<><TextField label="Имя персонажа" value={name} maxLength={80} onChange={e=>onName(e.target.value)}/><TextField label="Имя канала" value={channel} maxLength={80} onChange={e=>onChannel(e.target.value)}/></>:sheet==='goal'?<p>{goal||options[index].goal}</p>:<><TextField label="Поиск навыка" value={query} onChange={e=>setQuery(e.target.value)}/><p className="caption muted">Выбрано {skills.length} из 3</p><div className="practice-grid">{choices(availableSkills.filter(s=>s.name.toLocaleLowerCase('ru').includes(query.toLocaleLowerCase('ru'))))}</div></>}
   <Button onClick={()=>setSheet(null)}>Готово</Button>
  </fieldset></Dialog>}
 </section>;
}

import {syncBackButton} from '../../telegram';
import {useEffect, useRef, useState} from 'react';
import {Button, Chip, Dialog, Icon, TextField, ScreenHeading} from '../ui';
import {characterLooks, characterPortrait} from '../portraits';
import {heroPortraitSource, type HeroOption} from './HeroPicker';
import {practiceFor, practiceSuggestions} from '../skillPractice';
import skillCatalog from '../skillCatalog.json';
import {SkillArt} from '../ui/GameArt';
import './heroStudio.css';

export function HeroStudio({options, selected, onChoose, name, channel, goal, onName, onChannel, onGoal, skills, onSkills, seed, avatar, removed, onLook, onUpload, onRemove, address, onAddress, worldId, worldTitle, style, busy, canEnter, onEnter, onBack}: {
 options:HeroOption[]; selected:string|null; onChoose:(id:string)=>void;
 name:string; channel:string; goal:string; onName:(value:string)=>void; onChannel:(value:string)=>void; onGoal:(value:string)=>void;
 skills:string[]; onSkills:(value:string[])=>void; seed:string; avatar?:string; removed:boolean;
 onLook:(seed:string)=>void; onUpload:(src:string)=>void; onRemove:()=>void;
 address:string; onAddress:(value:'she'|'he'|'nick')=>void; worldId:string; worldTitle:string; style:string;
 busy:boolean; canEnter:boolean; onEnter:()=>void; onBack:()=>void;
}) {
 const [panel,setPanel]=useState<'skills'|'look'>('skills');
 useEffect(()=>syncBackButton(!busy,()=>panel==='look'?setPanel('skills'):onBack()),[panel,busy,onBack]);
 const [sheet,setSheet]=useState<'identity'|'goal'|'skills'|null>(null);
 const [query,setQuery]=useState(''),[focused,setFocused]=useState('Смелость'),[uploadError,setUploadError]=useState('');
 const root=useRef<HTMLElement>(null);
 useEffect(()=>{root.current?.closest('.app-body')?.scrollTo({top:0});},[panel]);
 const swipe=useRef<{x:number;y:number}|null>(null);
 const index=Math.max(0,options.findIndex(hero=>hero.id===selected));
 const custom=selected==='custom';
 useEffect(()=>{if(!selected&&options[0])onChoose(options[0].id);},[selected,options[0]?.id]);
 const portrait=heroPortraitSource(selected||options[0].id,seed,worldId,style);
 const turn=(delta:number)=>onChoose(options[(index+delta+options.length)%options.length].id);
 const toggle=(skill:string)=>{setFocused(skill);onSkills(skills.includes(skill)?skills.filter(s=>s!==skill):skills.length<3?[...skills,skill]:skills);};
 const displayed=[...new Set([...Object.keys(practiceSuggestions).slice(0,6),...skills])];
 const choices=(names:string[])=>names.map(skill=><button type="button" key={skill} className={`practice-choice${skills.includes(skill)?' is-selected':''}`} aria-pressed={skills.includes(skill)} onClick={()=>toggle(skill)}><SkillArt name={skill}/><span className="practice-choice-copy"><strong>{skill}</strong><small>{practiceFor(skill).short}</small></span><span className="practice-check" aria-hidden="true">{skills.includes(skill)&&<Icon name="check"/>}</span></button>);
 return <section className="hero-studio" ref={root}>
  <ScreenHeading title={panel==='skills'?'Твои навыки':'Твой персонаж'} back={()=>{if(!busy){if(panel==='look')setPanel('skills');else onBack();}}}/>
  <fieldset disabled={busy}>
   <div className="studio-step" aria-live="polite"><span>Шаг {panel==='skills'?1:2} из 2</span><span>{worldTitle}</span></div><div className="studio-step-track" aria-hidden="true"><i className="is-done"/><i className={panel==='look'?'is-done':''}/></div>
   {panel==='look'&&<>
   <div className="studio-stage">
    <div className="studio-portrait" tabIndex={0} aria-label={`${name}. Выбор персонажа стрелками влево и вправо`} onKeyDown={e=>{if(e.key==='ArrowLeft'||e.key==='ArrowRight'){e.preventDefault();turn(e.key==='ArrowLeft'?-1:1);}}}
     onPointerDown={e=>{if(busy||e.button!==0)return;swipe.current={x:e.clientX,y:e.clientY};e.currentTarget.setPointerCapture(e.pointerId);}}
     onPointerUp={e=>{const start=swipe.current;swipe.current=null;if(start&&Math.abs(e.clientX-start.x)>48&&Math.abs(e.clientX-start.x)>Math.abs(e.clientY-start.y))turn(e.clientX<start.x?1:-1);}} onPointerCancel={()=>{swipe.current=null;}}>
     {removed?<span className="studio-initials">{name.slice(0,2)}</span>:<img key={avatar||portrait} src={avatar||portrait||characterPortrait(seed)} alt="" draggable={false}/>}
    </div>
   </div>
   <div className="studio-identity"><button disabled={!custom} onClick={()=>setSheet('identity')}><h2>{name}</h2>{custom&&<Icon name="compose"/>}</button>{(custom||options[index].role!==name)&&<p>{custom?'Твой персонаж':options[index].role}</p>}</div>
   <div className="studio-roster" aria-label="Персонажи">{options.map((hero,i)=><button key={hero.id} aria-label={hero.id==='custom'?'Создать своего персонажа':hero.name} aria-pressed={i===index} onClick={()=>onChoose(hero.id)}>{hero.id==='custom'?<Icon name="compose"/>:<img src={heroPortraitSource(hero.id,hero.id,worldId,style)} alt=""/>}</button>)}</div>
   <button className="studio-goal" onClick={()=>setSheet('goal')}><span><small>Твоя цель</small><strong>{goal||options[index].goal}</strong></span>{custom&&<Icon name="compose"/>}</button>
   </>}
   {panel==='skills'?<div className="studio-panel">
    <div className="studio-panel-heading"><h3>Что хочешь прокачать?</h3><button className="link-button" onClick={()=>setSheet('skills')}>Все навыки</button></div>
    <p className="studio-help">Выбери три направления для практики в постах и разговорах.</p>
    <div className="practice-grid">{choices(displayed)}</div>
    <div className="practice-example" aria-live="polite"><strong><SkillArt name={focused}/>{focused} · попробуй в игре</strong><p>{practiceFor(focused).action}</p>{skills.length===3&&!skills.includes(focused)&&<p className="practice-limit">Чтобы выбрать этот навык, убери один из отмеченных.</p>}</div>
    <details className="practice-explainer"><summary>Как это работает<Icon name="chevron"/></summary><ol><li>Получи задание в мире.</li><li>Практикуй выбранный приём в посте или ответе.</li><li>Прочитай реакцию персонажей и попробуй следующий подход.</li></ol><p>Выбор задаёт твои цели практики. Игра пока оценивает три общие шкалы: смелость, юмор и «в тему». Их значения обновляются после ответа сервера; отдельная оценка остальных навыков ещё не подключена.</p></details>
   </div>:<div className="studio-panel">
    <h3>Выбери свой образ</h3><div className="studio-looks">{characterLooks.map((look,i)=><button key={look} aria-label={`Образ ${i+1}`} aria-pressed={!removed&&!avatar&&seed===look} onClick={()=>onLook(look)}><img src={characterPortrait(look)} alt=""/></button>)}</div>
    <div className="studio-upload"><label className="button button--secondary">Загрузить фото<input className="sr-only" type="file" accept="image/png,image/jpeg,image/webp" onChange={e=>{const file=e.target.files?.[0];if(!file)return;if(!['image/png','image/jpeg','image/webp'].includes(file.type)||file.size>1024*1024){setUploadError('Выбери PNG, JPG или WebP до 1 МБ.');return;}const reader=new FileReader();reader.onload=()=>{onUpload(String(reader.result));setUploadError('');};reader.onerror=()=>setUploadError('Не удалось прочитать фото.');reader.readAsDataURL(file);e.target.value='';}}/></label><Button variant="ghost" onClick={onRemove}>Без фото</Button></div>{uploadError&&<p role="alert">{uploadError}</p>}
    {custom&&<div className="studio-address"><span>Обращение</span><div className="row">{([['she','Она'],['he','Он'],['nick','По нику']] as const).map(([id,label])=><Chip key={id} selected={address===id} onClick={()=>onAddress(id)}>{label}</Chip>)}</div></div>}
   </div>}

  </fieldset>
  <div className="studio-footer"><Button busy={busy} disabled={busy||skills.length!==3||(panel==='look'&&!canEnter)} onClick={()=>panel==='skills'?setPanel('look'):onEnter()}>{skills.length<3?`Выбери ещё ${3-skills.length} ${skills.length===2?'навык':'навыка'}`:panel==='skills'?'Выбрать персонажа':'Начать историю'}</Button></div>
  {sheet&&<Dialog title={sheet==='identity'?'Имя и канал':sheet==='goal'?'Твоя цель':'Три навыка для практики'} onClose={()=>setSheet(null)}><fieldset className="studio-sheet" disabled={busy}>
   {sheet==='identity'?<><TextField label="Имя персонажа" value={name} maxLength={80} onChange={e=>onName(e.target.value)}/><TextField label="Имя канала" value={channel} maxLength={80} onChange={e=>onChannel(e.target.value)}/></>:sheet==='goal'?custom?<label>Цель в истории<textarea rows={4} maxLength={500} value={goal} onChange={e=>onGoal(e.target.value)}/></label>:<p>{goal||options[index].goal}</p>:<><TextField label="Поиск навыка" value={query} onChange={e=>setQuery(e.target.value)}/><p className="caption muted">Выбрано {skills.length} из 3. Нажми на отмеченный навык, чтобы заменить.</p><div className="practice-grid">{choices(skillCatalog.filter(n=>n.toLocaleLowerCase('ru').includes(query.toLocaleLowerCase('ru'))))}</div></>}
   <Button onClick={()=>setSheet(null)}>Готово</Button>
  </fieldset></Dialog>}
 </section>;
}

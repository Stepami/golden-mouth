import {useEffect,useId,useState} from 'react';
import {syncBackButton} from '../../telegram';
import type {Address,Player,WorldDefinition} from '../../api/types';
import {getWorldDraft,savedWorldDraft,selectDraftOwner,setWorldDraft,type WorldDraft} from '../../app/worldDraft';
import {avatarPersonalization} from '../../app/avatarPersonalization';
import {Button,Chip,Icon,Notice,ScreenHeading,TextField} from '../ui';
import {AvatarGenerator} from './AvatarGenerator';
import {generatedAvatarSource} from '../generatedAvatar';
import './worldEditor.css';

export type WorldEditorPreferences={address:Address;personalizeAvatar:boolean};
function Area({label,value,onChange,maxLength=2000}:{label:string;value:string;onChange:(value:string)=>void;maxLength?:number}){
 const id=useId();
 return <div className="field"><label htmlFor={id}>{label}</label><textarea id={id} value={value} maxLength={maxLength} rows={3} onChange={e=>onChange(e.target.value)}/></div>;
}
const traits=['Дерзкость','Поддержка','Загадка','Воображение','Любопытство','Энергия','Ирония и расчёт','Сухой юмор','Самоирония','Азарт','Внимание'];
const styles=[['lorelei','Портрет'],['notionists','Скетч'],['pixelArt','Пиксели'],['thumbs','Мини'],['shapes','Формы'],['adventurer','Приключения']];

export function WorldEditor({initial,creating=false,editingId,profile,busy,error,onSave,onBack}:{initial:WorldDefinition;creating?:boolean;editingId?:string;profile:Pick<Player,'id'|'address'|'ageBand'>;busy:boolean;error:string;onSave:(value:WorldDefinition,portrait:string|undefined,preferences:WorldEditorPreferences)=>void;onBack:()=>void}){
 const [restored]=useState(()=>{selectDraftOwner(profile.id);const d=creating?savedWorldDraft('custom'):getWorldDraft();return d&&!!d.editingId===!creating&&d.editingId===editingId&&!d.previewOnly?d:null;});
 const [value,setValue]=useState(()=>structuredClone(restored?.definition??initial));
 const [address,setAddress]=useState<Address>(restored?.address??profile.address);
 const [personalize,setPersonalize]=useState(restored?.personalizeAvatar??avatarPersonalization(profile.id));
 const [step,setStep]=useState<NonNullable<WorldDraft['editorStep']>>(restored?.editorStep??(creating?'world':'advanced'));
 const [returnStep,setReturnStep]=useState<NonNullable<WorldDraft['editorStep']>>('world');
 const update=(patch:Partial<WorldDefinition>)=>setValue(old=>({...old,...patch}));
 const star=(id:string,patch:Partial<WorldDefinition['stars'][number]>)=>update({stars:value.stars.map(s=>s.id===id?{...s,...patch}:s)});
 const ready=!!(value.name.trim()&&value.avatar.trim()&&value.role.trim()&&value.title.trim()&&value.description.trim()&&value.voice.trim()&&value.stars.length&&value.stars.every(s=>s.name.trim()&&s.role.trim()&&s.about.trim()&&s.voice.trim()));
 const leave=()=>{if(busy)return;if(step==='advanced'&&creating)setStep(returnStep);else if(step==='mood')setStep('person');else if(step==='person')setStep('world');else onBack();};
 useEffect(()=>syncBackButton(!busy,leave),[busy,step,returnStep,onBack]);
 useEffect(()=>{setWorldDraft({definition:value,editingId,advanced:step==='advanced',editorStep:step,address,group:profile.ageBand==='14-17'?'14-17':'18+',ageBand:profile.ageBand,personalizeAvatar:personalize});},[value,address,personalize,step,editingId,profile.ageBand]);
 useEffect(()=>{document.querySelector('.world-editor')?.closest('.app-body')?.scrollTo({top:0});},[step]);
 const advanced=step==='advanced';
 const avatarProfile={address,ageBand:profile.ageBand,personalizeAvatar:personalize};
 return <><ScreenHeading title={creating?'Создать свой мир':'Настройки мира'} back={leave}/>
 <form className="section stack world-editor" onSubmit={e=>{e.preventDefault();if(!ready||busy||!['mood','advanced'].includes(step))return;onSave({...value,roles:value.roles.map(s=>s.trim()).filter(Boolean),voices:value.voices.map(s=>s.trim()).filter(Boolean),postIdeas:value.postIdeas.map(s=>s.trim()).filter(Boolean),demoPosts:value.demoPosts.map(s=>s.trim()).filter(Boolean),rules:value.rules.filter(r=>r.locked||r.text.trim())},creating?generatedAvatarSource(value.avatar,value.avatarStyle,avatarProfile):undefined,{address,personalizeAvatar:personalize});}}>
 <p className="caption muted">{creating?'Придумай мир и выбери, кем в нём быть.':'Изменения будут действовать на следующие посты и ответы. Публикации, подписчики и отношения сохранятся.'}</p>
 <p className="caption muted">Черновик сохраняется в этой вкладке.</p>
 {creating&&!advanced&&<p className="caption">Шаг {step==='world'?1:step==='person'?2:3} из 3 · {step==='world'?'Твой мир':step==='person'?'Твой персонаж':'Собеседники и настроение'}</p>}
 <Button type="button" variant="ghost" onClick={()=>{if(advanced)setStep(returnStep);else{setReturnStep(step);setStep('advanced');}}}>{advanced?'Быстрые настройки':'Расширенные настройки'}</Button>
 <fieldset disabled={busy} className="stack">
 {(advanced||step==='world')&&<details open className="world-editor-section"><summary>Мир и начало истории<Icon name="chevron"/></summary><div className="stack">
  <TextField label="Название мира" required maxLength={120} value={value.title} onChange={e=>update({title:e.target.value})}/>
  <Area label="О чём этот мир" value={value.description} onChange={description=>update({description})}/>
  {advanced&&<><TextField label="Место действия" maxLength={200} value={value.where} onChange={e=>update({where:e.target.value})}/>
  <Area label="С чего начинается история" value={value.firstContext} onChange={firstContext=>update({firstContext})}/>
  <Area label="Подсказка для первого поста" value={value.firstPrompt} onChange={firstPrompt=>update({firstPrompt})}/>
  <TextField label="Короткое описание для карточки" maxLength={200} value={value.summary} onChange={e=>update({summary:e.target.value})}/>
  <TextField label="Значок мира" maxLength={8} value={value.emoji} onChange={e=>update({emoji:e.target.value})}/></>}
 </div></details>}
 {(advanced||step==='person')&&<details className="world-editor-section" open><summary>Твой персонаж<Icon name="chevron"/></summary><div className="stack">
  <TextField label="Имя персонажа" required maxLength={80} value={value.name} onChange={e=>update({name:e.target.value})}/>
  <TextField label="Кто ты в этом мире" required maxLength={80} value={value.role} onChange={e=>update({role:e.target.value})}/>
  {creating&&<><div className="stack"><span>Как к тебе обращаться</span><div className="row world-address">{([['she','Она'],['he','Он'],['nick','По нику']] as const).map(([id,label])=><Chip type="button" key={id} selected={address===id} onClick={()=>setAddress(id)}>{label}</Chip>)}</div></div>
  <label className="world-editor-select">Стиль аватаров мира<select value={value.avatarStyle} onChange={e=>update({avatarStyle:e.target.value})}>{styles.map(([id,title])=><option key={id} value={id}>{title}</option>)}</select></label>
  <AvatarGenerator seed={value.avatar} style={value.avatarStyle} profile={avatarProfile} onSeed={avatar=>update({avatar})}/>
  <label className="world-personalization"><input type="checkbox" checked={personalize} onChange={e=>setPersonalize(e.target.checked)}/>Учитывать обращение и возраст в аватаре</label></>}
 </div></details>}
 {(advanced||step==='mood')&&<><details open className="world-editor-section"><summary>Сложность и стиль<Icon name="chevron"/></summary><div className="stack">
  <label className="world-editor-select">Сложность<select value={value.difficulty} onChange={e=>update({difficulty:e.target.value as WorldDefinition['difficulty']})}><option value="easy">Легко</option><option value="normal">Средне</option><option value="hard">Сложно</option></select></label>
  <label className="world-editor-select">Драма<select value={value.drama} onChange={e=>update({drama:e.target.value as WorldDefinition['drama']})}><option value="soft">Мягко</option><option value="mid">Средне</option><option value="hot">Остро</option><option value="off">Без хейта</option></select></label>
  <TextField label="Как пишет твой персонаж" maxLength={200} value={value.voice} onChange={e=>update({voice:e.target.value})}/>
 </div></details>
 <details open className="world-editor-section"><summary>Собеседники · {value.stars.length}<Icon name="chevron"/></summary><div className="stack">
  <p className="caption muted">Персонаж, за которого ты играешь, сюда не входит.</p>
  {value.stars.map((s,index)=><details key={s.id} className="world-editor-character" open={creating||undefined}><summary>{s.name||`Собеседник ${index+1}`}<Icon name="chevron"/></summary><div className="stack">
   <TextField label="Имя собеседника" maxLength={80} required value={s.name} onChange={e=>star(s.id,{name:e.target.value})}/>
   <TextField label="Чем занимается" maxLength={80} required value={s.role} onChange={e=>star(s.id,{role:e.target.value})}/>
   <Area label="Характер и история" value={s.about} onChange={about=>star(s.id,{about})}/>
   <Area label="Как общается" value={s.voice} maxLength={200} onChange={voice=>star(s.id,{voice})}/>
   <label className="world-editor-select">Главная черта<select value={s.trait} onChange={e=>star(s.id,{trait:Number(e.target.value)})}>{traits.map((t,i)=><option key={t} value={i}>{t}</option>)}</select></label>
   <AvatarGenerator seed={s.avatarSeed} style={value.avatarStyle} onSeed={avatarSeed=>star(s.id,{avatarSeed})}/>
   <Button type="button" variant="ghost" disabled={value.stars.length===1} onClick={()=>update({stars:value.stars.filter(v=>v.id!==s.id)})}>Убрать собеседника</Button>
  </div></details>)}
  <Button type="button" variant="secondary" onClick={()=>update({stars:[...value.stars,{id:crypto.randomUUID().replaceAll('-',''),name:'',role:'',about:'',voice:'Живо и по делу',avatarSeed:`generated-${crypto.randomUUID()}`,trait:1,relation:0}]})}>Добавить собеседника</Button>
 </div></details></>}
 {advanced&&<><details className="world-editor-section"><summary>Правила мира<Icon name="chevron"/></summary><div className="stack">
  {value.rules.map((r,i)=>r.locked?<p key={i} className="caption muted">{r.text}</p>:<div className="stack" key={i}><Area label={`Правило ${i+1}`} value={r.text} onChange={text=>update({rules:value.rules.map((v,j)=>j===i?{...v,text}:v)})}/><Button type="button" variant="ghost" onClick={()=>update({rules:value.rules.filter((_,j)=>j!==i)})}>Убрать правило</Button></div>)}
  <Button type="button" variant="secondary" onClick={()=>update({rules:[...value.rules,{text:'',locked:false}]})}>Добавить правило</Button>
 </div></details>
 <details className="world-editor-section"><summary>Подсказки и варианты<Icon name="chevron"/></summary><div className="stack">
  <p className="caption muted">Каждый вариант — с новой строки.</p>
  <Area label="Идеи для постов" maxLength={12000} value={value.postIdeas.join('\n')} onChange={text=>update({postIdeas:text.split('\n')})}/>
  <Area label="Примеры постов" maxLength={12000} value={value.demoPosts.join('\n')} onChange={text=>update({demoPosts:text.split('\n')})}/>
  <Area label="Варианты роли своего персонажа" value={value.roles.join('\n')} onChange={text=>update({roles:text.split('\n')})}/>
  <Area label="Варианты стиля" value={value.voices.join('\n')} onChange={text=>update({voices:text.split('\n')})}/>
 </div></details></>}
 </fieldset>
 {error&&<Notice title="Не удалось сохранить мир" tone="error">{error}</Notice>}
 {step==='world'?<Button type="button" disabled={!value.title.trim()||!value.description.trim()} onClick={()=>setStep('person')}>Мой персонаж</Button>:step==='person'?<Button type="button" disabled={!value.name.trim()||!value.role.trim()} onClick={()=>setStep('mood')}>Собеседники и драма</Button>:<>{!ready&&<p className="caption muted">Заполни название и описание мира, имя и роль своего персонажа, имена, роли и описания собеседников.</p>}<Button type="submit" busy={busy} disabled={!ready||busy}>{creating?'Начать историю':'Сохранить изменения'}</Button></>}
 </form></>;
}

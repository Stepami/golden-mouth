import {useState} from 'react';
import {communicationInterests} from '../interests';
import {Button, Icon, ScreenHeading} from '../ui';
import {characterPortrait} from '../portraits';
import './onboarding.css';

export function Onboarding({onDone,onBack,initialQualities=[],initialStep=0,onTry,standalone=false}:{
 onDone:(qualities:string[])=>void;onBack:()=>void;initialQualities?:string[];initialStep?:0|1;onTry?:()=>void;standalone?:boolean;
}) {
 const [step,setStep]=useState<0|1>(initialStep);
 const [selected,setSelected]=useState<string[]>(initialQualities);
 return <section className={`entry-onboarding ${step===0?'entry-welcome':'entry-preferences'}`}>
  {step===0?<>
   <header className="entry-brand"><span className="brand-small">реки®</span><span>Игра в общение</span></header>
   <div className="entry-scene" aria-hidden="true">
    <img className="entry-scene-portrait" src={characterPortrait('muse')} alt=""/>
    <div className="entry-message"><img src={characterPortrait('scout')} alt=""/><span>А ты как думаешь?</span></div>
    <div className="entry-message entry-message--reply">У меня есть идея.</div>
   </div>
   <div className="entry-copy"><h1>Твоя история.<br/>Твой голос.</h1><p>Выбери мир и героя. Пиши посты, пробуй новые подходы и смотри, как отвечают персонажи.</p></div>
   <footer className="entry-actions"><Button className="full" onClick={()=>onTry?onTry():setStep(1)}>Начать историю</Button><p>Твои собеседники — AI-персонажи.</p></footer>
  </>:<>
   <ScreenHeading title="Твои интересы" back={()=>initialStep===1?onBack():setStep(0)}/>
   {!standalone&&<div className="entry-step-track" aria-label="Шаг 2 из 2"><i className="is-done"/><i className="is-done"/></div>}
   <div className="entry-copy"><h1>Что хочется<br/>попробовать?</h1><p>Подберём миры под твои интересы. Можно выбрать несколько.</p></div>
   <div className="entry-interest-list">{communicationInterests.map(interest=><button key={interest} aria-pressed={selected.includes(interest)} onClick={()=>setSelected(values=>values.includes(interest)?values.filter(v=>v!==interest):[...values,interest])}><span>{interest}</span><span className="entry-choice-mark" aria-hidden="true">{selected.includes(interest)&&<Icon name="check"/>}</span></button>)}</div>
   <footer className="entry-actions"><Button className="full" onClick={()=>onDone(selected)}>Найти мой мир</Button><button className="entry-skip" onClick={()=>onDone([])}>Показать все миры</button></footer>
  </>}
 </section>;
}

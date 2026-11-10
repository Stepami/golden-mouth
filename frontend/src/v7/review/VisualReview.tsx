import {ResultIsland} from '../publication/ResultIsland';
import {GameArt,type ArtKind} from '../ui/GameArt';
import {WorldProgress, StreakDetails} from '../screens/WorldResources';
import {useScrollEdges} from '../ui/useScrollEdges';
import {useEffect, useState, useRef, type ReactNode} from 'react';
import {Button, Chip, EmptyState, LoadingIndicator, Notice, ScreenHeading} from '../ui';
import {PublicationResult, PublicationWaiting, type PublicationReceipt} from '../publication/PublicationCards';
import {WalletView} from '../energy/WalletView';
import {ReactionTicker} from '../social/ReactionTicker';
import type {EnergyWallet} from '../energy/exchange';
import '../social/social.css';
import '../screens/resourcePages.css';
import './visualReview.css';

// Presentation fixtures only. Used by the development gallery and design.html.
const wallet:EnergyWallet={amount:4,cap:10,nextEnergyAtUtc:null,regenSeconds:600,crystals:7,exchangeQuote:{targetEnergy:10,crystalCost:3,available:true,unavailableReason:null}};
const receipt:PublicationReceipt={worldId:'visual-example',before:{brave:{level:1,progress:0},humor:{level:1,progress:0},topic:{level:1,progress:0}},result:{postId:'visual-example',delta:3,headline:'Запись сохранена',whys:[],ribbons:[],viral:false,flop:false,views:12}};
const sample='Предлагаю сравнить письма вместе. Какие подписи вы заметили и что они меняют для нашего разговора?';

function Frame({id,title,note,children}:{id:string;title:string;note:string;children:ReactNode}){
 return <section className="visual-review-case" id={id}><header><span>{id}</span><h2>{title}</h2><p>{note}</p></header><div className="v7-stage"><div className="app"><div className="app-body">{children}</div></div></div></section>;
}
export default function VisualReview(){
 const root=useRef<HTMLElement>(null);useScrollEdges(root);
 const [island,setIsland]=useState(false);
 const [message,setMessage]=useState(''),[read,setRead]=useState(false),[large,setLarge]=useState(false);
 useEffect(()=>{document.documentElement.style.setProperty('--text-scale',large?'2':'1');document.documentElement.dataset.largeText=String(large);return()=>{document.documentElement.style.removeProperty('--text-scale');delete document.documentElement.dataset.largeText;};},[large]);
 const explain=()=>setMessage('В приложении эта кнопка продолжает сценарий. Здесь показан только внешний вид состояния.');
 return <main ref={root} className={`visual-review${large?' visual-review--large':''}`}>
  <header className="visual-review-header"><div><p>Реки · следующая итерация</p><h1>Состояния для обсуждения</h1><p>Настоящие компоненты интерфейса, примерные данные. Действия в этой галерее не меняют аккаунт.</p></div><nav><a href={location.pathname.endsWith("design.html")?"design.html":"/"}>Открыть приложение</a><Chip selected={large} onClick={()=>setLarge(v=>!v)}>Крупный текст</Chip></nav></header>
  <p className="visual-review-feedback" role="status">{message||'Выбери состояние и проверь, как читаются сообщение, контекст и действие.'}</p>
  <div className="visual-review-grid"><Frame id="13" title="Игровые значки" note="Простые формы, глубокий цвет, мягкий глянец. Один материал для ресурсов и семейств навыков."><div className="art-review">{Object.entries({energy:'Энергия',crystal:'Кристаллы',flame:'Серия',followers:'Аудитория',gift:'Подарок',booster:'Усиление',courage:'Смелость',humor:'Юмор',context:'В тему',diplomacy:'Дипломатия',empathy:'Эмпатия',argument:'Аргументация',precision:'Точность',knowledge:'Знания',voice:'Свой голос',direction:'Направление'}).map(([kind,label])=><div key={kind}><GameArt kind={kind as ArtKind}/><span>{label}</span></div>)}</div></Frame><Frame id="12" title="Уведомление о результате" note="Исчезает через 6 секунд; наведение и фокус приостанавливают таймер."><ScreenHeading title="Лента"/><section className="section"><Button onClick={()=>setIsland(true)}>Показать результат</Button></section>{island&&<ResultIsland receipt={receipt} onRead={()=>setMessage("Открываем отклики к записи")} onClose={()=>setIsland(false)}/>}</Frame>
   <Frame id="01" title="Ждём ответ" note="Текст можно раскрыть, ожидание можно остановить."><ScreenHeading title="Лента"/><PublicationWaiting pending unknown={false} status="ready_for_moderation" text={sample} onStop={explain} onContinue={explain}/></Frame>
   <Frame id="02" title="Ответ не получен" note="Проверка статуса использует прежнюю отправку."><ScreenHeading title="Лента"/><PublicationWaiting pending={false} unknown status="unknown" text={sample} onStop={explain} onContinue={explain}/></Frame>
   <Frame id="03" title="Черновик сохранён" note="Один ясный способ вернуться к публикации."><ScreenHeading title="Лента"/><PublicationWaiting pending={false} unknown={false} status="failed" text={sample} onStop={explain} onContinue={explain}/></Frame>
   <Frame id="04" title="Запись опубликована" note="Подтверждённая публикация и реакция аудитории. Подробности остаются только внутри обсуждения."><ScreenHeading title="Обсуждение"/><PublicationResult receipt={receipt} onRead={explain}/></Frame>
   <Frame id="05" title="Список уведомлений" note="Строки на общем фоне, перенос текста и отметка прочтения."><ScreenHeading title="Уведомления"/>{['Гермиона','Рон'].map((name,i)=><ReactionTicker key={name} inline read={read||i===1} notice={{id:String(i),postId:'visual-example',worldId:'visual-example',kind:i?'mention':'reaction',actorName:name,text:i?'В обсуждении появился вопрос к твоему каналу. Можно вернуться к нему, когда будет удобно.':sample,relation:i?0:1}} onOpen={explain} onClose={()=>setRead(true)}/>)}</Frame>
   <Frame id="06" title="Обмен доступен" note="Баланс и действие следуют друг за другом."><ScreenHeading title="Энергия"/><section className="section resource-page"><WalletView wallet={wallet} busy={false} message="" onExchange={explain} onRefresh={explain}/></section></Frame>
   <Frame id="10" title="Кристаллы" note="Отдельная вкладка: баланс и обмен, без восстановления энергии."><ScreenHeading title="Кристаллы"/><section className="section resource-page"><WalletView page="crystals" wallet={wallet} busy={false} message="" onExchange={explain} onRefresh={explain}/></section></Frame>
   <Frame id="07" title="Запас полный" note="Доступность обмена задаёт сервер."><ScreenHeading title="Энергия"/><section className="section resource-page"><WalletView wallet={{...wallet,amount:10,exchangeQuote:{...wallet.exchangeQuote,available:false,unavailableReason:'energy_full'}}} busy={false} message="" onExchange={explain} onRefresh={explain}/></section></Frame>
   <Frame id="08" title="Ошибка загрузки" note="Ошибка не выглядит успешным результатом."><ScreenHeading title="Лента"/><section className="section stack"><Notice title="Не удалось обновить ленту" tone="error">Проверь соединение и попробуй ещё раз.</Notice><Button onClick={explain}>Повторить</Button></section></Frame>
   <Frame id="09" title="Пустой список" note="Короткое пустое состояние без лишних действий."><ScreenHeading title="Уведомления"/><EmptyState title="Пока тихо">Здесь появятся реакции персонажей и упоминания твоего канала.</EmptyState></Frame>
   <Frame id="11" title="Индикаторы и серия" note="Один размер индикатора в кнопке и строке. Серия и отметки без emoji."><ScreenHeading title="Общие элементы"/><WorldProgress streak={{days:1,nextReward:'',week:[true,false,false,false,false,false,false]}} onStreak={explain}/><section className="section stack"><StreakDetails streak={{days:1,nextReward:'',week:[true,false,false,false,false,false,false]}}/><Button busy>Сохраняем</Button><div className="pending-line" role="status"><LoadingIndicator/>Загружаем обсуждение</div></section></Frame>
  </div>
 </main>;
}

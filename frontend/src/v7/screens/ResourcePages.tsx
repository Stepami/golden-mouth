import { Button, Chip, ScreenHeading } from '../ui';
import { DemoInventory } from '../storyplay/StorySurfaces';
import type { StoryPlay } from '../storyplay/model';
import './resourcePages.css';
export type ResourcePageName = 'resources'|'crystals'|'boosters';
export function ResourcePages({page,energy,state,activeStory,selectedBooster,onPage,onBack,onEnable,onDisable,onGrant,onBuy,onToggle,onChooseWorld}: {
 page:ResourcePageName;energy?:number;state:StoryPlay;activeStory:boolean;selectedBooster:boolean;
 onPage:(page:ResourcePageName)=>void;onBack:()=>void;onEnable:()=>void;onDisable:()=>void;onGrant:()=>void;onBuy:()=>void;onToggle:()=>void;onChooseWorld:()=>void;
}) {
 const title={resources:'Энергия',crystals:'Кристаллы',boosters:'Бустеры'}[page];
 const demo=activeStory&&state.enabled;
 return <><ScreenHeading title={title} back={onBack}/><section className="section resource-page">
  <nav className="resource-tabs" aria-label="Ресурсы">{([['resources','Энергия'],['crystals','Кристаллы'],['boosters','Бустеры']] as const).map(([id,label])=><Chip key={id} selected={page===id} onClick={()=>onPage(id)}>{label}</Chip>)}</nav>
  {page==='resources'?<>
   <div className="resource-balance"><span className="resource-symbol resource-symbol--energy" aria-hidden="true">ϟ</span><div><strong>{energy??'—'}</strong><span>энергии доступно</span></div></div>
   <h2>{energy===0?'Энергия закончилась':'Для твоих следующих ходов'}</h2><p>Публикация и ответ расходуют по 1 энергии. Запас общий для всех миров; чтение ленты бесплатно.</p>
   <div className="resource-explanation"><strong>Обновление в 09:00 МСК</strong><p>Черновик сохранён. Можно вернуться к нему позже.</p></div>
   <Button variant="secondary" onClick={()=>onPage('crystals')}>Кристаллы и обмен</Button>
  </>:page==='crystals'?<>
   <div className="resource-balance"><span className="resource-symbol resource-symbol--crystal" aria-hidden="true">◇</span><div><strong>{demo?state.crystals:'—'}</strong><span>{demo?'кристаллов в пробе':'баланс не подключён'}</span></div></div>
   {demo&&<p className="local-story-notice">Локальная проба. Тестовые кристаллы общие для аккаунта.</p>}
   <h2>На что обменять</h2><article className="resource-product"><div><h3>Удвоение XP за квест</h3><p>Бустер на одно действие</p></div><strong>8 ◇</strong><Button disabled={!demo||state.crystals<8} onClick={onBuy}>Обменять на бустер</Button><button className="link-button" onClick={()=>onPage('boosters')}>Посмотреть инвентарь</button></article>
   <article className="resource-product"><div><h3>Энергия</h3><p>Обмен кристаллов на настоящую энергию пока не подключён.</p></div><Button disabled>Обмен недоступен</Button></article>
   {demo&&!state.bonusUsed&&<Button variant="ghost" onClick={onGrant}>Добавить 24 тестовых кристалла</Button>}
   {!demo&&<Button variant="secondary" onClick={()=>onPage('boosters')}>Открыть бустеры и пробу</Button>}
  </>:activeStory?<DemoInventory state={state} onEnable={onEnable} onDisable={onDisable} onGrant={onGrant} onBuy={onBuy} selected={selectedBooster} onSelect={()=>{onToggle();onBack();}}/>:<>
   <h2>Больше XP за квест</h2><article className="resource-product"><div><h3>Бустер ×2 XP</h3><p>Применяется к сюжетному квесту перед публикацией.</p></div><strong>8 ◇</strong></article>
   <p className="caption muted">Серверный инвентарь пока не подключён. Локальную пробу покупки и применения можно открыть в сюжетном мире.</p><Button onClick={onChooseWorld}>Выбрать сюжетный мир</Button>
  </>}
 </section></>;
}

import {useScrollEdges} from './ui/useScrollEdges';
import {useEnergyWallet} from './energy/useEnergyWallet';
import {EnergyBadge} from './energy/WalletView';
import {WorldName} from './ui/WorldName';
import {accountIdentity} from './accountIdentity';
import {roleForWorld,scenarioForTemplate,definitionForRole} from './story/roles';
import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import { activitiesForWorld, markWorldActivitiesRead, type Activity } from './activity';
import { submissionFor, publicationError } from './submission';
import { screenView, setExperiments, tap, event as trackEvent } from '../app/analytics';
import { setScreen as setLogScreen } from '../app/log';
import { api, ApiError } from '../api/client';
import type { Player, WorldDocument, WorldDefinition, Post as ServerPost, FeedItem, PostResult, Address } from '../api/types';
import { worlds, worldDefinitionOf, worldDocumentOf, installWorldContent, optionalWorldTemplate } from '../api/worldContent';
import { ageBand as ageBandOf } from '../app/ageBand';
import { AgeEntry } from './screens/AgeEntry';
import { rankCatalog } from './interests';
import { worldArt } from '../app/worldArt';
import { storyForTemplate, storyForCampaign, boundCampaign, type PilotBinding, type PilotCampaign } from './story/pilot';
import { StoryOpening, StoryMap, PlayMode, StoryWallet, XPFlight } from './storyplay/StorySurfaces';
import { freshStory, isStoryPlay, repairClosedDays, finishQuest, freezeStoryDay, buyBooster, grantDemoCrystals, spendStoryXP, type StoryPlay } from './storyplay/model';
import './storyplay/storyplay.css';
import './portraits.css';
import './social/social.css';
import {ChannelAudience,formatAudience} from './social/ChannelAudience';
import {ReactionTicker,type ReactionNotice} from './social/ReactionTicker';
import {ResultReview} from './publication/ResultReview';
import {parseQuestOutcome} from './publication/outcomes';
import {FollowersFlight} from './publication/RewardMotion';
import { ResourcePages, type ResourcePageName } from './screens/ResourcePages';
import { OwnedBoosterPicker } from './storyplay/BoosterPicker';
import { initialInventory, stockOf, type BoosterId, type Inventory } from './storyplay/boosters';
import {SeriesGiftCard} from './storyplay/SeriesGiftCard';
import {claimSeriesGift,seriesGiftFor,moscowDay} from './storyplay/gifts';
import {legacyCurriculum} from './storyplay/curriculum';
import { starterFor } from './storyplay/starters';
import { StoryBrief } from './story/StoryBrief';
import {RewardArt} from './ui/RewardArt';
import {ResultIsland} from './publication/ResultIsland';
import { PublicationWaiting, PublicationResult, type PublicationReceipt } from './publication/PublicationCards';
import {formatXP} from './xp/wallet';
import { heroPortrait } from './screens/HeroPicker';
import {HeroStudio} from './screens/HeroStudio';
import {WorldCarousel} from './screens/WorldCarousel';
import {PracticeFocus} from './screens/PracticeFocus';
import {CurrentSkills} from './screens/CurrentSkills';
import {AccountDeletion} from './screens/AccountDeletion';
import {ReportComment} from './screens/ReportComment';
import {receiptForRelease} from './releasePolicy';
import {deleteConfirmedAccount} from './accountDeletion';
import { personaFor, savePersona, clearPersonas } from './persona';
import { WorldProgress, StreakDetails } from './screens/WorldResources';
import { blankWorld } from '../app/worldDraft';
import { pendingReferral, clearReferral } from '../app/referral';
import { draftKey, readPostDraft, savePostDraft, completePostDraft } from '../app/postDraft';
import { syncBackButton } from '../telegram';
import { worldView, playerView, feedView, detailView, errorText, readLocal, saveLocal } from './backend';
import { worlds as plannedWorlds } from './domain/worlds';
import { catalogEntries, filterCatalog, withHogwartsCard } from './catalog';
import type { World, Person, Post, Approach } from './domain/types';
import { Onboarding } from './screens/Onboarding';
import { WorldIntro } from './screens/WorldIntro';
import { WorldBrief } from './screens/WorldBrief';
import { GoalCard, TaskCard } from './ui/GameBrief';
import { DesignSystem } from './screens/DesignSystem';
import { NominalSignOut, LoadingIndicator, Avatar, Badge, BottomNav, Button, Chip, Dialog, EmptyState, Icon, Notice, PersonRow, PostCard, ProfileHeader, Progress, QuoteBlock, ResourceBadge, ResumeWorld, ScreenHeading, SettingsRow, TextField, WorldCard } from './ui';

type Screen = 'welcome' | 'age' | 'catalog' | 'search' | 'intro' | 'onboarding' | 'hero' | 'story-skills' | 'feed' | 'goals' | 'compose' | 'thread' | 'topic' | 'profile' | 'character' | 'messages' | 'activity' | 'account' | ResourcePageName;
type ReplyTarget = { postId: string; commentId: string | null; name: string; text: string; authorId:string };
type ReplyDraft = { text: string; requestId?: string; submittedText?: string; beforeSubscribers?:number; beforeSkills?:Player['skills']; roleBefore?:Player['roleSkills']; questId?:string; additionalStarId?:string };
const statusLabels: Record<string, string> = { pending: 'Персонажи читают публикацию…', ready_for_moderation: 'Готовим результат…', completed: 'Публикация готова', failed: 'Публикация не отправлена' };
const titleOf: Record<Screen, string> = { catalog: 'Миры', search: 'Поиск', intro: 'Вход в мир', onboarding: 'Знакомство', welcome: 'Знакомство', age: 'Возраст', hero: 'Ваш персонаж', 'story-skills': 'Навыки персонажа', feed: 'Лента', goals: 'Цели', compose: 'Новая публикация', thread: 'Обсуждение', topic: 'Тема', profile: 'Профиль персонажа', character: 'Собеседник', messages: 'Личка', activity: 'Уведомления', account: 'Профиль', resources:'Энергия',crystals:'Кристаллы',boosters:'Бустеры' };

const walletClient={read:()=>api.energyWallet(),exchange:()=>api.exchangeEnergy()};

export default function BackendApp() {
  // Keep the accepted release UI separate from historical story experiments.
  const preview = import.meta.env.DEV && new URLSearchParams(location.search).has('story-preview');
  const [deleteOpen,setDeleteOpen]=useState(false);
  const [reportId,setReportId]=useState<string>();
  const [hash, setHash] = useState(location.hash);
  useEffect(() => { const change = () => setHash(location.hash); window.addEventListener('hashchange', change); return () => window.removeEventListener('hashchange', change); }, []);
  const [screen, setScreen] = useState<Screen>('catalog');
  const [stack, setStack] = useState<Screen[]>([]);
  const [templates, setTemplates] = useState<World[]>([]);
  const [owned, setOwned] = useState<WorldDocument[]>([]);
  const [selected, setSelected] = useState<World>();
  const [selectedVariant, setSelectedVariant] = useState(false);
  const [introStep, setIntroStep] = useState(0);
  const [player, setPlayer] = useState<Player>();
  const [world, setWorld] = useState<World>();
  const accountEnergy=useEnergyWallet(player?.id,walletClient,w=>setPlayer(p=>p?{...p,postsLeft:w.amount,postsCap:w.cap}:p));
  const [booting, setBooting] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [feed, setFeed] = useState<FeedItem[]>([]);
  const [cursor, setCursor] = useState<string | null>(null);
  const [feedLoading, setFeedLoading] = useState(false);
  const [feedError, setFeedError] = useState('');
  const [post, setPost] = useState<ServerPost>();
  const [threadLoading, setThreadLoading] = useState(false);
  const [person, setPerson] = useState<Person>();
  const [overlay, setOverlay] = useState<'exit' | 'streak' | 'map' | 'skills' | 'text-size' | 'local-like' | 'quote' | 'about' | null>(null);
  const [query, setQuery] = useState('');
  const [genre, setGenre] = useState('Все');
  const [qualities, setQualities] = useState<string[]>([]);
  const [textScale, setTextScale] = useState(() => readLocal('reki-text-scale', 1));
  const [age, setAge] = useState('');
  const [entryAge, setEntryAge] = useState(18);
  const [address, setAddress] = useState<Address>('nick');
  const [definition, setDefinition] = useState<WorldDefinition>();
  const [heroBase, setHeroBase] = useState<WorldDefinition>();
  const [heroChoice, setHeroChoice] = useState<string | null>(null);
  const [heroGoal, setHeroGoal] = useState('');
  const [heroSkills, setHeroSkills] = useState<string[]>([]);
  const [heroSeed, setHeroSeed] = useState('');
  const [heroAvatar, setHeroAvatar] = useState<string>();
  const [avatarRemoved, setAvatarRemoved] = useState(false);
  const [pilotSelected, setPilotSelected] = useState(false);
  const [activeCampaign, setActiveCampaign] = useState<PilotCampaign>();
  const [draft, setDraft] = useState('');
  const [reply, setReply] = useState<ReplyTarget>();
  const [quote, setQuote] = useState<{ name: string; text: string }>();
  const [approach, setApproach] = useState<Approach>('ask');
  const [pending, setPending] = useState(false);
  const [status, setStatus] = useState('');
  const [unknown, setUnknown] = useState(false);
  const [activities, setActivities] = useState<Activity[]>([]);
  const [likes, setLikes] = useState<string[]>([]);
  const [storageError, setStorageError] = useState(false);
  const [toast, setToast] = useState<PostResult>();
  const [reactionNotice,setReactionNotice]=useState<ReactionNotice>();
  const [gainAnimation,setGainAnimation]=useState<{delta:number;nonce:string}>();
  const [accountVisible,setAccountVisible]=useState(5);
  const [giftReveal,setGiftReveal]=useState('');
  const [giftDay,setGiftDay]=useState(()=>moscowDay());
  useEffect(()=>{const timer=setInterval(()=>setGiftDay(moscowDay()),30000);return()=>clearInterval(timer);},[]);
  useEffect(()=>{if(!giftReveal)return;const t=setTimeout(()=>setGiftReveal(''),3200);return()=>clearTimeout(t);},[giftReveal]);
  const [receipt, setReceipt] = useState<PublicationReceipt>();
  const [showReceipt,setShowReceipt]=useState(false);
  const [publicationFailed, setPublicationFailed] = useState(false);
  const [prepared, setPrepared] = useState<{ title: string; prompt: string; questId?: string; xp?: number }>();
  const [playMode, setPlayMode] = useState<'story'|'sandbox'>('story');
  const [storyOnboarding,setStoryOnboarding] = useState(false);
  const [selectedBooster, setSelectedBooster] = useState<BoosterId>();
  const [energyNudge,setEnergyNudge] = useState(0);
  const [storyState, setStoryState] = useState<StoryPlay>(() => freshStory(''));
  const [storyOwner,setStoryOwner] = useState('');
  const [storyInventory,setStoryInventory] = useState<Inventory>(()=>initialInventory());
  const [rewardAnimation, setRewardAnimation] = useState<{amount:number;nonce:string}>();
  const cancel = useRef<(() => void) | undefined>(undefined);
  const sending = useRef(false);
  const main = useRef<HTMLDivElement>(null);
  const appRoot = useRef<HTMLDivElement>(null);
  useScrollEdges(appRoot);
  const focusHeading = useRef<HTMLDivElement>(null);
  const feedRequest = useRef(0);
  const threadRequest = useRef(0);
  const interestOnly = useRef(false);
  useEffect(() => { if (player?.experiments) setExperiments(player.experiments); }, [player?.experiments]);
  const key = `reki:v7:${player?.id ?? api.playerId() ?? 'guest'}`;
  const localSave = (name: string, value: unknown) => { if (!saveLocal(`${key}:${name}`, value)) setStorageError(true); };
  const go = (next: Screen) => { setError(''); setStack(s => [...s, screen]); setScreen(next); setToast(undefined); setReactionNotice(undefined); };
  const openResource = (next: ResourcePageName) => {
    void accountEnergy.refresh();
    if(['resources','crystals','boosters'].includes(screen)){setScreen(next);setError('');}else go(next);
  };
  const back = useCallback(() => { if (pending || busy) return; if (screen === 'intro' && introStep > 0) { setIntroStep(introStep - 1); return; } setError(''); setScreen(stack.at(-1) ?? (world ? 'feed' : 'catalog')); setStack(s => s.slice(0, -1)); }, [stack, world, pending, busy, screen, introStep]);
  useEffect(() => screen==='hero'&&definition?.templateId ? undefined : syncBackButton(stack.length > 0 && screen !== 'age' && !pending, back), [back, stack.length, pending, screen, definition?.templateId]);
  useEffect(() => {
    if (booting || hash.startsWith('#system') || hash.startsWith('#result-review')) return;
    const id = `v7.${screen}`; setLogScreen(id); screenView(id);
  }, [screen, booting, hash]);
  useEffect(() => { main.current?.scrollTo(0, 0); if (!document.activeElement?.closest('.resource-switch')) focusHeading.current?.focus({ preventScroll: true }); }, [screen]);
  useEffect(() => { document.documentElement.style.setProperty('--text-scale', String(textScale)); document.documentElement.dataset.largeText = String(textScale > 1); if (!saveLocal('reki-text-scale', textScale)) setStorageError(true); }, [textScale]);
  useEffect(() => () => { cancel.current?.(); }, []);
  useEffect(() => {
    if (!player) return;
    setAccountVisible(5); setActivities(readLocal(`${key}:activity`, [])); setLikes(readLocal(`${key}:likes`, [])); setQualities(readLocal(`${key}:qualities`, []));
  }, [key, !!player]);

  const loadFeed = useCallback(async (p: Player, more?: string) => {
    const request = ++feedRequest.current;
    setFeedLoading(true); setFeedError('');
    try {
      const page = await api.feed(p.worldInstanceId, more);
      if (request !== feedRequest.current) return;
      setFeed(old => more ? [...old, ...page.items.filter(item => !old.some(x => x.postId === item.postId))] : page.items); setCursor(page.nextCursor);
    } catch (cause) { if (request === feedRequest.current) setFeedError(errorText(cause)); }
    finally { if (request === feedRequest.current) setFeedLoading(false); }
  }, []);
  const pilotFor = (doc: WorldDocument, playerId: string) => boundCampaign(doc, readLocal<PilotBinding | undefined>(`reki:v7:${playerId}:story:${doc.id}`, undefined));
  const storyView = (doc: WorldDocument, campaign?: PilotCampaign) => {
    const view = worldView(doc);
    const goal = doc.firstContext.split('\nЦель персонажа: ')[1];
    const persona = player ? personaFor(player.id, doc.id) : {};
    const role = roleForWorld(doc,persona);
    if(role)return {...view,goal:role.goal};
    if (goal || persona.goal) view.goal = persona.goal || goal;
    return campaign ? { ...view, title: storyForCampaign(campaign).worldKey === 'sorcerers' ? storyForCampaign(campaign).scenario : view.title, cover: worldArt[storyForCampaign(campaign!).templateId].cover, hero: worldArt[storyForCampaign(campaign!).templateId].cover, goal: persona.goal || goal || campaign.goal } : view;
  };
  const ownedView = (doc: WorldDocument) => storyView(doc, player ? pilotFor(doc, player.id) : undefined);
  const installPlayer = (p: Player) => { setPlayer(p); const doc = worldDocumentOf(p.world); const campaign = pilotFor(doc, p.id); setActiveCampaign(campaign); setWorld(storyView(doc, campaign));
    const draft = readPostDraft(draftKey(p));
    const savedReceipt = readLocal<PublicationReceipt | undefined>(`reki:v7:${p.id}:result:${p.world}`, undefined);
    setReceipt(receiptForRelease(savedReceipt ? { ...savedReceipt, worldId: p.world } : undefined,preview));
    if(player?.world!==p.world)setShowReceipt(false);
    if (!sending.current) { setDraft(draft.text); setUnknown(!!draft.publicationId); setPublicationFailed(false); }
    return p; };
  const refresh = async () => { const p = installPlayer(await api.me()); await loadFeed(p); await accountEnergy.refresh(); return p; };
  const boot = useCallback(async () => {
    setBooting(true); setError('');
    try {
      await api.loadWorldTemplates();
      setTemplates(worlds.map(w => { const { templateId, ...rest } = worldDefinitionOf(w.id); return worldView({ ...rest, id: templateId!, subscribers: 0 }); }));
      try {
        const p = await api.me();
        if (readLocal(`reki:v7:${p.id}:entry-required`, false)) { setPlayer(p); setWorld(undefined); setScreen('welcome'); setStack([]); }
        else installPlayer(p);
        const own = api.ownedWorldsSnapshot() ?? []; installWorldContent(own); setOwned(own);
        // The catalogue is the agreed returning-player entry. Resume chooses a concrete world instance.
      } catch (cause) {
        if (!(cause instanceof ApiError) || ![401, 404].includes(cause.status)) throw cause;
        if (api.hasPlayer()) { const own = await api.ownedWorlds(); installWorldContent(own); setOwned(own); }
        setScreen('welcome'); setStack([]);
      }
    } catch (cause) { setError(errorText(cause)); }
    finally { setBooting(false); }
  }, []);
  useEffect(() => { void boot(); }, [boot]);

  useEffect(()=>{if(!gainAnimation)return;const timer=setTimeout(()=>setGainAnimation(undefined),2100);return()=>clearTimeout(timer);},[gainAnimation?.nonce]);
  useEffect(() => { if (!rewardAnimation) return; const timer=setTimeout(()=>setRewardAnimation(undefined),1800); return()=>clearTimeout(timer); },[rewardAnimation?.nonce]);
  const activeRole = player ? roleForWorld(worldDocumentOf(player.world),personaFor(player.id,player.world)) : undefined;
  const roleTask = activeRole ? (player?.campaignProgress?.completed ? undefined : activeRole.tasks.find(t=>t.id===player?.campaignProgress?.taskId)??activeRole.tasks[0]) : undefined;
  const campaignProgress = player?.campaignProgress?.percent;
  const curriculum = activeCampaign || (!activeRole && (world ? legacyCurriculum(world,player?optionalWorldTemplate({...worldDocumentOf(player.world),firstContext:worldDocumentOf(player.world).firstContext.split('\nЦель персонажа: ')[0]})?.id:undefined) : undefined)) || undefined;
  const storyKey = player && curriculum ? `${key}:story-play:${player.world}:${curriculum.id}` : '';
  const inventoryKey = `${key}:story-inventory`;
  const playBase = curriculum && storyOwner === storyKey && storyState.campaignId === curriculum.id ? storyState : {...freshStory(curriculum?.id??''),enabled:import.meta.env.DEV};
  const play = preview ? {...playBase,...storyInventory} : {...freshStory(curriculum?.id??''),enabled:false,crystals:0,boosters:0};
  const allQuests = curriculum?.days.flatMap(day => day.quests) ?? [];
  const currentQuest = allQuests.find(q => !play.completed.includes(q.id));
  const editorQuest = allQuests.find(q => q.id === prepared?.questId) ?? currentQuest;
  const storyLabels = activeRole?.skills.map(s=>s.name) ?? (player && personaFor(player.id, player.world).skills?.length === 3 ? personaFor(player.id, player.world).skills! : ['Смелость','Юмор','В тему']);
  const stageDay = curriculum ? (curriculum?.days.find(day => day.quests.some(q => q.id === currentQuest?.id))?.day??curriculum.days.at(-1)?.day??1) : undefined;
  useEffect(() => {
    if (!preview || !curriculum || !storyKey) { setStoryState(freshStory('')); setStoryOwner(''); return; }
    const saved = readLocal<unknown>(storyKey, undefined);
    const initial = repairClosedDays(isStoryPlay(saved,curriculum.id) ? saved : {...freshStory(curriculum.id),enabled:import.meta.env.DEV},curriculum.days.map(d=>({id:`${curriculum.id}:day${d.day}`,questIds:d.quests.map(q=>q.id)})));
    setStoryState(initial); setStoryOwner(storyKey);
    const inventory = readLocal<Inventory | undefined>(inventoryKey,undefined);
    const shared=initialInventory(inventory??{crystals:initial.crystals,boosters:initial.boosters,boosterStock:initial.boosterStock,bonusUsed:initial.bonusUsed});
    setStoryInventory(shared);if(!inventory?.openingGrant)saveLocal(inventoryKey,shared);
    setSelectedBooster(undefined); setRewardAnimation(undefined);
  }, [storyKey]);
  const updateStory = (change: (state: StoryPlay) => StoryPlay) => {
    if (!preview || !storyKey || !curriculum) return;
    const saved = readLocal<unknown>(storyKey, undefined);
    const inventory = initialInventory(readLocal(inventoryKey,storyInventory));
    const before = {...repairClosedDays(isStoryPlay(saved,curriculum.id)?saved:play,curriculum.days.map(d=>({id:`${curriculum.id}:day${d.day}`,questIds:d.quests.map(q=>q.id)}))),...inventory};
    const next = change(before); if (next === before) return;
    setStoryState(next); setStoryOwner(storyKey); const inventoryNext = {crystals:next.crystals,boosters:next.boosters,bonusUsed:next.bonusUsed,boosterStock:stockOf(next),openingGrant:true,loginGiftDays:inventory.loginGiftDays,lastGift:inventory.lastGift,energyGift:inventory.energyGift,energyBoosters:inventory.energyBoosters}; setStoryInventory(inventoryNext); if (!saveLocal(storyKey,next) || !saveLocal(inventoryKey,inventoryNext)) setStorageError(true);
  };
  const openSeriesGift = () => {
    if(!preview||!player||!play.enabled)return;
    const before=initialInventory(readLocal(inventoryKey,storyInventory));
    const next=claimSeriesGift(before,moscowDay(),seriesGiftFor(player.id,moscowDay()));
    setStoryInventory(next);if(!saveLocal(inventoryKey,next))setStorageError(true);
    if(next!==before){const g=next.lastGift!.gift;setGiftReveal(g.kind==='crystals'?`+${g.amount} кристалл в пробе`:g.kind==='energy'?`Запас энергии +${g.amount} · в инвентаре`:'Новый бустер в инвентаре');}
  };
  const creditQuest = (actionId: string, reference?: {questId?:string; mode?:string; booster?:boolean|BoosterId}) => {
    if (!preview || !curriculum || reference?.mode !== 'story') return;
    const quest = allQuests.find(q => q.id === reference.questId); if (!quest) return;
    let reward: {amount:number;title:string;questId:string} | undefined;
    updateStory(before => { const day=curriculum.days.find(d=>d.quests.some(q=>q.id===quest.id))!; const next = finishQuest(before,quest.id,actionId,quest.baseXP,reference.booster??false,{id:`${curriculum.id}:day${day.day}`,number:day.day,questIds:day.quests.map(q=>q.id),requires:day.day>1?`${curriculum.id}:day${day.day-1}`:undefined,unlockActor:curriculum.actorOrder?.find(id=>!curriculum.initialActors?.includes(id)&&!(before.followingActors??[]).includes(id))}); if(next!==before) reward={amount:(next.total-before.total)/100,title:quest.title,questId:quest.id}; return next; });
    if(reward) { setRewardAnimation({amount:reward.amount,nonce:actionId}); setSelectedBooster(undefined); }
    return reward;
  };
  const openWorld = async (w: World, storyVariant = false) => {
    if (sending.current || pending || busy) return;
    if (owned.some(d => d.id === w.id)) { await enter(w); return; }
    setIntroStep(0); setSelected(w); setSelectedVariant(storyVariant); go('intro');
  };
  const startHero = (w: World) => {
    const def = worldDefinitionOf(w.id); setHeroBase(def); setDefinition(def); setHeroChoice(null); setPilotSelected(!!storyForTemplate(def.templateId) && (storyForTemplate(def.templateId)!.worldKey !== 'sorcerers' || selectedVariant)); setHeroGoal(''); setHeroSkills([]); setHeroAvatar(undefined); setHeroSeed(def.avatar); setAvatarRemoved(false); go('hero');
  };
  const chooseHero = (choice?: { id: string }) => {
    if (!definition || !heroBase) return;
    setHeroChoice(choice?.id ?? 'custom');
    setHeroGoal((pilotSelected ? scenarioForTemplate(heroBase.templateId)?.roles.find(r=>r.id===(choice?.id??'custom'))?.goal : undefined) || selected?.goal || '');
    setHeroAvatar(undefined); setHeroSeed(choice?.id ?? crypto.randomUUID()); setAvatarRemoved(false);
    if (pilotSelected) { setDefinition(definitionForRole(heroBase, choice?.id ?? 'custom')); return; }
    if (!choice) { setDefinition(heroBase); return; }
    const star = heroBase.stars.find(star => star.id === choice.id); if (!star) return;
    setDefinition({ ...heroBase, name: star.name, avatar: star.avatarSeed, role: star.role,
      description: `${heroBase.description}\nПерсона игрока: ${star.name}. ${star.about}`.slice(0, 2000),
      stars: heroBase.stars.filter(value => value.id !== star.id) });
  };
  const enter = async (target = selected) => {
    if (!target || pending || busy) return;
    const existing = owned.find(d => d.id === target.id);
    if (existing) {
      setBusy(true); setError('');
      try { const p = installPlayer(await api.me(existing.id)); setFeed([]); setScreen('feed'); setStack([]); await loadFeed(p); }
      catch (cause) { setError(errorText(cause)); } finally { setBusy(false); }
    } else startHero(target);
  };
  const beginEntry = () => {
    if (sending.current || busy) return;
    feedRequest.current++; threadRequest.current++;
    localSave('entry-required', true);
    setWorld(undefined); setReceipt(undefined); setPublicationFailed(false); setActiveCampaign(undefined); setSelected(undefined); setPerson(undefined); setPost(undefined);
    setFeed([]); setCursor(null); setReply(undefined); setQuote(undefined); setToast(undefined);
    setOverlay(null); setUnknown(false); setError(''); setQuery(''); setGenre('Все');
    interestOnly.current = false; setEntryAge(18); setAge(''); setScreen('welcome'); setStack([]);
  };
  const completeInterests = (values: string[]) => {
    setQualities(values); localSave('qualities', values); setQuery(''); setGenre('Все');
    if (interestOnly.current) { back(); interestOnly.current = false; return; }
    localSave('entry-required', false); setWorld(undefined); setScreen('catalog'); setStack([]);
  };
  const continueAge = (value: number) => {
    setEntryAge(value); setAge(ageBandOf(value) ?? '');
    interestOnly.current = false; setStack(['welcome', 'age']); setScreen('onboarding'); setError('');
  };
  const createWorld = async () => {
    if (!definition || busy) return;
    setBusy(true); setError('');
    try {
      if (!api.hasPlayer()) {
        if (!age) { setError('Проверь возраст перед началом истории.'); setScreen('age'); return; }
        await api.onboarding({ birthGroup: age === '14-17' ? '14-17' : '18+', ageBand: age, address, referralCode: pendingReferral() });
        clearReferral(); trackEvent('onboarding_step', { step: 'age', ok: true });
        saveLocal(`reki:v7:${api.playerId()}:qualities`, qualities); saveLocal(`reki:v7:${api.playerId()}:entry-required`, false);
      }
      const created = await api.createWorld(definition);
      const role = pilotSelected ? scenarioForTemplate(definition.templateId)?.roles.find(r=>r.id===heroChoice) : undefined;
      const portrait = avatarRemoved ? undefined : heroAvatar || await heroPortrait(heroChoice ?? 'custom', heroSeed || definition.avatar, definition.templateId || created.world, definition.avatarStyle).catch(() => undefined);
      if (!savePersona(created.id, created.world, { goal: role?.goal??heroGoal.trim(), skills: role?.skills.map(s=>s.name)??heroSkills, ...(role?{roleId:role.id,templateId:definition.templateId!}:{}), avatar: avatarRemoved ? '' : portrait })) setStorageError(true);
      accountIdentity(created.id,owned[0]?.name??created.name);
      const p = installPlayer(created); trackEvent('onboarding_step', { step: 'done', ok: true, world: p.world }); localSave('qualities', qualities);
      const own = await api.ownedWorlds(); installWorldContent(own); setOwned(own);
      setStoryOnboarding(!preview || !!role); setScreen('goals'); setStack([]); await loadFeed(p);
    } catch (cause) { setError(errorText(cause)); } finally { setBusy(false); }
  };
  const replyKey = (target: ReplyTarget) => `${key}:reply:${player?.world}:${target.postId}:${target.commentId ?? 'root'}`;
  const openCompose = (target?: ReplyTarget, context?: { name: string; text: string }) => {
    if (!player || sending.current) return;
    setPrepared(undefined); setReply(target); setQuote(context); setUnknown(false); setStatus('');
    if (target) { const saved = readLocal<ReplyDraft>(replyKey(target), { text: '' }); setDraft(saved.text); setUnknown(!!saved.requestId); }
    else { const saved = readPostDraft(draftKey(player)); setDraft(saved.text); setUnknown(!!saved.publicationId); }
    go('compose');
  };
  const openQuest = (questId?: string) => {
    if (!world || !player || sending.current) return;
    if(!preview&&activeRole){openCompose();if(roleTask)setPrepared({title:roleTask.context,prompt:roleTask.prompt,questId:roleTask.id});return;}
    const quest = allQuests.find(q => q.id === questId) ?? currentQuest;
    const doc = worldDocumentOf(player.world);
    const template = optionalWorldTemplate({...doc,firstContext:doc.firstContext.split('\nЦель персонажа: ')[0]});
    const starter = starterFor(template?.id,world);
    openCompose(undefined, { name: 'Квест', text: quest?.prompt ?? starter.prompt });
    const award=quest?play.questAwards?.[quest.id]:undefined;
    setPrepared({ title: quest?.title ?? starter.title, prompt: quest?.prompt ?? starter.prompt, questId: quest?.id, xp: award?.status==='partial'?(award.capCents-award.baseCents)/100:quest?.baseXP ?? starter.xp });
    const saved = readPostDraft(draftKey(player));
    const questDay=curriculum?.days.find(d=>d.quests.some(q=>q.id===quest?.id));
    const dayId=questDay&&curriculum?`${curriculum.id}:day${questDay.day}`:undefined;
    if(preview&&dayId)updateStory(s=>freezeStoryDay(s,dayId));
    const dayLevels=dayId?play.dayLevels?.[dayId]??play.levels:play.levels;
    const focus = quest?.focusSkill === 'secondary' ? 1 : quest?.focusSkill === 'tertiary' ? 2 : 0;
    const eligible = quest?.drafts.filter(d => d.minSkill <= dayLevels[focus]).sort((a,b)=>b.minSkill-a.minSkill)[0];
    const preparedText = eligible?.text || activeCampaign?.firstPost || starter.draft;
    if (!saved.text.trim() && !saved.publicationId) {
      const text = preparedText; setDraft(text);
      if (!savePostDraft(draftKey(player),{text,ownWords:false})) setStorageError(true);
    }
  };
  const openFirstPost = () => { setStoryOnboarding(false); if(curriculum&&!currentQuest){setPlayMode('sandbox');openCompose(undefined,{name:'Свободная история',text:'Доступный эпизод завершён. Можно продолжить разговор в песочнице.'});return;} openQuest(currentQuest?.id); };
  const updateDraft = (text: string) => {
    setDraft(text);
    if (!player) return;
    if (reply) { const saved = readLocal<ReplyDraft>(replyKey(reply), { text: '' }); if (!saveLocal(replyKey(reply), { ...saved, text })) setStorageError(true); }
    else { const k = draftKey(player); if (!savePostDraft(k, { ...readPostDraft(k), text, ownWords: true })) setStorageError(true); }
  };
  const openThread = async (id: string) => {
    if (!id || pending) return;
    const request = ++threadRequest.current; setPost(undefined); setThreadLoading(true); go('thread');
    try { const p = await api.post(id); if (threadRequest.current === request) { setPost(p); const next = activities.map(a => a.id === id ? { ...a, read: true } : a); setActivities(next); localSave('activity', next); } }
    catch (cause) { if (threadRequest.current === request) setError(errorText(cause)); }
    finally { if (threadRequest.current === request) setThreadLoading(false); }
  };
  const publish = () => {
    if (!player || !draft.trim() || sending.current) return;
    if (player.postsLeft <= 0 && !unknown) { trackEvent('limit_reached'); setEnergyNudge(Date.now()); setScreen('feed'); setStack([]); return; }
    if (reply) { void sendReply(); return; }
    const k = draftKey(player), saved = readPostDraft(k);
    const command = submissionFor(saved, draft, () => crypto.randomUUID());
    const { id, text, recovery } = command;
    const beforeKey=`${key}:turn-before:${player.world}:${id}`;
    const fallback={skills:player.skills,roleSkills:player.roleSkills,questId:prepared?.questId??roleTask?.id??currentQuest?.id};
    if(!recovery)saveLocal(beforeKey,fallback);
    const turnBefore=readLocal(beforeKey,fallback);
    if (!recovery && curriculum) localSave(`story-action:${player.world}:${id}`,{questId:editorQuest?.id,mode:playMode,booster:selectedBooster});
    if (!recovery) trackEvent('post_publish', { len: text.length, own_words: command.ownWords });
    if (!savePostDraft(k, { ...saved, text: draft, publicationId: id, submittedText: text, ownWords: command.ownWords, submittedOwnWords: command.ownWords })) setStorageError(true);
    sending.current = true; setPending(true); setUnknown(false); setPublicationFailed(false); setError(''); setStatus('pending'); setScreen('feed'); setStack([]);
    cancel.current = api.publish(text, id, event => {
      if (event.type !== 'result') return;
      trackEvent('post_result', { delta: event.result.delta, viral: event.result.viral, flop: event.result.flop });
      completePostDraft(k, id, text); setDraft(readPostDraft(k).text);
      const serverOutcome=parseQuestOutcome((event.result as unknown as {questOutcome?:unknown}).questOutcome);
      const reward = serverOutcome?undefined:creditQuest(event.result.postId,readLocal(`${key}:story-action:${player.world}:${id}`,undefined));
      const value: PublicationReceipt = { worldId: player.world, questId:turnBefore.questId, result: event.result, before: turnBefore.skills, roleBefore:turnBefore.roleSkills, roleLabels:activeRole?.skills.map(s=>s.name), questReward: reward,questOutcome:serverOutcome??(reward?{status:'success',source:'local',title:reward.title,reason:'Действие сохранено. Шаг закрыт в сюжетной пробе.',xp:reward.amount}:{status:'unverified',source:'server',title:editorQuest?.title??'Твоя запись',reason:playMode==='sandbox'?'Свободная запись опубликована.':'Пост опубликован, но серверный исход квеста пока не возвращается.'}) };
      setReceipt(value);setShowReceipt(true); saveLocal(`${key}:result:${player.world}`, value);
      if(value.questId)saveLocal(`${key}:task-result:${player.world}:${value.questId}`,value);
      setToast(event.result); setGainAnimation({delta:event.result.delta,nonce:event.result.postId});
      const best=event.result.bestComment;
      const actor=best?world?.cast.find(p=>p.handle===best.avatar||p.id===best.avatar):undefined;
      if(best&&actor){const notice:ReactionNotice={id:event.result.postId,postId:event.result.postId,worldId:player.world,kind:'reaction',actorName:actor.name,text:best.text,relation:actor.relation};setReactionNotice(notice);}
      setPublicationFailed(false); setUnknown(false); setPending(false); sending.current = false; setStatus('completed');
      const item: Activity = { id: event.result.postId, worldId: player.world, title: actor ? `${actor.name} отвечает на твою запись` : event.result.headline, text: best?.text ?? `${event.result.delta > 0 ? '+' : ''}${event.result.delta} подписчиков`, read: false, ...(actor&&best?{kind:'reaction' as const,actorName:actor.name,relation:actor.relation}: {}) };
      setActivities(old => { const next = [item, ...old.filter(a => a.id !== item.id)].slice(0, 100); localSave('activity', next); return next; });
      setScreen('feed'); setStack([]); void refresh().then(p => { const updated = { ...value, after: p.skills, roleAfter:p.roleSkills, questOutcome:p.lastTurn?.actionId===id?{...p.lastTurn.outcome,source:'server' as const}:value.questOutcome }; setReceipt(updated); saveLocal(`${key}:result:${player.world}`, updated); if(value.questId)saveLocal(`${key}:task-result:${player.world}:${value.questId}`,updated); const freshActor=best?worldDocumentOf(p.world).stars.find(s=>s.avatarSeed===best.avatar):undefined; if(freshActor){setReactionNotice(n=>n?.id===event.result.postId?{...n,relation:freshActor.relation}:n);setActivities(old=>{const next=old.map(a=>a.id===event.result.postId&&a.worldId===player.world?{...a,relation:freshActor.relation}:a);localSave('activity',next);return next;});} }).catch(() => setFeedError('Публикация сохранена. Обнови ленту, чтобы увидеть результат.'));
    }, (state, reason) => {
      sending.current = false; setPending(false); setUnknown(state === 'unknown'); setPublicationFailed(state === 'failed');
      if (state === 'failed') { const latest = readPostDraft(k); savePostDraft(k, { text: latest.text, ownWords: latest.ownWords }); }
      setError(publicationError(state, reason));
    }, setStatus, command.ownWords, command.wantId, recovery);
  };
  const sendReply = async () => {
    if (!player || !reply || sending.current) return;
    const target=reply,k=replyKey(target),saved=readLocal<ReplyDraft>(k,{text:''});
    const requestId=saved.requestId??crypto.randomUUID(),text=saved.submittedText??draft;
    if(!saved.requestId&&curriculum)localSave(`story-action:${player.world}:${requestId}`,{questId:currentQuest?.id,mode:playMode,booster:selectedBooster});
    saveLocal(k,{...saved,text:draft,requestId,submittedText:text,beforeSubscribers:saved.beforeSubscribers??player.subs,beforeSkills:saved.beforeSkills??player.skills,roleBefore:saved.roleBefore??player.roleSkills,questId:saved.questId??roleTask?.id??currentQuest?.id});
    sending.current=true;setPending(true);setError('');setReceipt(undefined);setStatus('Сохраняем комментарий…');
    let snapshot:Awaited<ReturnType<typeof api.reply>>|undefined,completed=false;
    try{
      const controller=new AbortController();cancel.current=()=>controller.abort();
      snapshot=await api.reply(target.postId,target.commentId,requestId,text,target.authorId,{
        signal:controller.signal,additionalStarId:saved.additionalStarId,
        onSaved:value=>{const first=!snapshot;snapshot=value;if(first)void accountEnergy.refresh();setPost({...value.post,id:value.post.postId});setPlayer(p=>p?{...p,postsLeft:value.energyLeft,subs:value.subscribers}:p);setScreen('thread');setStack(v=>v.filter(x=>x!=='compose'));},
        onTyping:authorId=>setStatus(authorId?`${world?.cast.find(p=>`Star_${p.id}`===authorId)?.name??'Персонаж'} печатает…`:''),
        onAdditionalStar:id=>saveLocal(k,{...readLocal<ReplyDraft>(k,{text}),additionalStarId:id??undefined}),
      });
      completed=true;saveLocal(k,{text:''});setDraft('');setReply(undefined);setUnknown(false);setScreen('thread');setStack(v=>v.filter(x=>x!=='compose'));
    }catch(cause){
      if(snapshot){setUnknown(true);setError('Комментарий сохранён. Ответ персонажа пока не готов — можно продолжить получение с той же попыткой.');}
      else{const rejected=cause instanceof ApiError&&cause.status>=400&&cause.status<500&&cause.status!==409;if(rejected)saveLocal(k,{text:draft});setUnknown(!rejected);setError(rejected?'Комментарий не сохранён. Текст оставлен для исправления.':errorText(cause)+' Текст и попытка сохранены.');}
    }finally{
      if(snapshot){
        const reward=completed?creditQuest(requestId,readLocal(`${key}:story-action:${player.world}:${requestId}`,undefined)):undefined;
        const delta=snapshot.subscribers-(saved.beforeSubscribers??player.subs);
        const value:PublicationReceipt={worldId:player.world,questId:saved.questId??roleTask?.id??currentQuest?.id,actionKind:'comment',audienceKnown:true,before:saved.beforeSkills??player.skills,roleBefore:saved.roleBefore??player.roleSkills,roleLabels:activeRole?.skills.map(s=>s.name),questReward:reward,questOutcome:reward?{status:'success',source:'local',title:reward.title,reason:'Комментарий и реакция сохранены. Шаг закрыт в сюжетной пробе.',xp:reward.amount}:{status:'unverified',source:'server',title:'Твой комментарий',reason:completed?'Комментарий и реакция сохранены. Серверная оценка квеста пока не возвращается.':'Комментарий сохранён. Получение реакции ещё не завершено.'},result:{postId:target.postId,delta,headline:'Комментарий сохранён',whys:[],ribbons:[],viral:false,flop:false,views:snapshot.post.views}};
        setReceipt(value);setShowReceipt(true);saveLocal(`${key}:result:${player.world}`,value);if(delta)setGainAnimation({delta,nonce:requestId});
        void refresh().then(p=>{const updated={...value,after:p.skills,roleAfter:p.roleSkills,questOutcome:p.lastTurn?.actionId===requestId?{...p.lastTurn.outcome,source:'server' as const}:value.questOutcome};setReceipt(updated);saveLocal(`${key}:result:${player.world}`,updated);if(value.questId)saveLocal(`${key}:task-result:${player.world}:${value.questId}`,updated);}).catch(()=>setFeedError('Комментарий сохранён. Обнови ленту, чтобы получить актуальные данные.'));
      }
      sending.current=false;setPending(false);
    }
  };
  const stopWaiting = () => { cancel.current?.(); sending.current = false; setPending(false); setUnknown(true); setError('Ожидание остановлено. Запрос мог сохраниться на сервере — проверь статус, чтобы продолжить без дубликата.'); };
  const deleteAccount = async () => {
    const owner=player?.id; if(!owner||pending||sending.current)throw new Error('Account is busy');
    await deleteConfirmedAccount(owner,()=>api.deleteAccount(),clearPersonas,[localStorage,sessionStorage]);
    cancel.current?.(); feedRequest.current++;threadRequest.current++;setDeleteOpen(false);setPlayer(undefined);setWorld(undefined);setOwned([]);setFeed([]);setPost(undefined);setReceipt(undefined);setDraft('');setStack([]);setScreen('welcome');
  };
  const showPerson = (value: Person) => { if (value.id === 'player') go('profile'); else { const cast = world?.cast.find(p => p.id === value.id || p.name === value.name); setPerson(cast ?? value); go('character'); } };
  const card = (p: Post, author: Person, count = 0, compact = false) => <PostCard key={p.id} post={{ ...p, liked: likes.includes(`${player?.world}:${p.id}`) }} author={author} replyCount={count} compact={compact}
    onReport={p.parentId && author.id!=='player' && p.text!=='Комментарий скрыт' ? ()=>setReportId(p.id) : undefined}
    onReply={p.parentId || p.id ? () => void openThread(p.parentId ?? p.id) : undefined} onAuthor={() => showPerson(author)}
    onLike={() => { const id = `${player?.world}:${p.id}`; const next = likes.includes(id) ? likes.filter(x => x !== id) : [...likes, id]; setLikes(next); localSave('likes', next); }}
    onQuote={() => { setQuote({ name: author.name, text: p.text }); setOverlay('quote'); }} />;

  useEffect(()=>{if(!energyNudge)return;const timer=setTimeout(()=>setEnergyNudge(0),6000);return()=>clearTimeout(timer);},[energyNudge]);
  const renderFeed = () => <>
    <div className="world-hud"><header className="world-header"><button className="world-switch" aria-label={`${world?.title??''}. ${formatAudience(player?.subs??0)} подписчиков. Выбрать мир`} onClick={() => setOverlay('exit')}><strong><WorldName name={world?.title??''}/><Icon name="chevron"/></strong><span className="channel-sidebar-count"><b className="channel-count-target">{formatAudience(player?.subs??0)}</b><span className="channel-count-label">подписчиков</span></span></button>
      <div key={energyNudge} className={`energy-hud${energyNudge?' is-empty':''}`}><EnergyBadge wallet={accountEnergy.wallet} amount={player?.postsLeft} onClick={()=>{setEnergyNudge(0);openResource('resources');}}/></div>
      <ResourceBadge type="crystal" value={accountEnergy.wallet?String(accountEnergy.wallet.crystals):preview && curriculum ? String(play.crystals) : '—'} hint={accountEnergy.wallet?"Кристаллы для обмена на энергию":preview?"Кристаллы локальной пробы и бустеры":"Баланс кристаллов пока недоступен"} onClick={() => openResource('crystals')} />
      <button className="profile-access campaign-avatar" aria-label={`Профиль персонажа. История: ${campaignProgress===undefined?'прогресс уточняется':`${campaignProgress}%`}`} onClick={() => go('profile')} style={{background:`conic-gradient(var(--campaign-ring) ${campaignProgress??0}%,var(--color-divider) 0)`}}><Avatar name={player?.name ?? ''} src={player ? personaFor(player.id, player.world).avatar : undefined} size="lg" /></button></header>
    </div>
    {curriculum&&play.enabled&&play.available>0&&<section className="section"><div className="xp-day-ready"><strong>{formatXP(play.available)} XP можно направить на навыки</strong><Button onClick={()=>go('profile')}>Выбрать улучшения</Button></div></section>}
    {preview && curriculum && <section className="story-tools"><button className="map-access" onClick={() => setOverlay('map')}>Карта мира <span aria-hidden="true">↗</span></button><button className="local-play-badge" onClick={() => openResource('boosters')}>{preview?(play.enabled?'Локальная проба':'Включить сюжетную пробу'):'План истории'}</button></section>}
    {player?.postsLeft===0&&draft.trim()&&!pending&&<section className="section"><div className="energy-draft-notice"><strong>Черновик ждёт энергии</strong><blockquote className="energy-draft-text">{draft}</blockquote><p>Текст сохранён. Нажми на энергию вверху, чтобы посмотреть восстановление и обмен.</p><Button variant="secondary" onClick={()=>reply?openCompose(reply):openFirstPost()}>Вернуться к черновику</Button></div></section>}
    {!reply && (pending || unknown || publicationFailed) && <PublicationWaiting pending={pending} unknown={unknown} text={draft} status={status} onStop={stopWaiting} onContinue={() => { if (unknown) publish(); else openCompose(); }} />}
    {feedError && <div className="section"><Notice title="Не удалось обновить ленту" tone="error">{feedError}</Notice></div>}
    {!feed.length && !feedLoading && !feedError && !pending && !unknown && !publicationFailed && <EmptyState title="Первое слово за тобой" action={<Button onClick={() => openCompose()}>Написать пост</Button>}>{world?.seedPost || 'Расскажи свою версию истории. Здесь появятся публикации и ответы.'}</EmptyState>}
    {feed.map(item => { const view = feedView(item, playerView(player!)); return card(view.post, view.author, view.count); })}
    {(feed.length > 0 || feedError || feedLoading) && (!pending || feed.length > 0) && <div className="section feed-footer"><Button variant="ghost" busy={feedLoading} onClick={() => void loadFeed(player!, cursor ?? undefined)}>{feedLoading ? 'Загружаем ленту' : cursor ? 'Показать ещё' : 'Обновить ленту'}</Button></div>}
  </>;

  const worldActivities = activitiesForWorld(activities, player?.world).filter(a=>a.kind==='reaction'||a.kind==='mention'||a.kind==='follow');
  const selectedStory = scenarioForTemplate(definition?.templateId);
  const heroOptions = definition ? pilotSelected && selectedStory ? selectedStory.roles.map(role=>({id:role.id,name:role.id==='custom'?'Свой персонаж':role.name,role:role.name,goal:role.goal,skills:role.skills})) : [...(heroBase??definition).stars.map(s=>({id:s.id,name:s.name,role:s.role,goal:selected?.goal||''})),{id:'custom',name:'Свой персонаж',role:'Твоя история',goal:selected?.goal||''}] : [];
  let content: ReactNode;
  if (booting) content = <div className="loading-state" role="status"><LoadingIndicator/><p>Открываем твои миры</p></div>;
  else if (screen === 'catalog' || screen === 'search') {
    const story=scenarioForTemplate('aec9494b-4118-4525-8458-e85d759c8dd9')!;
    const entries = rankCatalog(withHogwartsCard(catalogEntries(templates, plannedWorlds), {templateId:story.templateId,scenario:story.title,premise:story.opening,subtitle:'Чьим будет будущее школы?'}).map(entry=>{const scenario=scenarioForTemplate(entry.world.id);return entry.available&&scenario&&(entry.storyVariant||scenario.templateId!==story.templateId)?{...entry,world:{...entry.world,title:scenario.title,description:scenario.opening,introTitles:scenario.openingTitles,tagline:scenario.opening.split('. ')[0]}}:entry;}), qualities);
    const filtered = filterCatalog(entries, query, genre);
    content = <><ScreenHeading title={screen === 'search' ? 'Найти свой мир' : 'Миры'} action={screen==='catalog'&&<button className="catalog-settings" aria-label="Настроить интересы" title="Интересы" onClick={()=>{interestOnly.current=true;go('onboarding');}}><Icon name="preferences"/></button>} />
      {screen === 'search' && <div className="search-field"><TextField label="Название или тема" value={query} onChange={e => setQuery(e.target.value)} placeholder="Магия, приключения, общение…" /></div>}
      {screen === 'catalog' && owned.length > 0 && <section className="saved-worlds"><h2 className="section-title">Мои миры</h2><WorldCarousel>{owned.map(d => <ResumeWorld key={d.id} world={ownedView(d)} subtitle={d.name} busy={busy} onClick={() => void openWorld(ownedView(d))} />)}</WorldCarousel></section>}
      <div className="filter-strip">{['Все', ...new Set(entries.map(({ world }) => world.genre))].map(g => <Chip key={g} selected={genre === g} onClick={() => setGenre(g)}>{g}</Chip>)}</div>
      {filtered.length ? <div className="world-grid">{filtered.map(({ key, world: w, available, storyVariant }) => <WorldCard key={key} world={w} disabled={!available || busy} selected={available && selected?.id === w.id && selectedVariant === !!storyVariant} onClick={available ? () => void openWorld(w, !!storyVariant) : undefined} />)}</div> : <EmptyState title="Такого мира пока нет" action={<Button onClick={() => { setQuery(''); setGenre('Все'); if (!templates.length) void boot(); }}>Сбросить поиск</Button>}>Попробуй другое название или убери фильтр.</EmptyState>}
      {player?.hitOwnWorldMilestone && <div className="section"><Button onClick={() => { setDefinition({ ...blankWorld(), name: player.name, avatar: player.handle, stars: [{ id: crypto.randomUUID(), name: '', role: '', about: '', voice: 'Живо и по делу', avatarSeed: 'guide', trait: 1, relation: 0 }] }); go('hero'); }}>Создать свой мир</Button></div>}
    </>;
  } else if (screen === 'intro' && selected) content = <WorldIntro key={selected.id} step={introStep} onStepChange={setIntroStep} world={selected} busy={busy} resume={owned.some(d => d.id === selected.id)} onBack={back} onEnter={() => void enter()} />;
  else if (screen === 'welcome') content = <Onboarding key="entry-welcome" initialStep={0} onTry={() => go('age')} onDone={() => go('age')} onBack={back} />;
  else if (screen === 'age') content = <AgeEntry initialAge={entryAge} minimumAge={14} requireConsent={true} onBack={value => { setEntryAge(value); back(); }} onContinue={continueAge} />;
  else if (screen === 'onboarding') content = <Onboarding key="entry-interests" initialStep={1} standalone={interestOnly.current} initialQualities={qualities} onBack={back} onDone={completeInterests} />;
  else if (screen === 'hero' && definition && definition.templateId) content = <><HeroStudio onBack={back}
    options={heroOptions} selected={heroChoice} onChoose={id=>chooseHero(id==='custom'?undefined:{id})}
    name={definition.name} channel={definition.avatar} goal={heroGoal} onName={name=>setDefinition({...definition,name})} onChannel={avatar=>setDefinition({...definition,avatar})}
    skills={heroSkills} onSkills={setHeroSkills} seed={heroSeed||definition.avatar} avatar={heroAvatar} removed={avatarRemoved}
    onLook={seed=>{setHeroSeed(seed);setHeroAvatar(undefined);setAvatarRemoved(false);}} onUpload={src=>{setHeroAvatar(src);setAvatarRemoved(false);}} onRemove={()=>{setHeroAvatar(undefined);setAvatarRemoved(true);}}
    address={address} onAddress={setAddress} worldId={definition.templateId} worldTitle={selected?.title||definition.title} style={definition.avatarStyle}
    busy={busy} canEnter={!!heroChoice&&!!definition.stars.length&&(heroChoice!=='custom'||!!(definition.name.trim()&&definition.avatar.trim()&&heroGoal.trim()))} onEnter={()=>void createWorld()}/></>;
  else if (screen === 'hero' && definition) content = <><ScreenHeading title="Свой мир" back={back} /><section className="section stack"><TextField label="Имя персонажа" maxLength={80} value={definition.name} onChange={e => setDefinition({ ...definition, name: e.target.value })} /><TextField label="Имя канала" maxLength={80} value={definition.avatar} onChange={e => setDefinition({ ...definition, avatar: e.target.value })} />
    <label>Как обращаться<select value={address} onChange={e => setAddress(e.target.value as Address)}><option value="nick">По имени</option><option value="she">Она</option><option value="he">Он</option></select></label>
    <TextField label="Кто ты в этом мире" value={definition.role} maxLength={80} onChange={e => setDefinition({ ...definition, role: e.target.value })} />
    <TextField label="Название мира" value={definition.title} onChange={e => setDefinition({ ...definition, title: e.target.value })} /><label>Описание<textarea value={definition.description} onChange={e => setDefinition({ ...definition, description: e.target.value })} maxLength={2000} /></label><TextField label="Место действия" value={definition.where} onChange={e => setDefinition({ ...definition, where: e.target.value })} /><TextField label="Первая завязка" value={definition.firstContext} onChange={e => setDefinition({ ...definition, firstContext: e.target.value })} /><h2>Собеседники</h2>{definition.stars.map((star, index) => <div className="stack" key={star.id}><TextField label={`Имя собеседника ${index + 1}`} value={star.name} onChange={e => setDefinition({ ...definition, stars: definition.stars.map((s, i) => i === index ? { ...s, name: e.target.value } : s) })} /><TextField label="Роль собеседника" value={star.role} onChange={e => setDefinition({ ...definition, stars: definition.stars.map((s, i) => i === index ? { ...s, role: e.target.value } : s) })} /><TextField label="О собеседнике" value={star.about} onChange={e => setDefinition({ ...definition, stars: definition.stars.map((s, i) => i === index ? { ...s, about: e.target.value } : s) })} /></div>)}<Button variant="secondary" disabled={definition.stars.length >= 3} onClick={() => setDefinition({ ...definition, stars: [...definition.stars, { id: crypto.randomUUID(), name: '', role: '', about: '', voice: 'Живо и по делу', avatarSeed: crypto.randomUUID(), trait: 1, relation: 0 }] })}>Добавить собеседника</Button>
    <label>Градус драмы<select value={definition.drama} onChange={e => setDefinition({ ...definition, drama: e.target.value as WorldDefinition['drama'] })}><option value="soft">Мягко</option><option value="mid">Средне</option><option value="hot">Остро</option><option value="off">Без хейта</option></select></label>
    <Button busy={busy} disabled={!definition.name.trim() || !definition.avatar.trim() || !definition.role.trim() || !definition.title.trim() || !definition.stars.length || definition.stars.some(star => !star.name.trim() || !star.role.trim())} onClick={() => void createWorld()}>Начать историю</Button></section></>;
  else if (screen === 'feed' && player && world) content = renderFeed();
  else if (screen === 'goals' && !preview && world && player) {
    const doc = worldDocumentOf(player.world);
    const template = optionalWorldTemplate({...doc, firstContext: doc.firstContext.split('\nЦель персонажа: ')[0]});
    const task = roleTask ?? (activeRole ? undefined : currentQuest ?? starterFor(template?.id, world));
    const savedDraft=readPostDraft(draftKey(player));
    const taskReceipt=receiptForRelease((receipt?.questId===(roleTask??currentQuest)?.id?receipt:undefined)??readLocal<PublicationReceipt|undefined>(`${key}:task-result:${player.world}:${(roleTask??currentQuest)?.id}`,undefined)??(receipt?.worldId===player.world&&((currentQuest&&receipt.questId===currentQuest.id)||receipt.questId===roleTask?.id||receipt.questOutcome?.title===task?.title)?receipt:undefined),false);
    content = <WorldBrief roleSkills={activeRole?.skills} campaignProgress={campaignProgress} campaignComplete={player.campaignProgress?.completed} practiceSkills={personaFor(player.id,player.world).skills??[]} world={world} player={player} avatar={personaFor(player.id, player.world).avatar} task={task} firstVisit={storyOnboarding} hasDraft={savedDraft.ownWords&&!!savedDraft.text.trim()} receipt={taskReceipt} onRead={taskReceipt?()=>void openThread(taskReceipt.result.postId):undefined} onWrite={openFirstPost} onFeed={() => { setStoryOnboarding(false); go('feed'); }} />;
  } else if (screen === 'goals' && activeCampaign) content = <StoryOpening firstVisit={storyOnboarding} playerName={player?.name ?? activeCampaign.heroName} avatar={player ? personaFor(player.id,player.world).avatar : undefined} worldTitle={world?.title || storyForCampaign(activeCampaign).scenario} campaign={activeCampaign} goal={world?.goal || activeCampaign.goal} skills={storyLabels} step="mission" onNext={() => go('story-skills')} onBack={back} />;
  else if (screen === 'story-skills' && activeCampaign) content = <StoryOpening firstVisit={storyOnboarding} playerName={player?.name ?? activeCampaign.heroName} avatar={player ? personaFor(player.id,player.world).avatar : undefined} worldTitle={world?.title || storyForCampaign(activeCampaign).scenario} campaign={activeCampaign} goal={world?.goal || activeCampaign.goal} skills={storyLabels} step="skills" onNext={openFirstPost} onBack={back} />;
  else if (screen === 'goals' && world) content = <><ScreenHeading title="Цели" subtitle={world.title} /><StoryBrief goal={world.goal || 'Найти свой голос и начать разговор.'} /><section className="section"><Button onClick={openFirstPost}>Написать пост →</Button></section></>;
  else if (['resources','crystals','boosters'].includes(screen)) content = <ResourcePages wallet={accountEnergy.wallet} exchangeBusy={accountEnergy.busy} exchangeMessage={accountEnergy.message} onExchange={()=>void accountEnergy.exchange()} onWalletRefresh={()=>void accountEnergy.refresh()} preview={preview} page={screen as ResourcePageName} energy={player?.postsLeft} state={play} activeStory={!!curriculum} selectedBooster={selectedBooster} onPage={openResource} onBack={back} onEnable={()=>updateStory(v=>({...v,enabled:true}))} onDisable={()=>{updateStory(v=>({...v,enabled:false}));setSelectedBooster(undefined);}} onGrant={()=>updateStory(grantDemoCrystals)} onBuy={id=>updateStory(s=>buyBooster(s,id))} onToggle={setSelectedBooster} onChooseWorld={()=>go('catalog')}/>;
  else if (screen === 'compose' && player) content = <><ScreenHeading title={reply ? 'Продолжить разговор' : 'Новая публикация'} back={back} /><section className="section stack composer-body">{prepared && playMode==='story' && <TaskCard title={prepared.title} prompt={prepared.prompt}>{preview && prepared.xp && <span className="quest-xp">{prepared.xp} XP за выполнение</span>}{preview&&<details className="quest-reward-info"><summary>Как начисляется XP</summary><p className="caption muted">Начисление доступно в отдельно включаемой сюжетной пробе. Для старых миров без сюжетной привязки здесь показана награда будущего зачёта.</p></details>}</TaskCard>}<PracticeFocus skills={personaFor(player.id,player.world).skills??[]}/>{(reply || quote) && !prepared && <QuoteBlock name={reply?.name ?? quote!.name} text={reply?.text ?? quote!.text} />}<div className="row"><Avatar name={player.name} src={personaFor(player.id, player.world).avatar} /><strong>{player.name}</strong></div><label className="sr-only" htmlFor="post-text">Текст публикации</label><textarea id="post-text" className="composer-text" value={draft} onChange={e => updateDraft(e.target.value)} disabled={pending || unknown} maxLength={reply ? 1000 : 2000} autoFocus={!!reply} /><div className="composer-count caption muted" aria-label="Символов в публикации">{draft.length} / {reply ? 1000 : 2000}</div>{preview && curriculum && <PlayMode value={playMode} onChange={mode=>{setPlayMode(mode);if(mode==='sandbox')setSelectedBooster(undefined);}}/>}{preview && playMode==='story'&&<OwnedBoosterPicker state={play} selected={selectedBooster} onSelect={setSelectedBooster} onStore={()=>openResource('boosters')} known={!!curriculum} available={!!curriculum&&play.enabled} sandbox={false}/> }
{quote && !reply && !prepared && <p className="caption muted">Контекст помогает написать пост. Связь цитирования на сервере пока недоступна.</p>}{pending && <div className="pending-line" role="status"><LoadingIndicator/>{statusLabels[status] ?? 'Отправляем ответ…'}</div>}{pending && <Button variant="secondary" onClick={stopWaiting}>Остановить ожидание</Button>}<Button className="full" size="lg" disabled={!draft.trim()} busy={pending} onClick={publish}>{unknown ? 'Проверить статус' : 'Опубликовать'} <span className="caption">· 1 энергия</span></Button></section></>;
  else if (screen === 'thread' && player && world) {
    const view = post ? detailView(post, world, playerView(player)) : undefined;
    const postAuthor = post && world.cast.find(p => `Star_${p.id}` === post.authorId);
    content = <><ScreenHeading title="Обсуждение" back={back} />{pending&&<div className="section stack" role="status"><div className="pending-line"><LoadingIndicator/>{status||'Получаем ответ персонажа…'}</div><Button variant="ghost" onClick={stopWaiting}>Остановить ожидание</Button></div>}{reply&&unknown&&!pending&&<section className="section"><Button onClick={()=>openCompose(reply)}>Продолжить получение ответа</Button><p className="caption muted">Сохранённый комментарий повторно не публикуется.</p></section>}{receipt&&receipt.worldId===player.world&&receipt.result.postId===post?.id&&<PublicationResult preview={preview} receipt={receipt} animateReward={!!rewardAnimation} animateFollowers={!!gainAnimation} onRead={()=>document.querySelector('.thread-nested')?.scrollIntoView({block:'center'})}/>}{threadLoading && <div className="pending-line" role="status"><LoadingIndicator/>Загружаем обсуждение…</div>}{view && card(view.post, view.author, post!.comments.length)}{post?.comments.map(comment => {
      const author: Person = comment.me ? playerView(player) : world.cast.find(p => `Star_${p.id}` === comment.authorId) ?? { id: comment.authorId ?? comment.id, name: comment.author, handle: comment.avatar ?? '', role: comment.role ?? '', boundary: '' };
      return <div key={comment.id} className={comment.parentCommentId ? 'thread-nested' : ''}>{comment.parentCommentId && <span className="sr-only">Ответ на реплику</span>}{card({ id: comment.id, parentId: post.id, authorId: author.id, text: comment.hidden ? 'Комментарий скрыт' : comment.text, createdAt: '', likes: 0 }, author, 0, true)}{comment.star && !comment.hidden && world.cast.some(p => `Star_${p.id}` === comment.authorId) && <div className="reply-action"><Button variant="ghost" aria-label={`Ответить ${author.name}`} onClick={() => openCompose({ postId: post.id, commentId: comment.id, name: author.name, text: comment.text, authorId:`Star_${author.id}` })}>Ответить</Button></div>}</div>;
    })}{post && !post.comments.length && <EmptyState title="Здесь ещё нет ответов">Персонажи могут продолжить разговор после публикации.</EmptyState>}{postAuthor && post && <div className="section"><Button className="full" onClick={() => openCompose({ postId: post.id, commentId: null, name: postAuthor.name, text: post.text, authorId:`Star_${postAuthor.id}` })}>Начать разговор с автором</Button></div>}</>;
  } else if (screen === 'profile' && player && world) content = <><ProfileHeader name={player.name} avatar={personaFor(player.id, player.world).avatar} onBack={back} /><WorldProgress streak={player.streak} storyDay={preview?stageDay:undefined} boosterCount={preview?String(play.boosters+(play.energyBoosters?.length??0)):'—'} onStreak={()=>setOverlay('streak')} onGift={play.enabled?openSeriesGift:undefined} giftClaimed={storyInventory.loginGiftDays?.includes(giftDay)} onBoosters={preview?()=>openResource('boosters'):undefined}/><section className="section profile-overview"><div className="profile-audience"><ChannelAudience value={player.subs}/>{receipt?.worldId===player.world&&receipt.audienceKnown!==false&&<p className="caption last-turn-audience">{receipt.result.delta>0?'+':receipt.result.delta<0?'−':''}{Math.abs(receipt.result.delta)} за последний ход</p>}</div><GoalCard goal={world.goal} onClick={() => go('goals')} /><PracticeFocus skills={personaFor(player.id,player.world).skills??[]}/></section>{preview&&curriculum&&<section className="section"><h2 className="section-title">Твой путь</h2><StoryMap preview={preview} campaign={curriculum} actors={world.cast} state={play} currentId={currentQuest?.id} onSelect={id=>openQuest(id)}/></section>}{preview?<StoryWallet state={play} labels={storyLabels} onEnable={() => updateStory(v=>({...v,enabled:true}))} onSpend={points=>updateStory(v=>spendStoryXP(v,points))}/>:<CurrentSkills skills={player.skills} labels={activeRole?storyLabels:['Смелость','Юмор','В тему']} roleSkills={activeRole?.skills} values={player.roleSkills}/>}<section className="section"><Button variant="secondary" onClick={()=>setOverlay('exit')}>Выйти из мира</Button></section></>;
  else if (screen === 'character' && person) content = <><ProfileHeader name={person.name} avatar={person.avatar} onBack={back} /><div className="character-bio"><ChannelAudience value={person.subscribers}/><span className="caption muted">@{person.handle} · AI-персонаж</span><p>{person.boundary || person.role}</p>{person.relation !== undefined && <p className="caption muted">Отношения: {person.relation > 0 ? "+" : ""}{person.relation}</p>}</div>{feed.filter(item => item.author === person.name).map(item => { const view = feedView(item, playerView(player!)); return card(view.post, view.author, view.count); })}<div className="section"><Button onClick={() => { setReply(undefined); setQuote({ name: person.name, text: person.boundary || person.role }); go('topic'); }}>О чём поговорить</Button></div></>;
  else if (screen === 'topic' && world) content = <><div className="topic-header"><button className="back-button" onClick={back}>Назад</button><img className="topic-cover" src={world.hero} alt="" /><h1>{quote?.name ?? world.title}</h1><p>{quote?.text ?? world.topic}</p></div><section className="topic-people">{world.cast.map(p => <PersonRow key={p.id} person={p} detail={p.relation === undefined ? undefined : `Отношения: ${p.relation > 0 ? "+" : ""}${p.relation}`} onClick={() => showPerson(p)} />)}<Button className="full" onClick={() => openCompose(undefined, quote)}>Написать</Button></section></>;
  else if (screen === 'messages' && world) content = <><ScreenHeading title="Личка" /><EmptyState title="Сначала — общий разговор" action={<Button onClick={() => go('feed')}>Вернуться в ленту</Button>}>Личные сообщения скоро появятся.</EmptyState><section className="section"><h2 className="section-title">Собеседники</h2>{world.cast.map(p => <PersonRow key={p.id} person={p} detail={p.relation === undefined ? undefined : `Отношения: ${p.relation > 0 ? "+" : ""}${p.relation}`} onClick={() => showPerson(p)} />)}</section></>;
  else if (screen === 'activity') content = <><ScreenHeading title="Уведомления" action={worldActivities.length > 0 && <button className="link-button caption" onClick={() => { const next = markWorldActivitiesRead(activities, player?.world); setActivities(next); localSave('activity', next); }}>Прочитать всё</button>} />{!worldActivities.length && <EmptyState title="Пока тихо">Здесь появятся реакции персонажей и доступные упоминания твоего канала.</EmptyState>}{worldActivities.map(a=><ReactionTicker key={a.id} inline read={a.read} notice={{id:a.id,postId:a.id,worldId:a.worldId,kind:a.kind??'reaction',actorName:a.actorName??a.title,text:a.text,relation:a.relation}} onOpen={()=>void openThread(a.id)} onClose={()=>{const next=activities.map(v=>v.id===a.id?{...v,read:true}:v);setActivities(next);localSave('activity',next);}}/>)}</>;
  else if (screen === 'account') content = <><ProfileHeader name={player?accountIdentity(player.id,owned[0]?.name??player.name).name:'Твой профиль'} neutralAvatar handle={player?accountIdentity(player.id,owned[0]?.name??player.name).handle:undefined} label="Профиль" /><section className="account-worlds"><h2 className="section-title">Мои миры</h2>{owned.length ? owned.slice(0,accountVisible).map(d => <ResumeWorld key={d.id} world={ownedView(d)} subtitle={d.name} busy={busy} onClick={() => void openWorld(ownedView(d))} />) : <Button onClick={() => go('catalog')}>Выбрать мир</Button>}{owned.length>accountVisible&&<Button variant="secondary" onClick={()=>setAccountVisible(n=>n+5)}>Показать ещё {Math.min(5,owned.length-accountVisible)}</Button>}</section><section className="account-settings"><SettingsRow label="Ресурсы" value={`${player?.postsLeft ?? '—'} энергии`} onClick={() => openResource('resources')} /><SettingsRow label="Размер текста" value={`${textScale * 100}%`} onClick={() => setOverlay('text-size')} /><SettingsRow label="Интересы" value={qualities.join(' · ') || 'Выбрать'} onClick={() => { interestOnly.current = true; go('onboarding'); }} /><SettingsRow label="О новой версии" onClick={() => setOverlay('about')} /><SettingsRow label="Удалить аккаунт" onClick={()=>setDeleteOpen(true)} disabled={pending || busy} /><NominalSignOut onSignOut={beginEntry} disabled={pending || busy} /></section></>;
  else content = <EmptyState title="Выбери мир" action={<Button onClick={() => go('catalog')}>К мирам</Button>}>Продолжи сохранённую историю или начни новую.</EmptyState>;

  const inWorld = !!world && !['catalog', 'search', 'account', 'intro', 'onboarding', 'welcome', 'age', 'hero'].includes(screen);
  const showNav = !booting && !(storyOnboarding && ['goals','story-skills'].includes(screen)) && ['catalog', 'search', 'account', 'feed', 'goals', 'story-skills', 'messages', 'activity'].includes(screen);
  const nav = inWorld ? [ { id: 'feed', label: 'Лента', icon: 'feed' as const }, { id: 'goals', label: 'Цели', icon: 'goals' as const }, { id: 'compose', label: 'Написать', icon: 'compose' as const }, { id: 'activity', label: 'Уведомления', icon: 'activity' as const, badge: worldActivities.filter(a=>!a.read).length } ] : [ { id: 'catalog', label: 'Главная', icon: 'home' as const }, { id: 'search', label: 'Поиск', icon: 'search' as const }, { id: 'account', label: 'Профиль', icon: 'profile' as const } ];
  if(import.meta.env.DEV&&hash.startsWith('#result-review'))return <ResultReview/>;
  if (hash.startsWith('#system')) return <DesignSystem />;
  return <div className="v7-stage"><div ref={appRoot} className={`app ${showNav ? 'app--nav' : ''} ${screen === 'intro' ? 'app--prologue' : ''} ${screen === 'hero' ? 'app--hero' : ''}`}><div className="app-body" ref={main}><div className="sr-only" tabIndex={-1} ref={focusHeading}>{titleOf[screen]}</div>{storageError && <Notice title="Не удалось сохранить на устройстве" tone="error">Не закрывай вкладку: черновик остаётся в памяти до перезагрузки.</Notice>}{error && <div className="section"><Notice title={screen==='thread'&&reply&&unknown?'Комментарий сохранён, ответ пока не готов':'Не получилось завершить действие'} tone="error">{error}</Notice>{!booting && !templates.length && <Button onClick={() => void boot()}>Повторить</Button>}</div>}{content}</div>
    {showNav && <BottomNav items={nav.map(n => ({ ...n, disabled: pending }))} active={screen === 'story-skills' ? 'goals' : screen} onChange={id => { tap('tab', { to: `v7.${id}` }); id === 'compose' ? (curriculum && playMode==='story' ? openFirstPost() : openCompose()) : go(id as Screen); }} />}
    {deleteOpen&&<AccountDeletion onDelete={deleteAccount} onClose={()=>setDeleteOpen(false)}/>}
    {reportId&&<ReportComment onSend={reason=>api.report(reportId,reason)} onClose={()=>setReportId(undefined)}/>}
    {showReceipt&&receipt&&receipt.worldId===player?.world&&<ResultIsland receipt={receipt} onRead={()=>void openThread(receipt.result.postId)} onClose={()=>setShowReceipt(false)}/>}
    {reactionNotice&&!showReceipt&&<ReactionTicker notice={reactionNotice} onOpen={()=>{void openThread(reactionNotice.postId);setReactionNotice(undefined);}} onClose={()=>setReactionNotice(undefined)}/>}
    {giftReveal&&<div className="gift-reveal" role="status"><RewardArt kind="gift"/><strong>{giftReveal}</strong><small>Подарок серии · локальная проба</small></div>}
    {gainAnimation&&<FollowersFlight delta={gainAnimation.delta} nonce={gainAnimation.nonce}/>}
    {rewardAnimation && <XPFlight amount={rewardAnimation.amount} nonce={rewardAnimation.nonce} />}
    {overlay && <Dialog title={{ exit: 'Выйти из мира?', streak: 'Серия дней', map: 'Карта мира', skills: 'Навыки и отношения', 'text-size': 'Размер текста', 'local-like': 'Реакция', quote: 'Цитата', about: 'Новая версия' }[overlay]} onClose={() => setOverlay(null)}>
      {overlay === 'streak' ? <><StreakDetails preview={preview} streak={player!.streak}/>{play.enabled&&<SeriesGiftCard inventory={storyInventory} onClaim={openSeriesGift}/>}</> : overlay === 'map' && curriculum ? <StoryMap preview={preview} campaign={curriculum} actors={world?.cast??[]} state={play} currentId={currentQuest?.id} onSelect={id => { setOverlay(null); openQuest(id); }}/> : overlay === 'exit' ? <><p>Публикации и черновик сохранятся. Вернёшься с того же места.</p><Button disabled={pending} onClick={() => { feedRequest.current++; setWorld(undefined); setScreen('catalog'); setStack([]); setOverlay(null); }}>Выйти к мирам</Button><Button variant="secondary" onClick={() => setOverlay(null)}>Остаться</Button></> : overlay === 'text-size' ? <><div className="row" style={{ flexWrap: 'wrap' }}>{[1, 1.25, 1.5, 2].map(v => <Chip key={v} selected={textScale === v} onClick={() => setTextScale(v)}>{v * 100}%</Chip>)}</div><p>Так будет выглядеть текст в приложении.</p><Button onClick={() => setOverlay(null)}>Готово</Button></> : overlay === 'skills' ? <><p>Навыки растут после подтверждённых сервером действий. Высокий навык не гарантирует согласия собеседника.</p>{world?.cast.map(p => <PersonRow key={p.id} person={p} onClick={() => { setOverlay(null); showPerson(p); }} />)}</> : overlay === 'local-like' ? <><p>Реакция сохранена на этом устройстве. На отношения и награды она пока не влияет.</p><Button onClick={() => setOverlay(null)}>Понятно</Button></> : overlay === 'quote' ? <><QuoteBlock name={quote?.name ?? ''} text={quote?.text ?? ''} /><p className="caption muted">Связь цитирования пока не поддерживается. Можно взять текст как контекст нового поста.</p><Button onClick={() => { setOverlay(null); openCompose(undefined, quote); }}>Написать с этим контекстом</Button></> : <><p>Миры, публикации, ответы и обмен кристаллов на энергию работают с сервером. Прогресс нового сюжета пока не подключён.</p><p className="caption muted">Уведомления и реакции сохраняются на этом устройстве.</p><Button onClick={() => setOverlay(null)}>Понятно</Button></>}
    </Dialog>}
  </div></div>;
}

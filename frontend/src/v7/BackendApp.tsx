import {ActivityItem} from './ui/ActivityItem';
import {InviteFriend} from './screens/InviteFriend';
import {PostIdeas,ideasForWorld} from './screens/PostIdeas';
import {LinkProfile} from '../app/LinkProfile';
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
import { worlds, worldDefinitionOf, worldDocumentOf, installWorldContent } from '../api/worldContent';
import { ageBand as ageBandOf } from '../app/ageBand';
import { AgeEntry } from './screens/AgeEntry';
import { rankCatalog } from './interests';
import { worldArt } from '../app/worldArt';
import './portraits.css';
import './social/social.css';
import {ChannelAudience,formatAudience} from './social/ChannelAudience';
import {ReactionTicker,type ReactionNotice} from './social/ReactionTicker';
import {ResultReview} from './publication/ResultReview';
import {parseQuestOutcome} from './publication/outcomes';
import {FollowersFlight} from './publication/RewardMotion';
import { ResourcePages, type ResourcePageName } from './screens/ResourcePages';
import { StoryBrief } from './story/StoryBrief';
import {RewardArt} from './ui/RewardArt';
import {ResultIsland} from './publication/ResultIsland';
import { PublicationWaiting, PublicationResult, type PublicationReceipt } from './publication/PublicationCards';
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
import { WorldProgress, StreakDetails, dayCount } from './screens/WorldResources';
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

type Screen = 'welcome' | 'age' | 'catalog' | 'search' | 'intro' | 'onboarding' | 'hero' | 'feed' | 'goals' | 'compose' | 'thread' | 'topic' | 'profile' | 'character' | 'messages' | 'activity' | 'account' | ResourcePageName;
type ReplyTarget = { postId: string; commentId: string | null; name: string; text: string; authorId:string };
type ReplyDraft = { text: string; requestId?: string; submittedText?: string; beforeSubscribers?:number; beforeSkills?:Player['skills']; roleBefore?:Player['roleSkills']; questId?:string; additionalStarId?:string };
const statusLabels: Record<string, string> = { pending: 'Персонажи читают публикацию…', ready_for_moderation: 'Готовим результат…', completed: 'Публикация готова', failed: 'Публикация не отправлена' };
const titleOf: Record<Screen, string> = { catalog: 'Миры', search: 'Поиск', intro: 'Вход в мир', onboarding: 'Знакомство', welcome: 'Знакомство', age: 'Возраст', hero: 'Ваш персонаж', feed: 'Лента', goals: 'Цели', compose: 'Новая публикация', thread: 'Обсуждение', topic: 'Тема', profile: 'Профиль персонажа', character: 'Собеседник', messages: 'Личка', activity: 'Уведомления', account: 'Профиль', resources:'Энергия',crystals:'Кристаллы' };

const walletClient={read:()=>api.energyWallet(),exchange:()=>api.exchangeEnergy()};

export default function BackendApp() {
  const [linkProfileOpen, setLinkProfileOpen] = useState(false);
  const [inviteOpen,setInviteOpen]=useState(false);
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
  const [overlay, setOverlay] = useState<'exit' | 'streak' | 'skills' | 'text-size' | 'local-like' | 'quote' | 'about' | null>(null);
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
  const [roleScenarioSelected, setRoleScenarioSelected] = useState(false);
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
  const [receipt, setReceipt] = useState<PublicationReceipt>();
  const [showReceipt,setShowReceipt]=useState(false);
  const [publicationFailed, setPublicationFailed] = useState(false);
  const [prepared, setPrepared] = useState<{ title: string; prompt: string; questId?: string }>();
  const [storyOnboarding,setStoryOnboarding] = useState(false);
  const [energyNudge,setEnergyNudge] = useState(0);
  const cancel = useRef<(() => void) | undefined>(undefined);
  const sending = useRef(false);
  const main = useRef<HTMLDivElement>(null);
  const appRoot = useRef<HTMLDivElement>(null);
  useScrollEdges(appRoot);
  const focusHeading = useRef<HTMLDivElement>(null);
  const feedRequest = useRef(0);
  const displayedWorld = useRef(world?.id);
  displayedWorld.current = world?.id;
  const threadRequest = useRef(0);
  const interestOnly = useRef(false);
  useEffect(() => { if (player?.experiments) setExperiments(player.experiments); }, [player?.experiments]);
  const key = `reki:v7:${player?.id ?? api.playerId() ?? 'guest'}`;
  const localSave = (name: string, value: unknown) => { if (!saveLocal(`${key}:${name}`, value)) setStorageError(true); };
  const go = (next: Screen) => { setError(''); setStack(s => [...s, screen]); setScreen(next); setToast(undefined); setReactionNotice(undefined); };
  const openResource = (next: ResourcePageName) => {
    void accountEnergy.refresh();
    if(['resources','crystals'].includes(screen)){setScreen(next);setError('');}else go(next);
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

  const recordComments = (postId:string, worldId:string, comments:ServerPost['comments']) => {
    const confirmed=comments.filter(c=>!c.me&&!c.hidden&&(c.star||c.authorId?.startsWith('Star_')));
    if(!confirmed.length)return;
    setActivities(old=>{
      const fresh=confirmed.filter(c=>!old.some(a=>a.id===`comment:${c.id}`)).map(c=>({id:`comment:${c.id}`,postId,worldId,title:`${c.author} отвечает`,actorName:c.author,text:c.text,read:false,kind:'reaction' as const}));
      if(!fresh.length)return old;
      const next=[...fresh.reverse(),...old].slice(0,100);localSave('activity',next);return next;
    });
  };
  useEffect(()=>{
    if(!player||player.streak.days<1)return;
    const id=`streak:${player.world}:${player.streak.days}`;
    setActivities(old=>{
      if(old.some(a=>a.id===id))return old;
      const days=player.streak.days;
      const next=[{id,worldId:player.world,title:days===1?'Первый день серии':`Серия: ${days} дней`,text:days===1?'Начало серии отмечено. Продолжи завтра постом или комментарием.':'Твоя серия продолжается.',kind:'streak' as const,read:false},...old].slice(0,100);
      localSave('activity',next);return next;
    });
  },[player?.id,player?.world,player?.streak.days]);
  useEffect(()=>{
    if(screen!=='activity'||!player)return;
    let active=true;
    const ownPosts=feed.filter(p=>p.postId&&p.author===player.name);
    const likes=ownPosts.map(p=>({postId:p.postId!,count:p.reactions?.reduce((sum,r)=>sum+r.count,0)??0})).filter(p=>p.count>0);
    setActivities(old=>{
      const updates=likes.filter(p=>!old.some(a=>a.id===`likes:${p.postId}`&&a.count===p.count)).map(p=>({id:`likes:${p.postId}`,postId:p.postId,worldId:player.world,title:'Реакции на твою публикацию',text:`Сейчас реакций: ${p.count}.`,count:p.count,kind:'likes' as const,read:false}));
      if(!updates.length)return old;
      const next=[...updates,...old.filter(a=>!updates.some(u=>u.id===a.id))].slice(0,100);localSave('activity',next);return next;
    });
    void Promise.all(ownPosts.filter(p=>(p.commentCount??0)>0).slice(0,20).map(async item=>{
      try{const post=await api.post(item.postId!);if(active)recordComments(post.id,player.world,post.comments);}catch{/* The list remains usable when a post cannot be loaded. */}
    }));
    return()=>{active=false;};
  },[screen,player?.world]);
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
  const storyView = (doc: WorldDocument) => {
    const view = worldView(doc);
    const persona = player ? personaFor(player.id, doc.id) : {};
    const role = roleForWorld(doc, persona);
    const goal = role?.goal || persona.goal || doc.firstContext.split('\nЦель персонажа: ')[1];
    return goal ? {...view, goal} : view;
  };
  const ownedView = (doc: WorldDocument) => storyView(doc);
  const installPlayer = (p: Player) => { setPlayer(p); const doc = worldDocumentOf(p.world); setWorld(storyView(doc));
    const draft = readPostDraft(draftKey(p));
    const savedReceipt = readLocal<PublicationReceipt | undefined>(`reki:v7:${p.id}:result:${p.world}`, undefined);
    setReceipt(receiptForRelease(savedReceipt ? { ...savedReceipt, worldId: p.world } : undefined,false));
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
  const activeRole = player ? roleForWorld(worldDocumentOf(player.world),personaFor(player.id,player.world)) : undefined;
  const roleTask = activeRole ? (player?.campaignProgress?.completed ? undefined : activeRole.tasks.find(t=>t.id===player?.campaignProgress?.taskId)??activeRole.tasks[0]) : undefined;
  const campaignProgress = player?.campaignProgress?.percent;
  const storyLabels = activeRole?.skills.map(skill => skill.name) ?? ['Смелость','Юмор','В тему'];
  const openWorld = async (w: World, storyVariant = false) => {
    if (sending.current || pending || busy) return;
    if (owned.some(d => d.id === w.id)) { await enter(w); return; }
    setIntroStep(0); setSelected(w); setSelectedVariant(storyVariant); go('intro');
  };
  const startHero = (w: World) => {
    const def = worldDefinitionOf(w.id); setHeroBase(def); setDefinition(def); setHeroChoice(null); setRoleScenarioSelected(!!scenarioForTemplate(def.templateId) && (def.templateId !== 'aec9494b-4118-4525-8458-e85d759c8dd9' || selectedVariant)); setHeroGoal(''); setHeroSkills([]); setHeroAvatar(undefined); setHeroSeed(def.avatar); setAvatarRemoved(false); go('hero');
  };
  const chooseHero = (choice?: { id: string }) => {
    if (!definition || !heroBase) return;
    setHeroChoice(choice?.id ?? 'custom');
    setHeroGoal((roleScenarioSelected ? scenarioForTemplate(heroBase.templateId)?.roles.find(r=>r.id===(choice?.id??'custom'))?.goal : undefined) || selected?.goal || '');
    setHeroAvatar(undefined); setHeroSeed(choice?.id ?? crypto.randomUUID()); setAvatarRemoved(false);
    if (roleScenarioSelected) { setDefinition(definitionForRole(heroBase, choice?.id ?? 'custom')); return; }
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
  const beginEntry = async () => {
    if(sending.current||busy)return;
    setBusy(true);setError('');
    try{await api.logout();}catch{setError('Не удалось выйти. Попробуй ещё раз.');}finally{setBusy(false);}
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
      const role = roleScenarioSelected ? scenarioForTemplate(definition.templateId)?.roles.find(r=>r.id===heroChoice) : undefined;
      const custom = !heroChoice || heroChoice==='custom';
      const portrait = custom&&avatarRemoved ? '' : (custom?heroAvatar:undefined) || await heroPortrait(heroChoice ?? 'custom', heroSeed || definition.avatar, definition.templateId || created.world, definition.avatarStyle).catch(() => undefined);
      if (!savePersona(created.id, created.world, { goal: role?.goal??heroGoal.trim(), skills: role?.skills.map(s=>s.name)??heroSkills, ...(role?{roleId:role.id,templateId:definition.templateId!}:{}), avatar: portrait })) setStorageError(true);
      accountIdentity(created.id,owned[0]?.name??created.name);
      const p = installPlayer(created); trackEvent('onboarding_step', { step: 'done', ok: true, world: p.world }); localSave('qualities', qualities);
      const own = await api.ownedWorlds(); installWorldContent(own); setOwned(own);
      setStoryOnboarding(true); setScreen('goals'); setStack([]); await loadFeed(p);
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
  const openFirstPost = () => {
    setStoryOnboarding(false);
    openCompose();
    if (roleTask) setPrepared({title: roleTask.context, prompt: roleTask.prompt, questId: roleTask.id});
  };
  const updateDraft = (text: string) => {
    setDraft(text);
    if (!player) return;
    if (reply) { const saved = readLocal<ReplyDraft>(replyKey(reply), { text: '' }); if (!saveLocal(replyKey(reply), { ...saved, text })) setStorageError(true); }
    else { const k = draftKey(player); if (!savePostDraft(k, { ...readPostDraft(k), text, ownWords: true })) setStorageError(true); }
  };
  const openThread = async (id: string) => {
    if (!id || pending) return;
    const request = ++threadRequest.current; setPost(undefined); setThreadLoading(true); go('thread');
    try { const p = await api.post(id); if (threadRequest.current === request) { setPost(p); const next = activities.map(a => a.id === id || a.postId === id ? { ...a, read: true } : a); setActivities(next); localSave('activity', next); } }
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
    const fallback={skills:player.skills,roleSkills:player.roleSkills,questId:prepared?.questId??roleTask?.id};
    if(!recovery)saveLocal(beforeKey,fallback);
    const turnBefore=readLocal(beforeKey,fallback);
    if (!recovery) trackEvent('post_publish', { len: text.length, own_words: command.ownWords });
    if (!savePostDraft(k, { ...saved, text: draft, publicationId: id, submittedText: text, ownWords: command.ownWords, submittedOwnWords: command.ownWords })) setStorageError(true);
    sending.current = true; setPending(true); setUnknown(false); setPublicationFailed(false); setError(''); setStatus('pending'); setScreen('feed'); setStack([]);
    let publishedPostId='';
    cancel.current = api.publish(text, id, event => {
      if (event.type === 'comment') {
        recordComments(publishedPostId,player.world,[event.comment]);
        if (displayedWorld.current === player.world) void loadFeed(player);
        return;
      }
      if (event.type !== 'result') return;
      publishedPostId=event.result.postId;
      trackEvent('post_result', { delta: event.result.delta, viral: event.result.viral, flop: event.result.flop });
      completePostDraft(k, id, text); setDraft(readPostDraft(k).text);
      const serverOutcome=parseQuestOutcome((event.result as unknown as {questOutcome?:unknown}).questOutcome);

      const value: PublicationReceipt = { worldId: player.world, questId:turnBefore.questId, result: event.result, before: turnBefore.skills, roleBefore:turnBefore.roleSkills, roleLabels:activeRole?.skills.map(s=>s.name), questOutcome:serverOutcome??({status:'unverified',source:'server',title:roleTask?.context??'Твоя запись',reason:'Пост опубликован, но серверный исход квеста пока не возвращается.'}) };
      setReceipt(value);setShowReceipt(true); saveLocal(`${key}:result:${player.world}`, value);
      if(value.questId)saveLocal(`${key}:task-result:${player.world}:${value.questId}`,value);
      setToast(event.result); setGainAnimation({delta:event.result.delta,nonce:event.result.postId});
      const best=event.result.bestComment;
      const actor=best?world?.cast.find(p=>p.handle===best.avatar||p.id===best.avatar):undefined;
      if(best&&actor){const notice:ReactionNotice={id:event.result.postId,postId:event.result.postId,worldId:player.world,kind:'reaction',actorName:actor.name,text:best.text,relation:actor.relation};setReactionNotice(notice);}
      setPublicationFailed(false); setUnknown(false); setPending(false); sending.current = false; setStatus('completed');
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
    saveLocal(k,{...saved,text:draft,requestId,submittedText:text,beforeSubscribers:saved.beforeSubscribers??player.subs,beforeSkills:saved.beforeSkills??player.skills,roleBefore:saved.roleBefore??player.roleSkills,questId:saved.questId??roleTask?.id});
    sending.current=true;setPending(true);setError('');setReceipt(undefined);setStatus('Сохраняем комментарий…');
    let snapshot:Awaited<ReturnType<typeof api.reply>>|undefined,completed=false;
    try{
      const controller=new AbortController();cancel.current=()=>controller.abort();
      snapshot=await api.reply(target.postId,target.commentId,requestId,text,target.authorId,{
        signal:controller.signal,additionalStarId:saved.additionalStarId,
        onSaved:value=>{recordComments(target.postId,player.world,value.post.comments);const first=!snapshot;snapshot=value;if(first)void accountEnergy.refresh();setPost({...value.post,id:value.post.postId});setPlayer(p=>p?{...p,postsLeft:value.energyLeft,subs:value.subscribers}:p);setScreen('thread');setStack(v=>v.filter(x=>x!=='compose'));},
        onTyping:authorId=>setStatus(authorId?`${world?.cast.find(p=>`Star_${p.id}`===authorId)?.name??'Персонаж'} печатает…`:''),
        onAdditionalStar:id=>saveLocal(k,{...readLocal<ReplyDraft>(k,{text}),additionalStarId:id??undefined}),
      });
      completed=true;saveLocal(k,{text:''});setDraft('');setReply(undefined);setUnknown(false);setScreen('thread');setStack(v=>v.filter(x=>x!=='compose'));
    }catch(cause){
      if(snapshot){setUnknown(true);setError('Комментарий сохранён. Ответ персонажа пока не готов — можно продолжить получение с той же попыткой.');}
      else{const rejected=cause instanceof ApiError&&cause.status>=400&&cause.status<500&&cause.status!==409;if(rejected)saveLocal(k,{text:draft});setUnknown(!rejected);setError(rejected?'Комментарий не сохранён. Текст оставлен для исправления.':errorText(cause)+' Текст и попытка сохранены.');}
    }finally{
      if(snapshot){

        const delta=snapshot.subscribers-(saved.beforeSubscribers??player.subs);
        const value:PublicationReceipt={worldId:player.world,questId:saved.questId??roleTask?.id,actionKind:'comment',audienceKnown:true,before:saved.beforeSkills??player.skills,roleBefore:saved.roleBefore??player.roleSkills,roleLabels:activeRole?.skills.map(s=>s.name),questOutcome:{status:'unverified',source:'server',title:'Твой комментарий',reason:completed?'Комментарий и реакция сохранены. Серверная оценка квеста пока не возвращается.':'Комментарий сохранён. Получение реакции ещё не завершено.'},result:{postId:target.postId,delta,headline:'Комментарий сохранён',whys:[],ribbons:[],viral:false,flop:false,views:snapshot.post.views}};
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
      <ResourceBadge type="crystal" value={accountEnergy.wallet?String(accountEnergy.wallet.crystals):'—'} hint={accountEnergy.wallet?"Кристаллы для обмена на энергию":"Баланс кристаллов пока недоступен"} onClick={() => openResource('crystals')} />
      <button className="profile-access campaign-avatar" aria-label={`Профиль персонажа. История: ${campaignProgress===undefined?'прогресс уточняется':`${campaignProgress}%`}`} onClick={() => go('profile')} style={{background:`conic-gradient(var(--campaign-ring) ${campaignProgress??0}%,var(--color-divider) 0)`}}><Avatar name={player?.name ?? ''} src={player ? personaFor(player.id, player.world).avatar : undefined} size="lg" /></button></header>
    {player&&<button className="hud-streak" onClick={()=>setOverlay('streak')} aria-label={`Серия: ${player.streak.days}. Посмотреть дни`}>🔥 <strong>{dayCount(player.streak.days)}</strong><span>в серии</span></button>}
    </div>
    {player?.postsLeft===0&&draft.trim()&&!pending&&<section className="section"><div className="energy-draft-notice"><strong>Черновик ждёт энергии</strong><blockquote className="energy-draft-text">{draft}</blockquote><p>Текст сохранён. Нажми на энергию вверху, чтобы посмотреть восстановление и обмен.</p><Button variant="secondary" onClick={()=>reply?openCompose(reply):openFirstPost()}>Вернуться к черновику</Button></div></section>}
    {!reply && (pending || unknown || publicationFailed) && <PublicationWaiting pending={pending} unknown={unknown} text={draft} status={status} onStop={stopWaiting} onContinue={() => { if (unknown) publish(); else openCompose(); }} />}
    {feedError && <div className="section"><Notice title="Не удалось обновить ленту" tone="error">{feedError}</Notice></div>}
    {!feed.length && !feedLoading && !feedError && !pending && !unknown && !publicationFailed && <EmptyState title="Первое слово за тобой" action={<Button onClick={() => openCompose()}>Написать пост</Button>}>{world?.seedPost || 'Расскажи свою версию истории. Здесь появятся публикации и ответы.'}</EmptyState>}
    {feed.map(item => { const view = feedView(item, playerView(player!)); return card(view.post, view.author, view.count); })}
    {(feed.length > 0 || feedError || feedLoading) && (!pending || feed.length > 0) && <div className="section feed-footer"><Button variant="ghost" busy={feedLoading} onClick={() => void loadFeed(player!, cursor ?? undefined)}>{feedLoading ? 'Загружаем ленту' : cursor ? 'Показать ещё' : 'Обновить ленту'}</Button></div>}
  </>;

  const worldActivities = activitiesForWorld(activities, player?.world).filter(a=>!!a.kind);
  const selectedStory = scenarioForTemplate(definition?.templateId);
  const heroOptions = definition ? roleScenarioSelected && selectedStory ? selectedStory.roles.map(role=>({id:role.id,name:role.id==='custom'?'Свой персонаж':role.name,role:role.name,goal:role.goal,motivation:'motivation' in role?role.motivation:undefined,skills:role.skills})) : [...(heroBase??definition).stars.map(s=>({id:s.id,name:s.name,role:s.role,goal:selected?.goal||''})),{id:'custom',name:'Свой персонаж',role:'Твоя история',goal:selected?.goal||''}] : [];
  let content: ReactNode;
  if (booting) content = <div className="loading-state" role="status"><LoadingIndicator/><p>Открываем твои миры</p></div>;
  else if (screen === 'catalog' || screen === 'search') {
    const story=scenarioForTemplate('aec9494b-4118-4525-8458-e85d759c8dd9')!;
    const entries = rankCatalog(withHogwartsCard(catalogEntries(templates, plannedWorlds), {templateId:story.templateId,scenario:story.title,premise:story.opening,subtitle:'Чьим будет будущее школы?'}).map(entry=>{const scenario=scenarioForTemplate(entry.world.id);return entry.available&&scenario&&(entry.storyVariant||scenario.templateId!==story.templateId)?{...entry,world:{...entry.world,title:scenario.title,description:scenario.opening.split(/\n\s*\n/).slice(0,scenario.introParagraphCount).join('\n\n'),introTitles:scenario.openingTitles,tagline:scenario.opening.split('. ')[0]}}:entry;}), qualities);
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
  else if (screen === 'hero' && definition && definition.templateId) content = <><HeroStudio onBack={back} startWithHero={selectedStory?.startWithHero}
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
  else if (screen === 'goals' && world && player) {
    const doc = worldDocumentOf(player.world);

    const task = roleTask ?? (activeRole ? undefined : {title:world.goal,prompt:doc.postIdeas[0] || doc.firstPrompt || doc.firstContext,context:doc.firstContext});
    const savedDraft=readPostDraft(draftKey(player));
    const taskReceipt=receiptForRelease((receipt?.questId===(roleTask??roleTask)?.id?receipt:undefined)??readLocal<PublicationReceipt|undefined>(`${key}:task-result:${player.world}:${(roleTask??roleTask)?.id}`,undefined)??(receipt?.worldId===player.world&&((roleTask&&receipt.questId===roleTask.id)||receipt.questId===roleTask?.id||receipt.questOutcome?.title===task?.title)?receipt:undefined),false);
    content = <WorldBrief roleSkills={activeRole?.skills} campaignProgress={campaignProgress} campaignComplete={player.campaignProgress?.completed} practiceSkills={personaFor(player.id,player.world).skills??[]} world={world} player={player} avatar={personaFor(player.id, player.world).avatar} task={task} firstVisit={storyOnboarding} hasDraft={savedDraft.ownWords&&!!savedDraft.text.trim()} receipt={taskReceipt} onRead={taskReceipt?()=>void openThread(taskReceipt.result.postId):undefined} onWrite={openFirstPost} onFeed={() => { setStoryOnboarding(false); go('feed'); }} />;
  }
  else if (screen === 'goals' && world) content = <><ScreenHeading title="Цели" subtitle={world.title} /><StoryBrief goal={world.goal || 'Найти свой голос и начать разговор.'} /><section className="section"><Button onClick={openFirstPost}>Написать пост →</Button></section></>;
  else if (screen === 'resources' || screen === 'crystals') content = <ResourcePages wallet={accountEnergy.wallet} exchangeBusy={accountEnergy.busy} exchangeMessage={accountEnergy.message} onExchange={()=>void accountEnergy.exchange()} onWalletRefresh={()=>void accountEnergy.refresh()} page={screen} energy={player?.postsLeft} onPage={openResource} onBack={back}/>;
  else if (screen === 'compose' && player) content = <>
    <ScreenHeading title={reply ? 'Продолжить разговор' : 'Новая публикация'} back={back} />
    <section className="section stack composer-body">
      {prepared && <TaskCard title={prepared.title} prompt={prepared.prompt}/>}
      <PracticeFocus skills={personaFor(player.id, player.world).skills ?? []}/>
      {(reply || quote) && !prepared && <QuoteBlock name={reply?.name ?? quote!.name} text={reply?.text ?? quote!.text}/>}
      <div className="row"><Avatar name={player.name} src={personaFor(player.id, player.world).avatar}/><strong>{player.name}</strong></div>
      <label className="sr-only" htmlFor="post-text">Текст публикации</label>
      <textarea id="post-text" className="composer-text" value={draft} onChange={e=>updateDraft(e.target.value)} disabled={pending || unknown} maxLength={reply ? 1000 : 2000} autoFocus={!!reply}/>
      <div className="composer-tools"><span className="caption muted">{draft.length} / {reply ? 1000 : 2000}</span><button className="link-button caption" disabled={!draft||pending||unknown} onClick={()=>updateDraft('')}>Очистить {reply?'ответ':'пост'}</button></div>
      {!reply&&<PostIdeas ideas={ideasForWorld(worldDocumentOf(player.world))} draft={draft} onDraft={updateDraft} disabled={pending||unknown}/>}
      {quote && !reply && !prepared && <p className="caption muted">Контекст помогает написать пост. Связь цитирования на сервере пока недоступна.</p>}
      {pending && <div className="pending-line" role="status"><LoadingIndicator/>{statusLabels[status] ?? 'Отправляем ответ…'}</div>}
      {pending && <Button variant="secondary" onClick={stopWaiting}>Остановить ожидание</Button>}
      <Button className="full" size="lg" disabled={!draft.trim()} busy={pending} onClick={publish}>{unknown ? 'Проверить статус' : 'Опубликовать'} <span className="caption">· 1 энергия</span></Button>
    </section>
  </>;
  else if (screen === 'thread' && player && world) {
    const view = post ? detailView(post, world, playerView(player)) : undefined;
    const postAuthor = post && world.cast.find(p => `Star_${p.id}` === post.authorId);
    content = <><ScreenHeading title="Обсуждение" back={back} />{pending&&<div className="section stack" role="status"><div className="pending-line"><LoadingIndicator/>{status||'Получаем ответ персонажа…'}</div><Button variant="ghost" onClick={stopWaiting}>Остановить ожидание</Button></div>}{reply&&unknown&&!pending&&<section className="section"><Button onClick={()=>openCompose(reply)}>Продолжить получение ответа</Button><p className="caption muted">Сохранённый комментарий повторно не публикуется.</p></section>}{receipt&&receipt.worldId===player.world&&receipt.result.postId===post?.id&&<PublicationResult receipt={receipt} animateFollowers={!!gainAnimation} onRead={()=>document.querySelector('.thread-nested')?.scrollIntoView({block:'center'})}/>}{threadLoading && <div className="pending-line" role="status"><LoadingIndicator/>Загружаем обсуждение…</div>}{view && card(view.post, view.author, post!.comments.length)}{post?.comments.map(comment => {
      const author: Person = comment.me ? playerView(player) : world.cast.find(p => `Star_${p.id}` === comment.authorId) ?? { id: comment.authorId ?? comment.id, name: comment.author, handle: comment.avatar ?? '', role: comment.role ?? '', boundary: '' };
      return <div key={comment.id} className={comment.parentCommentId ? 'thread-nested' : ''}>{comment.parentCommentId && <span className="sr-only">Ответ на реплику</span>}{card({ id: comment.id, parentId: post.id, authorId: author.id, text: comment.hidden ? 'Комментарий скрыт' : comment.text, createdAt: '', likes: 0 }, author, 0, true)}{comment.star && !comment.hidden && world.cast.some(p => `Star_${p.id}` === comment.authorId) && <div className="reply-action"><Button variant="ghost" aria-label={`Ответить ${author.name}`} onClick={() => openCompose({ postId: post.id, commentId: comment.id, name: author.name, text: comment.text, authorId:`Star_${author.id}` })}>Ответить</Button></div>}</div>;
    })}{post && !post.comments.length && <EmptyState title="Здесь ещё нет ответов">Персонажи могут продолжить разговор после публикации.</EmptyState>}{postAuthor && post && <div className="section"><Button className="full" onClick={() => openCompose({ postId: post.id, commentId: null, name: postAuthor.name, text: post.text, authorId:`Star_${postAuthor.id}` })}>Начать разговор с автором</Button></div>}</>;
  } else if (screen === 'profile' && player && world) content = <><ProfileHeader name={player.name} avatar={personaFor(player.id, player.world).avatar} onBack={back} showStoryProgress storyProgress={campaignProgress} /><WorldProgress streak={player.streak} onStreak={()=>setOverlay('streak')}/><section className="section profile-overview"><div className="profile-audience"><ChannelAudience value={player.subs}/>{receipt?.worldId===player.world&&receipt.audienceKnown!==false&&<p className="caption last-turn-audience">{receipt.result.delta>0?'+':receipt.result.delta<0?'−':''}{Math.abs(receipt.result.delta)} за последний ход</p>}</div><GoalCard goal={world.goal} onClick={() => go('goals')} /><PracticeFocus skills={personaFor(player.id,player.world).skills??[]}/></section><CurrentSkills skills={player.skills} labels={activeRole?storyLabels:['Смелость','Юмор','В тему']} roleSkills={activeRole?.skills} values={player.roleSkills}/><section className="section"><Button variant="secondary" onClick={()=>setOverlay('exit')}>Выйти из мира</Button></section></>;
  else if (screen === 'character' && person) content = <><ProfileHeader name={person.name} avatar={person.avatar} onBack={back} /><div className="character-bio"><ChannelAudience value={person.subscribers}/><span className="caption muted">@{person.handle} · AI-персонаж</span><p>{person.boundary || person.role}</p>{person.relation !== undefined && <p className="caption muted">Отношения: {person.relation > 0 ? "+" : ""}{person.relation}</p>}</div>{feed.filter(item => item.author === person.name).map(item => { const view = feedView(item, playerView(player!)); return card(view.post, view.author, view.count); })}<div className="section"><Button onClick={() => { setReply(undefined); setQuote({ name: person.name, text: person.boundary || person.role }); go('topic'); }}>О чём поговорить</Button></div></>;
  else if (screen === 'topic' && world) content = <><div className="topic-header"><button className="back-button" onClick={back}>Назад</button><img className="topic-cover" src={world.hero} alt="" /><h1>{quote?.name ?? world.title}</h1><p>{quote?.text ?? world.topic}</p></div><section className="topic-people">{world.cast.map(p => <PersonRow key={p.id} person={p} detail={p.relation === undefined ? undefined : `Отношения: ${p.relation > 0 ? "+" : ""}${p.relation}`} onClick={() => showPerson(p)} />)}<Button className="full" onClick={() => openCompose(undefined, quote)}>Написать</Button></section></>;
  else if (screen === 'messages' && world) content = <><ScreenHeading title="Личка" /><EmptyState title="Сначала — общий разговор" action={<Button onClick={() => go('feed')}>Вернуться в ленту</Button>}>Личные сообщения скоро появятся.</EmptyState><section className="section"><h2 className="section-title">Собеседники</h2>{world.cast.map(p => <PersonRow key={p.id} person={p} detail={p.relation === undefined ? undefined : `Отношения: ${p.relation > 0 ? "+" : ""}${p.relation}`} onClick={() => showPerson(p)} />)}</section></>;
  else if (screen === 'activity') content = <><ScreenHeading title="Уведомления" action={worldActivities.length > 0 && <button className="link-button caption" onClick={() => { const next = markWorldActivitiesRead(activities, player?.world); setActivities(next); localSave('activity', next); }}>Прочитать всё</button>} />{!worldActivities.length && <EmptyState title="Пока тихо">Здесь появятся ответы персонажей, реакции на публикации и отметки серии.</EmptyState>}{worldActivities.map(a=><ActivityItem key={a.id} title={a.title} text={a.text} actorName={a.actorName} symbol={a.kind==='streak'?'🔥':a.kind==='likes'?'♡':'✦'} read={a.read} onOpen={()=>{const next=activities.map(v=>v.id===a.id?{...v,read:true}:v);setActivities(next);localSave('activity',next);if(a.kind==='streak')setOverlay('streak');else void openThread(a.postId??a.id);}}/>)}</>;
  else if (screen === 'account') content = <><ProfileHeader name={player?accountIdentity(player.id,owned[0]?.name??player.name).name:'Твой профиль'} neutralAvatar handle={player?accountIdentity(player.id,owned[0]?.name??player.name).handle:undefined} label="Профиль" /><section className="account-worlds"><h2 className="section-title">Мои миры</h2>{owned.length ? owned.slice(0,accountVisible).map(d => <ResumeWorld key={d.id} world={ownedView(d)} subtitle={d.name} busy={busy} onClick={() => void openWorld(ownedView(d))} />) : <Button onClick={() => go('catalog')}>Выбрать мир</Button>}{owned.length>accountVisible&&<Button variant="secondary" onClick={()=>setAccountVisible(n=>n+5)}>Показать ещё {Math.min(5,owned.length-accountVisible)}</Button>}</section><section className="account-settings">{player&&<SettingsRow label="Пригласить друга" onClick={()=>setInviteOpen(true)}/>} {player && <SettingsRow label="Привязать профиль" onClick={()=>setLinkProfileOpen(true)}/>}<SettingsRow label="Ресурсы" value={`${player?.postsLeft ?? '—'} энергии`} onClick={() => openResource('resources')} /><SettingsRow label="Размер текста" value={`${textScale * 100}%`} onClick={() => setOverlay('text-size')} /><SettingsRow label="Интересы" value={qualities.join(' · ') || 'Выбрать'} onClick={() => { interestOnly.current = true; go('onboarding'); }} /><SettingsRow label="О новой версии" onClick={() => setOverlay('about')} /><SettingsRow label="Удалить аккаунт" onClick={()=>setDeleteOpen(true)} disabled={pending || busy} /><NominalSignOut onSignOut={()=>void beginEntry()} disabled={pending || busy} /></section></>;
  else content = <EmptyState title="Выбери мир" action={<Button onClick={() => go('catalog')}>К мирам</Button>}>Продолжи сохранённую историю или начни новую.</EmptyState>;

  const inWorld = !!world && !['catalog', 'search', 'account', 'intro', 'onboarding', 'welcome', 'age', 'hero'].includes(screen);
  const showNav = !booting && !(storyOnboarding && ['goals'].includes(screen)) && ['catalog', 'search', 'account', 'feed', 'goals', 'messages', 'activity'].includes(screen);
  const nav = inWorld ? [ { id: 'feed', label: 'Лента', icon: 'feed' as const }, { id: 'goals', label: 'Цели', icon: 'goals' as const }, { id: 'compose', label: 'Написать', icon: 'compose' as const }, { id: 'activity', label: 'Уведомления', icon: 'activity' as const, badge: worldActivities.filter(a=>!a.read).length } ] : [ { id: 'catalog', label: 'Главная', icon: 'home' as const }, { id: 'search', label: 'Поиск', icon: 'search' as const }, { id: 'account', label: 'Профиль', icon: 'profile' as const } ];
  if(import.meta.env.DEV&&hash.startsWith('#result-review'))return <ResultReview/>;
  if (hash.startsWith('#system')) return <DesignSystem />;
  return <div className="v7-stage"><div ref={appRoot} className={`app ${showNav ? 'app--nav' : ''} ${screen === 'intro' ? 'app--prologue' : ''} ${screen === 'hero' ? 'app--hero' : ''}`}><div className="app-body" ref={main}><div className="sr-only" tabIndex={-1} ref={focusHeading}>{titleOf[screen]}</div>{storageError && <Notice title="Не удалось сохранить на устройстве" tone="error">Не закрывай вкладку: черновик остаётся в памяти до перезагрузки.</Notice>}{error && <div className="section"><Notice title={screen==='thread'&&reply&&unknown?'Комментарий сохранён, ответ пока не готов':'Не получилось завершить действие'} tone="error">{error}</Notice>{!booting && !templates.length && <Button onClick={() => void boot()}>Повторить</Button>}</div>}{content}</div>
    {showNav && <BottomNav items={nav.map(n => ({ ...n, disabled: pending }))} active={screen} onChange={id => { tap('tab', { to: `v7.${id}` }); id === 'compose' ? openFirstPost() : go(id as Screen); }} />}
    {inviteOpen&&<InviteFriend onClose={()=>setInviteOpen(false)}/>}
    {linkProfileOpen && player && <LinkProfile player={player} refresh={refresh} onClose={()=>setLinkProfileOpen(false)}/>}
    {deleteOpen&&<AccountDeletion onDelete={deleteAccount} onClose={()=>setDeleteOpen(false)}/>}
    {reportId&&<ReportComment onSend={reason=>api.report(reportId,reason)} onClose={()=>setReportId(undefined)}/>}
    {showReceipt&&receipt&&receipt.worldId===player?.world&&<ResultIsland receipt={receipt} onRead={()=>void openThread(receipt.result.postId)} onClose={()=>setShowReceipt(false)}/>}
    {reactionNotice&&!showReceipt&&<ReactionTicker notice={reactionNotice} onOpen={()=>{void openThread(reactionNotice.postId);setReactionNotice(undefined);}} onClose={()=>setReactionNotice(undefined)}/>}
    {gainAnimation&&<FollowersFlight delta={gainAnimation.delta} nonce={gainAnimation.nonce}/>}
    {overlay && <Dialog title={{ exit: 'Выйти из мира?', streak: 'Серия дней', skills: 'Навыки и отношения', 'text-size': 'Размер текста', 'local-like': 'Реакция', quote: 'Цитата', about: 'Новая версия' }[overlay]} onClose={() => setOverlay(null)}>
      {overlay === 'streak' ? <StreakDetails streak={player!.streak}/> : overlay === 'exit' ? <><p>Публикации и черновик сохранятся. Вернёшься с того же места.</p><Button disabled={pending} onClick={() => { feedRequest.current++; setWorld(undefined); setScreen('catalog'); setStack([]); setOverlay(null); }}>Выйти к мирам</Button><Button variant="secondary" onClick={() => setOverlay(null)}>Остаться</Button></> : overlay === 'text-size' ? <><div className="row" style={{ flexWrap: 'wrap' }}>{[1, 1.25, 1.5, 2].map(v => <Chip key={v} selected={textScale === v} onClick={() => setTextScale(v)}>{v * 100}%</Chip>)}</div><p>Так будет выглядеть текст в приложении.</p><Button onClick={() => setOverlay(null)}>Готово</Button></> : overlay === 'skills' ? <><p>Навыки растут после подтверждённых сервером действий. Высокий навык не гарантирует согласия собеседника.</p>{world?.cast.map(p => <PersonRow key={p.id} person={p} onClick={() => { setOverlay(null); showPerson(p); }} />)}</> : overlay === 'local-like' ? <><p>Реакция сохранена на этом устройстве. На отношения и награды она пока не влияет.</p><Button onClick={() => setOverlay(null)}>Понятно</Button></> : overlay === 'quote' ? <><QuoteBlock name={quote?.name ?? ''} text={quote?.text ?? ''} /><p className="caption muted">Связь цитирования пока не поддерживается. Можно взять текст как контекст нового поста.</p><Button onClick={() => { setOverlay(null); openCompose(undefined, quote); }}>Написать с этим контекстом</Button></> : <><p>Миры, публикации, ответы и обмен кристаллов на энергию работают с сервером. Прогресс нового сюжета пока не подключён.</p><p className="caption muted">Уведомления и реакции сохраняются на этом устройстве.</p><Button onClick={() => setOverlay(null)}>Понятно</Button></>}
    </Dialog>}
  </div></div>;
}

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
import { storyForTemplate, storyForCampaign, campaignFor, pilotDefinition, pilotBinding, boundCampaign, type PilotBinding, type PilotCampaign } from './story/pilot';
import { StoryOpening, StoryMap, PlayMode, DemoInventory, StoryWallet, XPFlight, TypingDraft } from './storyplay/StorySurfaces';
import { freshStory, isStoryPlay, finishQuest, buyBooster, grantDemoCrystals, spendStoryXP, type StoryPlay } from './storyplay/model';
import './storyplay/storyplay.css';
import { StoryBrief } from './story/StoryBrief';
import { PublicationWaiting, PublicationResult, type PublicationReceipt } from './publication/PublicationCards';
import { XPProfile } from './xp/XPProfile';
import { HeroPicker, SkillPicker, AvatarEditor, heroPortrait } from './screens/HeroPicker';
import { personaFor, savePersona } from './persona';
import { WorldProgress, StreakDetails, CrystalExchange, BoosterDetails } from './screens/WorldResources';
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
import { DesignSystem } from './screens/DesignSystem';
import { NominalSignOut, Avatar, Badge, BottomNav, Button, Chip, Dialog, EmptyState, Icon, Notice, PersonRow, PostCard, ProfileHeader, Progress, QuoteBlock, ResourceBadge, ResumeWorld, ScreenHeading, SettingsRow, TextField, WorldCard } from './ui';

type Screen = 'welcome' | 'age' | 'catalog' | 'search' | 'intro' | 'onboarding' | 'hero' | 'story-skills' | 'feed' | 'goals' | 'compose' | 'thread' | 'topic' | 'profile' | 'character' | 'messages' | 'activity' | 'account';
type ReplyTarget = { postId: string; commentId: string | null; name: string; text: string };
type ReplyDraft = { text: string; requestId?: string; submittedText?: string };
const statusLabels: Record<string, string> = { pending: 'Персонажи читают публикацию…', ready_for_moderation: 'Готовим результат…', completed: 'Публикация готова', failed: 'Публикация не отправлена' };
const titleOf: Record<Screen, string> = { catalog: 'Миры', search: 'Поиск', intro: 'Вход в мир', onboarding: 'Знакомство', welcome: 'Знакомство', age: 'Возраст', hero: 'Ваш персонаж', 'story-skills': 'Навыки персонажа', feed: 'Лента', goals: 'Цели', compose: 'Новая публикация', thread: 'Обсуждение', topic: 'Тема', profile: 'Профиль персонажа', character: 'Собеседник', messages: 'Личка', activity: 'Уведомления', account: 'Профиль' };

export default function BackendApp() {
  const [hash, setHash] = useState(location.hash);
  useEffect(() => { const change = () => setHash(location.hash); window.addEventListener('hashchange', change); return () => window.removeEventListener('hashchange', change); }, []);
  const [screen, setScreen] = useState<Screen>('catalog');
  const [stack, setStack] = useState<Screen[]>([]);
  const [templates, setTemplates] = useState<World[]>([]);
  const [owned, setOwned] = useState<WorldDocument[]>([]);
  const [selected, setSelected] = useState<World>();
  const [selectedVariant, setSelectedVariant] = useState(false);
  const [player, setPlayer] = useState<Player>();
  const [world, setWorld] = useState<World>();
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
  const [overlay, setOverlay] = useState<'exit' | 'resources' | 'streak' | 'crystals' | 'boosters' | 'map' | 'skills' | 'text-size' | 'local-like' | 'quote' | 'about' | null>(null);
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
  const [receipt, setReceipt] = useState<PublicationReceipt>();
  const [publicationFailed, setPublicationFailed] = useState(false);
  const [prepared, setPrepared] = useState<{ title: string; prompt: string; questId?: string; xp?: number }>();
  const [animateDraft, setAnimateDraft] = useState(false);
  const [typingDraft, setTypingDraft] = useState(false);
  const [playMode, setPlayMode] = useState<'story'|'sandbox'>('story');
  const [selectedBooster, setSelectedBooster] = useState(false);
  const [storyState, setStoryState] = useState<StoryPlay>(() => freshStory(''));
  const [storyOwner,setStoryOwner] = useState('');
  const [storyInventory,setStoryInventory] = useState({crystals:0,boosters:0,bonusUsed:false});
  const [rewardAnimation, setRewardAnimation] = useState<{amount:number;nonce:string}>();
  const cancel = useRef<(() => void) | undefined>(undefined);
  const sending = useRef(false);
  const main = useRef<HTMLDivElement>(null);
  const focusHeading = useRef<HTMLDivElement>(null);
  const feedRequest = useRef(0);
  const threadRequest = useRef(0);
  const interestOnly = useRef(false);
  useEffect(() => { if (player?.experiments) setExperiments(player.experiments); }, [player?.experiments]);
  const key = `reki:v7:${player?.id ?? api.playerId() ?? 'guest'}`;
  const localSave = (name: string, value: unknown) => { if (!saveLocal(`${key}:${name}`, value)) setStorageError(true); };
  const go = (next: Screen) => { setError(''); setStack(s => [...s, screen]); setScreen(next); setToast(undefined); };
  const back = useCallback(() => { if (pending || busy) return; setError(''); setScreen(stack.at(-1) ?? (world ? 'feed' : 'catalog')); setStack(s => s.slice(0, -1)); }, [stack, world, pending, busy]);
  useEffect(() => syncBackButton(stack.length > 0 && screen !== 'age' && !pending, back), [back, stack.length, pending, screen]);
  useEffect(() => {
    if (booting || hash.startsWith('#system')) return;
    const id = `v7.${screen}`; setLogScreen(id); screenView(id);
  }, [screen, booting, hash]);
  useEffect(() => { main.current?.scrollTo(0, 0); focusHeading.current?.focus({ preventScroll: true }); }, [screen]);
  useEffect(() => { document.documentElement.style.setProperty('--text-scale', String(textScale)); document.documentElement.dataset.largeText = String(textScale > 1); if (!saveLocal('reki-text-scale', textScale)) setStorageError(true); }, [textScale]);
  useEffect(() => () => { cancel.current?.(); }, []);
  useEffect(() => {
    if (!player) return;
    setActivities(readLocal(`${key}:activity`, [])); setLikes(readLocal(`${key}:likes`, [])); setQualities(readLocal(`${key}:qualities`, []));
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
    if (goal || persona.goal) view.goal = persona.goal || goal;
    return campaign ? { ...view, title: storyForCampaign(campaign).worldKey === 'sorcerers' ? storyForCampaign(campaign).scenario : view.title, cover: worldArt[storyForCampaign(campaign!).templateId].cover, hero: worldArt[storyForCampaign(campaign!).templateId].cover, goal: persona.goal || goal || campaign.goal } : view;
  };
  const ownedView = (doc: WorldDocument) => storyView(doc, player ? pilotFor(doc, player.id) : undefined);
  const installPlayer = (p: Player) => { setPlayer(p); const doc = worldDocumentOf(p.world); const campaign = pilotFor(doc, p.id); setActiveCampaign(campaign); setWorld(storyView(doc, campaign));
    const draft = readPostDraft(draftKey(p));
    const savedReceipt = readLocal<PublicationReceipt | undefined>(`reki:v7:${p.id}:result:${p.world}`, undefined);
    setReceipt(savedReceipt ? { ...savedReceipt, worldId: p.world } : undefined);
    if (!sending.current) { setDraft(draft.text); setUnknown(!!draft.publicationId); setPublicationFailed(false); }
    return p; };
  const refresh = async () => { const p = installPlayer(await api.me()); await loadFeed(p); return p; };
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

  useEffect(() => { if (!rewardAnimation) return; const timer=setTimeout(()=>setRewardAnimation(undefined),1800); return()=>clearTimeout(timer); },[rewardAnimation?.nonce]);
  const storyKey = player && activeCampaign ? `${key}:story-play:${player.world}:${activeCampaign.id}` : '';
  const inventoryKey = `${key}:story-inventory`;
  const playBase = activeCampaign && storyOwner === storyKey && storyState.campaignId === activeCampaign.id ? storyState : freshStory(activeCampaign?.id ?? '');
  const play = {...playBase,...storyInventory};
  const allQuests = activeCampaign?.days.flatMap(day => day.quests) ?? [];
  const currentQuest = allQuests.find(q => !play.completed.includes(q.id));
  const editorQuest = allQuests.find(q => q.id === prepared?.questId) ?? currentQuest;
  const storyLabels = player && personaFor(player.id, player.world).skills?.length === 3 ? personaFor(player.id, player.world).skills! : ['Смелость','Юмор','В тему'];
  const stageDay = activeCampaign ? Math.ceil((activeCampaign.days.find(day => day.quests.some(q => q.id === currentQuest?.id))?.day ?? 7) / 3) : undefined;
  const goalProgress = activeCampaign ? Math.floor(activeCampaign.days.reduce((sum,day) => sum + day.quests.filter(q => play.completed.includes(q.id)).length * day.weight / 3,0) / activeCampaign.goalWeights.reduce((a,b)=>a+b,0) * 100) : 0;
  useEffect(() => {
    if (!activeCampaign || !storyKey) { setStoryState(freshStory('')); setStoryOwner(''); return; }
    const saved = readLocal<unknown>(storyKey, undefined);
    const initial = isStoryPlay(saved,activeCampaign.id) ? saved : freshStory(activeCampaign.id);
    setStoryState(initial); setStoryOwner(storyKey);
    const inventory = readLocal<{crystals:number;boosters:number;bonusUsed:boolean} | undefined>(inventoryKey,undefined);
    const shared = inventory && Number.isSafeInteger(inventory.crystals) && inventory.crystals >= 0 && Number.isSafeInteger(inventory.boosters) && inventory.boosters >= 0 ? inventory : {crystals:initial.crystals,boosters:initial.boosters,bonusUsed:initial.bonusUsed};
    setStoryInventory(shared); if(!inventory) saveLocal(inventoryKey,shared);
    setSelectedBooster(false); setRewardAnimation(undefined);
  }, [storyKey]);
  const updateStory = (change: (state: StoryPlay) => StoryPlay) => {
    if (!storyKey || !activeCampaign) return;
    const saved = readLocal<unknown>(storyKey, undefined);
    const inventory = readLocal(inventoryKey,storyInventory);
    const before = {...(isStoryPlay(saved,activeCampaign.id) ? saved : play),...inventory};
    const next = change(before); if (next === before) return;
    setStoryState(next); setStoryOwner(storyKey); const inventoryNext = {crystals:next.crystals,boosters:next.boosters,bonusUsed:next.bonusUsed}; setStoryInventory(inventoryNext); if (!saveLocal(storyKey,next) || !saveLocal(inventoryKey,inventoryNext)) setStorageError(true);
  };
  const creditQuest = (actionId: string, reference?: {questId?:string; mode?:string; booster?:boolean}) => {
    if (!activeCampaign || reference?.mode !== 'story') return;
    const quest = allQuests.find(q => q.id === reference.questId); if (!quest) return;
    let reward: {amount:number;title:string;questId:string} | undefined;
    updateStory(before => { const next = finishQuest(before,quest.id,actionId,quest.baseXP,!!reference.booster); if(next!==before) reward={amount:(next.total-before.total)/100,title:quest.title,questId:quest.id}; return next; });
    if(reward) { setRewardAnimation({amount:reward.amount,nonce:actionId}); setSelectedBooster(false); }
    return reward;
  };
  const openWorld = async (w: World, storyVariant = false) => {
    if (sending.current || pending) return;
    setSelected(w); setSelectedVariant(storyVariant); go('intro');
  };
  const startHero = (w: World) => {
    const def = worldDefinitionOf(w.id); setHeroBase(def); setDefinition(def); setHeroChoice(null); setPilotSelected(!!storyForTemplate(def.templateId) && (storyForTemplate(def.templateId)!.worldKey !== 'sorcerers' || selectedVariant)); setHeroGoal(''); setHeroSkills([]); setHeroAvatar(undefined); setHeroSeed(def.avatar); setAvatarRemoved(false); go('hero');
  };
  const chooseHero = (choice?: { id: string }) => {
    if (!definition || !heroBase) return;
    setHeroChoice(choice?.id ?? 'custom');
    setHeroGoal((pilotSelected ? campaignFor(choice?.id ?? 'custom', heroBase.templateId)?.goal : undefined) || selected?.goal || '');
    setHeroSkills(choice ? ['Смелость', 'Юмор', 'В тему'] : []); setHeroAvatar(undefined); setHeroSeed(choice?.id ?? crypto.randomUUID()); setAvatarRemoved(false);
    if (pilotSelected) { setDefinition(pilotDefinition(heroBase, choice?.id ?? 'custom')); return; }
    if (!choice) { setDefinition(heroBase); return; }
    const star = heroBase.stars.find(star => star.id === choice.id); if (!star) return;
    setDefinition({ ...heroBase, name: star.name, avatar: star.avatarSeed, role: star.role,
      description: `${heroBase.description}\nПерсона игрока: ${star.name}. ${star.about}`.slice(0, 2000),
      stars: heroBase.stars.filter(value => value.id !== star.id) });
  };
  const enter = async () => {
    if (!selected || pending || busy) return;
    const existing = owned.find(d => d.id === selected.id);
    if (existing) {
      setBusy(true); setError('');
      try { const p = installPlayer(await api.me(existing.id)); setFeed([]); setScreen('feed'); setStack([]); await loadFeed(p); }
      catch (cause) { setError(errorText(cause)); } finally { setBusy(false); }
    } else startHero(selected);
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
    setQualities(values); localSave('qualities', values);
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
      const created = await api.createWorld(heroChoice === 'custom' && heroGoal.trim() ? { ...definition, firstContext: `${definition.firstContext}\nЦель персонажа: ${heroGoal.trim()}` } : definition);
      const campaign = pilotSelected && heroChoice && storyForTemplate(definition.templateId) ? campaignFor(heroChoice, definition.templateId) : undefined;
      if (campaign && !saveLocal(`reki:v7:${created.id}:story:${created.worldInstanceId}`, pilotBinding(campaign))) setStorageError(true);
      const portrait = avatarRemoved ? undefined : heroAvatar || await heroPortrait(heroChoice ?? 'custom', heroSeed || definition.avatar, definition.templateId || created.world, definition.avatarStyle).catch(() => undefined);
      if (!savePersona(created.id, created.world, { goal: heroGoal.trim(), skills: heroSkills, avatar: portrait })) setStorageError(true);
      const p = installPlayer(created); trackEvent('onboarding_step', { step: 'done', ok: true, world: p.world }); localSave('qualities', qualities);
      const own = await api.ownedWorlds(); installWorldContent(own); setOwned(own);
      setScreen('goals'); setStack([]); await loadFeed(p);
    } catch (cause) { setError(errorText(cause)); } finally { setBusy(false); }
  };
  const replyKey = (target: ReplyTarget) => `${key}:reply:${player?.world}:${target.postId}:${target.commentId ?? 'root'}`;
  const openCompose = (target?: ReplyTarget, context?: { name: string; text: string }) => {
    if (!player || sending.current) return;
    setAnimateDraft(false); setPrepared(undefined); setReply(target); setQuote(context); setUnknown(false); setStatus('');
    if (target) { const saved = readLocal<ReplyDraft>(replyKey(target), { text: '' }); setDraft(saved.text); setUnknown(!!saved.requestId); }
    else { const saved = readPostDraft(draftKey(player)); setDraft(saved.text); setUnknown(!!saved.publicationId); }
    go('compose');
  };
  const openQuest = (questId?: string) => {
    if (!world || !player || sending.current) return;
    const quest = allQuests.find(q => q.id === questId) ?? currentQuest;
    openCompose(undefined, { name: 'Квест', text: quest?.prompt ?? world.seedPost });
    setPrepared({ title: quest?.title ?? 'Первый ход', prompt: quest?.prompt ?? world.seedPost, questId: quest?.id, xp: quest?.baseXP });
    const saved = readPostDraft(draftKey(player));
    if (!saved.publicationId && saved.text === activeCampaign?.firstPost && !saved.ownWords) setAnimateDraft(true);
    const focus = quest?.focusSkill === 'secondary' ? 1 : quest?.focusSkill === 'tertiary' ? 2 : 0;
    const eligible = quest?.drafts.filter(d => d.minSkill <= play.levels[focus]).sort((a,b)=>b.minSkill-a.minSkill)[0];
    if (!saved.text.trim() && !saved.publicationId && (eligible?.text || activeCampaign?.firstPost)) {
      const text = eligible?.text ?? activeCampaign!.firstPost; setDraft(text); setAnimateDraft(true);
      if (!savePostDraft(draftKey(player),{text,ownWords:false})) setStorageError(true);
    }
  };
  const openFirstPost = () => openQuest(currentQuest?.id);
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
    if (!player || !draft.trim() || sending.current || typingDraft) return;
    if (player.postsLeft <= 0 && !unknown) { trackEvent('limit_reached'); setOverlay('resources'); return; }
    if (reply) { void sendReply(); return; }
    const k = draftKey(player), saved = readPostDraft(k);
    const command = submissionFor(saved, draft, () => crypto.randomUUID());
    const { id, text, recovery } = command;
    if (!recovery && activeCampaign) localSave(`story-action:${player.world}:${id}`,{questId:editorQuest?.id,mode:playMode,booster:selectedBooster});
    if (!recovery) trackEvent('post_publish', { len: text.length, own_words: command.ownWords });
    if (!savePostDraft(k, { ...saved, text: draft, publicationId: id, submittedText: text, ownWords: command.ownWords, submittedOwnWords: command.ownWords })) setStorageError(true);
    sending.current = true; setPending(true); setUnknown(false); setPublicationFailed(false); setError(''); setStatus('pending'); setScreen('feed'); setStack([]);
    cancel.current = api.publish(text, id, event => {
      if (event.type !== 'result') return;
      trackEvent('post_result', { delta: event.result.delta, viral: event.result.viral, flop: event.result.flop });
      completePostDraft(k, id, text); setDraft(readPostDraft(k).text);
      const reward = creditQuest(event.result.postId,readLocal(`${key}:story-action:${player.world}:${id}`,undefined));
      const value: PublicationReceipt = { worldId: player.world, result: event.result, before: player.skills, questReward: reward };
      setReceipt(value); saveLocal(`${key}:result:${player.world}`, value);
      setToast(event.result); setPublicationFailed(false); setUnknown(false); setPending(false); sending.current = false; setStatus('completed');
      const item: Activity = { id: event.result.postId, worldId: player.world, title: event.result.headline, text: `${event.result.delta > 0 ? '+' : ''}${event.result.delta} подписчиков`, read: false };
      setActivities(old => { const next = [item, ...old.filter(a => a.id !== item.id)].slice(0, 100); localSave('activity', next); return next; });
      setScreen('feed'); setStack([]); void refresh().then(p => { const updated = { ...value, after: p.skills }; setReceipt(updated); saveLocal(`${key}:result:${player.world}`, updated); }).catch(() => setFeedError('Публикация сохранена. Обнови ленту, чтобы увидеть результат.'));
    }, (state, reason) => {
      sending.current = false; setPending(false); setUnknown(state === 'unknown'); setPublicationFailed(state === 'failed');
      if (state === 'failed') { const latest = readPostDraft(k); savePostDraft(k, { text: latest.text, ownWords: latest.ownWords }); }
      setError(publicationError(state, reason));
    }, setStatus, command.ownWords, command.wantId, recovery);
  };
  const sendReply = async () => {
    if (!player || !reply || sending.current) return;
    const k = replyKey(reply), saved = readLocal<ReplyDraft>(k, { text: '' });
    const requestId = saved.requestId ?? crypto.randomUUID(), text = saved.submittedText ?? draft;
    if (!saved.requestId && activeCampaign) localSave(`story-action:${player.world}:${requestId}`,{questId:currentQuest?.actionKinds.includes('comment') ? currentQuest.id : undefined,mode:playMode,booster:selectedBooster});
    saveLocal(k, { text: draft, requestId, submittedText: text });
    sending.current = true; setPending(true); setError('');
    let terminalFailure = false;
    try {
      const controller = new AbortController(); cancel.current = () => controller.abort();
      const result = await api.reply(reply.postId, reply.commentId, requestId, text, { signal: controller.signal, onEvent: event => { if (event.status === 'failed') terminalFailure = true; } });
      if (!result.post) throw new Error('Missing reply');
      creditQuest(requestId,readLocal(`${key}:story-action:${player.world}:${requestId}`,undefined));
      setPost({ ...result.post, id: result.post.postId }); saveLocal(k, { text: '' }); setDraft(''); setReply(undefined); setScreen('thread'); setStack(s => s.filter(x => x !== 'compose'));
      setUnknown(false);
      void refresh().catch(() => setFeedError('Ответ сохранён. Обнови ленту, чтобы увидеть актуальные данные.'));
    } catch (cause) {
      const rejected = terminalFailure || cause instanceof ApiError && cause.status >= 400 && cause.status < 500 && cause.status !== 409;
      if (rejected) saveLocal(k, { text: draft });
      setUnknown(!rejected); setError(rejected ? 'Ответ не отправлен. Текст сохранён — можно изменить его или попробовать снова.' : errorText(cause) + ' Черновик сохранён; повтор проверяет тот же ответ.');
    }
    finally { sending.current = false; setPending(false); }
  };
  const stopWaiting = () => { cancel.current?.(); sending.current = false; setPending(false); setUnknown(true); setError('Ожидание остановлено. Запрос мог сохраниться на сервере — проверь статус, чтобы продолжить без дубликата.'); };
  const showPerson = (value: Person) => { if (value.id === 'player') go('profile'); else { const cast = world?.cast.find(p => p.id === value.id || p.name === value.name); setPerson(cast ?? value); go('character'); } };
  const card = (p: Post, author: Person, count = 0, compact = false) => <PostCard key={p.id} post={{ ...p, liked: likes.includes(`${player?.world}:${p.id}`) }} author={author} replyCount={count} compact={compact}
    onReply={p.parentId || p.id ? () => void openThread(p.parentId ?? p.id) : undefined} onAuthor={() => showPerson(author)}
    onLike={() => { const id = `${player?.world}:${p.id}`; const next = likes.includes(id) ? likes.filter(x => x !== id) : [...likes, id]; setLikes(next); localSave('likes', next); }}
    onQuote={() => { setQuote({ name: author.name, text: p.text }); setOverlay('quote'); }} />;

  const renderFeed = () => <>
    <header className="world-header"><button className="world-switch" onClick={() => setOverlay('exit')}><strong>{world?.title} ⌄</strong><span>Мой мир</span></button>
      <ResourceBadge type="energy" value={String(player?.postsLeft ?? 0)} onClick={() => setOverlay('resources')} />
      <ResourceBadge type="crystal" value={activeCampaign && play.enabled ? String(play.crystals) : '—'} hint={activeCampaign ? "Кристаллы локальной пробы и бустеры" : "Обменять на энергию и усиление XP · скоро"} onClick={() => setOverlay(activeCampaign ? 'boosters' : 'crystals')} />
      <button className="profile-access" data-unspent={activeCampaign && play.available>0 ? "true" : undefined} aria-label={activeCampaign && play.enabled ? `Профиль персонажа: ${play.total/100} XP, цель ${goalProgress}%. Потратить XP на навыки` : "Профиль персонажа"} onClick={() => go('profile')} style={activeCampaign && play.enabled ? { background: `conic-gradient(var(--color-progress) ${goalProgress}%,var(--color-divider) 0)` } : undefined}><Avatar name={player?.name ?? ''} src={player ? personaFor(player.id, player.world).avatar : undefined} size="lg" />{activeCampaign && play.enabled && <span className="story-avatar-label">{play.total/100} XP · {goalProgress}%</span>}</button></header>
    {player && <WorldProgress streak={player.streak} storyDay={stageDay} boosterCount={activeCampaign && play.enabled ? String(play.boosters) : '—'} onStreak={() => setOverlay('streak')} onBoosters={() => setOverlay('boosters')} />}
    {activeCampaign && <section className="story-tools"><button className="map-access" onClick={() => setOverlay('map')}>Карта мира <span aria-hidden="true">↗</span></button><button className="local-play-badge" onClick={() => setOverlay('boosters')}>{play.enabled ? 'Локальная проба' : 'Включить сюжетную пробу'}</button></section>}
    {!reply && (pending || unknown || publicationFailed) && <PublicationWaiting pending={pending} unknown={unknown} text={draft} status={status} onStop={stopWaiting} onContinue={() => { if (unknown) publish(); else openCompose(); }} />}
    {receipt && receipt.worldId === player?.world && <PublicationResult receipt={receipt} animateReward={rewardAnimation?.nonce===receipt.result.postId} onRead={() => void openThread(receipt.result.postId)} />}
    {feedError && <div className="section"><Notice title="Не удалось обновить ленту" tone="error">{feedError}</Notice></div>}
    {!feed.length && !feedLoading && !feedError && <EmptyState title="Первое слово за тобой" action={<Button onClick={() => openCompose()}>Написать пост</Button>}>{world?.seedPost || 'Расскажи свою версию истории. Здесь появятся публикации и ответы.'}</EmptyState>}
    {feed.map(item => { const view = feedView(item, playerView(player!)); return card(view.post, view.author, view.count); })}
    {feedLoading && <div className="pending-line" role="status"><span className="spinner" />Загружаем ленту…</div>}
    <div className="section"><Button variant="secondary" busy={feedLoading} onClick={() => void loadFeed(player!, cursor ?? undefined)}>{cursor ? 'Показать ещё' : 'Обновить ленту'}</Button></div>
  </>;

  const worldActivities = activitiesForWorld(activities, player?.world);
  const selectedStory = storyForTemplate(definition?.templateId);
  const heroOptions = definition ? [...(pilotSelected && selectedStory ? selectedStory.campaigns.filter(c => c.roleId !== 'own').map(c => ({ id: c.roleId, name: c.heroName, role: c.roleName, goal: c.goal })) : (heroBase ?? definition).stars.map(s => ({ id: s.id, name: s.name, role: s.role, goal: selected?.goal || '' }))), { id: 'custom', name: heroChoice === 'custom' ? definition.name || 'Свой персонаж' : 'Свой персонаж', role: 'Твоя история', goal: selected?.goal || '' }] : [];
  let content: ReactNode;
  if (booting) content = <EmptyState title="Открываем твои миры"><span className="spinner" /> Ещё немного…</EmptyState>;
  else if (screen === 'catalog' || screen === 'search') {
    const entries = rankCatalog(withHogwartsCard(catalogEntries(templates, plannedWorlds), storyForTemplate('aec9494b-4118-4525-8458-e85d759c8dd9')!), qualities);
    const filtered = filterCatalog(entries, query, genre);
    content = <><ScreenHeading title={screen === 'search' ? 'Найти свой мир' : 'Миры'} />
      {screen === 'search' && <div className="search-field"><TextField label="Название или тема" value={query} onChange={e => setQuery(e.target.value)} placeholder="Магия, приключения, общение…" /></div>}
      {screen === 'catalog' && <section className="catalog-preferences"><p className="caption muted">{qualities.length ? `Для тебя · ${qualities.join(' · ')}` : 'Все миры'}</p><button className="link-button" onClick={() => { interestOnly.current = true; go('onboarding'); }}>Изменить интересы</button></section>}
      {screen === 'catalog' && owned.length > 0 && <section className="saved-worlds"><h2 className="section-title">Мои миры</h2><div className="saved-worlds-scroll" style={{ gridTemplateRows: `repeat(${Math.min(4, owned.length)}, auto)` }} tabIndex={0} aria-label="Мои миры — листай вправо">{owned.map(d => <ResumeWorld key={d.id} world={ownedView(d)} subtitle={d.name} onClick={() => void openWorld(ownedView(d))} />)}</div>{owned.length > 4 && <p className="caption muted">Листай вправо, чтобы увидеть остальные миры →</p>}</section>}
      <div className="filter-strip">{['Все', ...new Set(entries.map(({ world }) => world.genre))].map(g => <Chip key={g} selected={genre === g} onClick={() => setGenre(g)}>{g}</Chip>)}</div>
      {filtered.length ? <div className="world-grid">{filtered.map(({ key, world: w, available, storyVariant }) => <WorldCard key={key} world={w} disabled={!available} selected={available && selected?.id === w.id && selectedVariant === !!storyVariant} onClick={available ? () => void openWorld(w, !!storyVariant) : undefined} />)}</div> : <EmptyState title="Такого мира пока нет" action={<Button onClick={() => { setQuery(''); setGenre('Все'); if (!templates.length) void boot(); }}>Сбросить поиск</Button>}>Попробуй другое название или убери фильтр.</EmptyState>}
      {player?.hitOwnWorldMilestone && <div className="section"><Button onClick={() => { setDefinition({ ...blankWorld(), name: player.name, avatar: player.handle, stars: [{ id: crypto.randomUUID(), name: '', role: '', about: '', voice: 'Живо и по делу', avatarSeed: 'guide', trait: 1, relation: 0 }] }); go('hero'); }}>Создать свой мир</Button></div>}
    </>;
  } else if (screen === 'intro' && selected) content = <WorldIntro world={selected} busy={busy} resume={owned.some(d => d.id === selected.id)} onBack={back} onEnter={() => void enter()} />;
  else if (screen === 'welcome') content = <Onboarding key="entry-welcome" initialStep={0} onTry={() => go('age')} onDone={() => go('age')} onBack={back} />;
  else if (screen === 'age') content = <AgeEntry initialAge={entryAge} minimumAge={14} requireConsent={true} onBack={value => { setEntryAge(value); back(); }} onContinue={continueAge} />;
  else if (screen === 'onboarding') content = <Onboarding key="entry-interests" initialStep={1} initialQualities={qualities} onBack={back} onDone={completeInterests} />;
  else if (screen === 'hero' && definition && definition.templateId) content = <><ScreenHeading title="Твой персонаж" back={back} /><section className="section hero-editor">
    {selected && <div className="hero-world"><img src={selected.hero} alt="" /><div className="hero-world-copy"><strong>{selected.title}</strong><p className="caption muted">Новая история</p></div></div>}
    <HeroPicker options={heroOptions} selected={heroChoice} onChoose={id => chooseHero(id === 'custom' ? undefined : { id })} worldId={definition.templateId} style={definition.avatarStyle} seed={heroSeed || definition.avatar} customAvatar={heroAvatar} removed={avatarRemoved} skills={heroSkills} goal={heroGoal} />
    {heroChoice === 'custom' && <div className="persona-form"><section className="persona-block persona-identity"><div className="persona-section-title"><h2>Создай своего героя</h2></div>
      <TextField label="Имя персонажа" maxLength={80} value={definition.name} onChange={e => setDefinition({ ...definition, name: e.target.value })} />
      <TextField label="Имя канала" maxLength={80} value={definition.avatar} onChange={e => setDefinition({ ...definition, avatar: e.target.value })} />
      <TextField label="Твоя цель в истории" maxLength={500} value={heroGoal} onChange={e => setHeroGoal(e.target.value)} placeholder="Моя миссия — спасти Хогвартс" />
      </section><SkillPicker selected={heroSkills} onChange={setHeroSkills} />
      <AvatarEditor onGenerate={() => { setHeroSeed(crypto.randomUUID()); setHeroAvatar(undefined); setAvatarRemoved(false); }} onUpload={src => { setHeroAvatar(src); setAvatarRemoved(false); }} onRemove={() => { setHeroAvatar(undefined); setAvatarRemoved(true); }} />
      <div className="hero-address persona-block"><p className="caption">Как обращаться</p><div className="row">{([['she', 'она'], ['he', 'он'], ['nick', 'по нику']] as const).map(([id, label]) => <Chip key={id} selected={address === id} onClick={() => setAddress(id)}>{label}</Chip>)}</div></div>
    </div>}
    {pilotSelected && campaignFor(heroChoice, definition.templateId) && <div className="hero-cast"><p className="caption muted">Собеседники: {definition.stars.map(star => star.name).join(' · ')}</p></div>}
    <div className="hero-entry-actions"><p className="caption muted">Дальше — первый день твоей истории</p>
    <Button busy={busy} disabled={!heroChoice || !definition.stars.length || (heroChoice === 'custom' && (!definition.name.trim() || !definition.avatar.trim() || !heroGoal.trim() || heroSkills.length !== 3))} onClick={() => void createWorld()}>{heroChoice ? `Войти как ${definition.name.trim() || '…'} →` : 'Выбери персонажа'}</Button></div></section></>;
  else if (screen === 'hero' && definition) content = <><ScreenHeading title="Свой мир" back={back} /><section className="section stack"><TextField label="Имя персонажа" maxLength={80} value={definition.name} onChange={e => setDefinition({ ...definition, name: e.target.value })} /><TextField label="Имя канала" maxLength={80} value={definition.avatar} onChange={e => setDefinition({ ...definition, avatar: e.target.value })} />
    <label>Как обращаться<select value={address} onChange={e => setAddress(e.target.value as Address)}><option value="nick">По имени</option><option value="she">Она</option><option value="he">Он</option></select></label>
    <TextField label="Кто ты в этом мире" value={definition.role} maxLength={80} onChange={e => setDefinition({ ...definition, role: e.target.value })} />
    <TextField label="Название мира" value={definition.title} onChange={e => setDefinition({ ...definition, title: e.target.value })} /><label>Описание<textarea value={definition.description} onChange={e => setDefinition({ ...definition, description: e.target.value })} maxLength={2000} /></label><TextField label="Место действия" value={definition.where} onChange={e => setDefinition({ ...definition, where: e.target.value })} /><TextField label="Первая завязка" value={definition.firstContext} onChange={e => setDefinition({ ...definition, firstContext: e.target.value })} /><h2>Собеседники</h2>{definition.stars.map((star, index) => <div className="stack" key={star.id}><TextField label={`Имя собеседника ${index + 1}`} value={star.name} onChange={e => setDefinition({ ...definition, stars: definition.stars.map((s, i) => i === index ? { ...s, name: e.target.value } : s) })} /><TextField label="Роль собеседника" value={star.role} onChange={e => setDefinition({ ...definition, stars: definition.stars.map((s, i) => i === index ? { ...s, role: e.target.value } : s) })} /><TextField label="О собеседнике" value={star.about} onChange={e => setDefinition({ ...definition, stars: definition.stars.map((s, i) => i === index ? { ...s, about: e.target.value } : s) })} /></div>)}<Button variant="secondary" disabled={definition.stars.length >= 3} onClick={() => setDefinition({ ...definition, stars: [...definition.stars, { id: crypto.randomUUID(), name: '', role: '', about: '', voice: 'Живо и по делу', avatarSeed: crypto.randomUUID(), trait: 1, relation: 0 }] })}>Добавить собеседника</Button>
    <label>Градус драмы<select value={definition.drama} onChange={e => setDefinition({ ...definition, drama: e.target.value as WorldDefinition['drama'] })}><option value="soft">Мягко</option><option value="mid">Средне</option><option value="hot">Остро</option><option value="off">Без хейта</option></select></label>
    <Button busy={busy} disabled={!definition.name.trim() || !definition.avatar.trim() || !definition.role.trim() || !definition.title.trim() || !definition.stars.length || definition.stars.some(star => !star.name.trim() || !star.role.trim())} onClick={() => void createWorld()}>Начать историю</Button></section></>;
  else if (screen === 'feed' && player && world) content = renderFeed();
  else if (screen === 'goals' && activeCampaign) content = <StoryOpening campaign={activeCampaign} goal={world?.goal || activeCampaign.goal} skills={storyLabels} step="mission" onNext={() => go('story-skills')} onBack={back} />;
  else if (screen === 'story-skills' && activeCampaign) content = <StoryOpening campaign={activeCampaign} goal={world?.goal || activeCampaign.goal} skills={storyLabels} step="skills" onNext={openFirstPost} onBack={back} />;
  else if (screen === 'goals' && world) content = <><ScreenHeading title="Цели" subtitle={world.title} /><StoryBrief goal={world.goal || 'Найти свой голос и начать разговор.'} /><section className="section"><Button onClick={openFirstPost}>Написать первый пост →</Button></section><section className="section stack"><h2 className="section-title">Темы для разговора</h2><p className="caption muted">Вдохновение для постов. Здесь нет обязательного порядка и начисления за квесты.</p>{world.quests.map((q, i) => <button className="quest-row" key={q.id} onClick={() => { setQuote({ name: 'Тема', text: q.prompt }); go('topic'); }}><span className="quest-number">{String(i + 1).padStart(2, '0')}</span><strong>{q.title}</strong><span>↗</span></button>)}<Button onClick={() => openCompose()}>Написать о своём</Button></section></>;
  else if (screen === 'compose' && player) content = <><ScreenHeading title={reply ? 'Продолжить разговор' : prepared?.title ?? 'Новая публикация'} back={back} /><section className="section stack composer-body">{prepared && <div className="quest-editor-brief"><div className="row between"><strong>Текущий квест</strong>{prepared.xp && <span className="quest-xp">+{prepared.xp} XP</span>}</div><p>{prepared.prompt}</p><span className="caption muted">Награда локальной пробы</span></div>}{(reply || quote) && !prepared && <QuoteBlock name={reply?.name ?? quote!.name} text={reply?.text ?? quote!.text} />}<div className="row"><Avatar name={player.name} src={personaFor(player.id, player.world).avatar} /><strong>{player.name}</strong><span className="caption muted">Публично</span></div><label className="sr-only" htmlFor="post-text">Текст публикации</label>{!reply ? <TypingDraft value={draft} animate={animateDraft} disabled={pending || unknown} onChange={text => { setAnimateDraft(false); updateDraft(text); }} onTyping={setTypingDraft} /> : <textarea id="post-text" className="composer-text" value={draft} onChange={e => updateDraft(e.target.value)} disabled={pending || unknown} maxLength={1000} autoFocus />}<div className="row between caption muted"><span>Черновик сохраняется</span><span>{draft.length} / {reply ? 1000 : 2000}</span></div>{activeCampaign && <><PlayMode value={playMode} onChange={setPlayMode}/><Button variant="secondary" onClick={() => setOverlay('boosters')}>{selectedBooster ? 'Бустер выбран · +100% XP' : `Воспользоваться бустером · ${play.enabled ? play.boosters : '—'}`}</Button></>}{quote && !reply && !prepared && <p className="caption muted">Контекст помогает написать пост. Связь цитирования на сервере пока недоступна.</p>}{pending && <div className="pending-line" role="status"><span className="spinner" />{statusLabels[status] ?? 'Отправляем ответ…'}</div>}{pending && <Button variant="secondary" onClick={stopWaiting}>Остановить ожидание</Button>}<Button className="full" size="lg" disabled={!draft.trim() || typingDraft} busy={pending} onClick={publish}>{unknown ? 'Проверить статус' : 'Опубликовать'} <span className="caption">· 1 энергия</span></Button></section></>;
  else if (screen === 'thread' && player && world) {
    const view = post ? detailView(post, world, playerView(player)) : undefined;
    const postAuthor = post && world.cast.find(p => `Star_${p.id}` === post.authorId);
    content = <><ScreenHeading title="Обсуждение" back={back} />{threadLoading && <div className="pending-line" role="status">Загружаем обсуждение…</div>}{view && card(view.post, view.author, post!.comments.length)}{post?.comments.map(comment => {
      const author: Person = comment.me ? playerView(player) : world.cast.find(p => `Star_${p.id}` === comment.authorId) ?? { id: comment.authorId ?? comment.id, name: comment.author, handle: comment.avatar ?? '', role: comment.role ?? '', boundary: '' };
      return <div key={comment.id} className={comment.parentCommentId ? 'thread-nested' : ''}>{comment.parentCommentId && <p className="reply-to caption muted">↳ Ответ на реплику</p>}{card({ id: comment.id, parentId: post.id, authorId: author.id, text: comment.hidden ? 'Комментарий скрыт' : comment.text, createdAt: '', likes: 0 }, author, 0, true)}{comment.star && !comment.hidden && world.cast.some(p => `Star_${p.id}` === comment.authorId) && <div className="reply-action"><Button variant="ghost" onClick={() => openCompose({ postId: post.id, commentId: comment.id, name: author.name, text: comment.text })}>Ответить {author.name}</Button></div>}</div>;
    })}{post && !post.comments.length && <EmptyState title="Здесь ещё нет ответов">Персонажи могут продолжить разговор после публикации.</EmptyState>}{postAuthor && post && <div className="section"><Button className="full" onClick={() => openCompose({ postId: post.id, commentId: null, name: postAuthor.name, text: post.text })}>Начать разговор с автором</Button></div>}</>;
  } else if (screen === 'profile' && player && world) content = <><ProfileHeader name={player.name} avatar={personaFor(player.id, player.world).avatar} onBack={back} />{activeCampaign ? <StoryWallet state={play} labels={storyLabels} onEnable={() => updateStory(s => ({...s,enabled:true}))} onSpend={points => updateStory(s => spendStoryXP(s,points))}/> : <XPProfile key={`${player.id}:${player.world}`} player={player} plannedSkills={personaFor(player.id, player.world).skills} />}<section className="profile-details"><button className="profile-goal" onClick={() => go('goals')}><strong>Ваша цель</strong><p>{world.goal}</p></button>{personaFor(player.id, player.world).skills?.length ? <p className="caption">Навыки для прокачки: {personaFor(player.id, player.world).skills!.join(' · ')}</p> : null}<SettingsRow label="Подписчики" value={String(player.subs)} onClick={() => setOverlay('skills')} /><Button variant="secondary" onClick={() => { go('account'); }}>Настройки профиля</Button></section></>;
  else if (screen === 'character' && person) content = <><ProfileHeader name={person.name} avatar={person.avatar} onBack={back} /><div className="character-bio"><span className="caption muted">@{person.handle} · AI-персонаж</span><p>{person.boundary || person.role}</p>{person.relation !== undefined && <p className="caption muted">Отношения: {person.relation > 0 ? "+" : ""}{person.relation}</p>}</div>{feed.filter(item => item.author === person.name).map(item => { const view = feedView(item, playerView(player!)); return card(view.post, view.author, view.count); })}<div className="section"><Button onClick={() => { setReply(undefined); setQuote({ name: person.name, text: person.boundary || person.role }); go('topic'); }}>О чём поговорить</Button></div></>;
  else if (screen === 'topic' && world) content = <><div className="topic-header"><button className="back-button" onClick={back}>Назад</button><img className="topic-cover" src={world.hero} alt="" /><h1>{quote?.name ?? world.title}</h1><p>{quote?.text ?? world.topic}</p></div><section className="topic-people">{world.cast.map(p => <PersonRow key={p.id} person={p} detail={p.relation === undefined ? undefined : `Отношения: ${p.relation > 0 ? "+" : ""}${p.relation}`} onClick={() => showPerson(p)} />)}<Button className="full" onClick={() => openCompose(undefined, quote)}>Написать</Button></section></>;
  else if (screen === 'messages' && world) content = <><ScreenHeading title="Личка" /><EmptyState title="Сначала — общий разговор" action={<Button onClick={() => go('feed')}>Вернуться в ленту</Button>}>Личные сообщения скоро появятся.</EmptyState><section className="section"><h2 className="section-title">Собеседники</h2>{world.cast.map(p => <PersonRow key={p.id} person={p} detail={p.relation === undefined ? undefined : `Отношения: ${p.relation > 0 ? "+" : ""}${p.relation}`} onClick={() => showPerson(p)} />)}</section></>;
  else if (screen === 'activity') content = <><ScreenHeading title="Уведомления" action={worldActivities.length > 0 && <button className="link-button caption" onClick={() => { const next = markWorldActivitiesRead(activities, player?.world); setActivities(next); localSave('activity', next); }}>Прочитать всё</button>} />{!worldActivities.length && <EmptyState title="Пока тихо">Здесь появятся результаты твоих публикаций на этом устройстве.</EmptyState>}{worldActivities.map(a => <button key={a.id} className={`activity-row ${a.read ? '' : 'activity-row--unread'}`} onClick={() => void openThread(a.id)}><span className="activity-marker"><Icon name="activity" /></span><span className="grow"><strong>{a.title}</strong><p>{a.text}</p><span className="caption muted">Открыть публикацию ↗</span></span></button>)}</>;
  else if (screen === 'account') content = <><ProfileHeader name={player?.name ?? 'Твой профиль'} label="Профиль" /><section className="account-worlds"><h2 className="section-title">Мои миры</h2>{owned.length ? owned.map(d => <ResumeWorld key={d.id} world={ownedView(d)} subtitle={d.name} onClick={() => void openWorld(ownedView(d))} />) : <Button onClick={() => go('catalog')}>Выбрать мир</Button>}</section><section className="account-settings"><SettingsRow label="Ресурсы" value={`${player?.postsLeft ?? '—'} энергии`} onClick={() => setOverlay('resources')} /><SettingsRow label="Размер текста" value={`${textScale * 100}%`} onClick={() => setOverlay('text-size')} /><SettingsRow label="Интересы" value={qualities.join(' · ') || 'Выбрать'} onClick={() => { interestOnly.current = true; go('onboarding'); }} /><SettingsRow label="О новой версии" onClick={() => setOverlay('about')} /><NominalSignOut onSignOut={beginEntry} disabled={pending || busy} /></section></>;
  else content = <EmptyState title="Выбери мир" action={<Button onClick={() => go('catalog')}>К мирам</Button>}>Продолжи сохранённую историю или начни новую.</EmptyState>;

  const inWorld = !!world && !['catalog', 'search', 'account', 'intro', 'onboarding', 'welcome', 'age', 'hero'].includes(screen);
  const showNav = !booting && ['catalog', 'search', 'account', 'feed', 'goals', 'messages', 'activity'].includes(screen);
  const nav = inWorld ? [ { id: 'feed', label: 'Лента', icon: 'feed' as const }, { id: 'goals', label: 'Цели', icon: 'goals' as const }, { id: 'compose', label: 'Написать', icon: 'compose' as const }, { id: 'activity', label: 'Уведомления', icon: 'activity' as const, badge: activities.filter(a => !a.read && a.worldId === player?.world).length } ] : [ { id: 'catalog', label: 'Главная', icon: 'home' as const }, { id: 'search', label: 'Поиск', icon: 'search' as const }, { id: 'account', label: 'Профиль', icon: 'profile' as const } ];
  if (hash.startsWith('#system')) return <DesignSystem />;
  return <div className="v7-stage"><div className={`app ${showNav ? 'app--nav' : ''}`}><div className="app-body" ref={main}><div className="sr-only" tabIndex={-1} ref={focusHeading}>{titleOf[screen]}</div>{storageError && <Notice title="Не удалось сохранить на устройстве" tone="error">Не закрывай вкладку: черновик остаётся в памяти до перезагрузки.</Notice>}{error && <div className="section"><Notice title="Не получилось завершить действие" tone="error">{error}</Notice>{!booting && !templates.length && <Button onClick={() => void boot()}>Повторить</Button>}</div>}{content}</div>
    {showNav && <BottomNav items={nav.map(n => ({ ...n, disabled: pending }))} active={screen} onChange={id => { tap('tab', { to: `v7.${id}` }); id === 'compose' ? openCompose() : go(id as Screen); }} />}
    {toast && <div className="result-toast" role="status"><button className="toast-content" onClick={() => { void openThread(toast.postId); setToast(undefined); }}><span className="toast-check">✓</span><span><strong>Запись опубликована · Получены реакции</strong><span className="caption block">{toast.delta > 0 ? '+' : ''}{toast.delta} подписчиков · Читать ответы ↗</span></span></button><button className="toast-close" aria-label="Закрыть уведомление" onClick={() => setToast(undefined)}>×</button></div>}
    {rewardAnimation && <XPFlight amount={rewardAnimation.amount} nonce={rewardAnimation.nonce} />}
    {overlay && <Dialog title={{ exit: 'Выйти из мира?', resources: 'Общие ресурсы', streak: 'Серия дней', crystals: 'Обмен кристаллов', boosters: 'Бустеры', map: 'Карта мира', skills: 'Навыки и отношения', 'text-size': 'Размер текста', 'local-like': 'Реакция', quote: 'Цитата', about: 'Новая версия' }[overlay]} onClose={() => setOverlay(null)}>
      {overlay === 'streak' ? <StreakDetails streak={player!.streak} /> : overlay === 'crystals' ? <CrystalExchange onBoosters={() => setOverlay('boosters')} /> : overlay === 'map' && activeCampaign ? <StoryMap campaign={activeCampaign} state={play} currentId={currentQuest?.id} onSelect={id => { setOverlay(null); openQuest(id); }}/> : overlay === 'boosters' ? (activeCampaign ? <DemoInventory state={play} onEnable={() => updateStory(s => ({...s,enabled:true}))} onDisable={() => { updateStory(s => ({...s,enabled:false})); setSelectedBooster(false); }} onGrant={() => updateStory(grantDemoCrystals)} onBuy={() => updateStory(buyBooster)} selected={selectedBooster} onSelect={() => setSelectedBooster(v => !v)}/> : <BoosterDetails />) : overlay === 'exit' ? <><p>Публикации и черновик сохранятся. Вернёшься с того же места.</p><Button disabled={pending} onClick={() => { feedRequest.current++; setWorld(undefined); setScreen('catalog'); setStack([]); setOverlay(null); }}>Выйти к мирам</Button><Button variant="secondary" onClick={() => setOverlay(null)}>Остаться</Button></> : overlay === 'resources' ? <><p>Энергия общая для всех миров. Публикация и ответ расходуют по 1 энергии.</p><div className="resource-details"><span>Энергия <b>{player?.postsLeft ?? '—'}</b></span><span>Кристаллы <b>—</b></span></div><p className="caption muted">Запас энергии обновляется в 09:00 МСК. Баланс кристаллов и обмен пока недоступны.</p><Button variant="secondary" onClick={() => setOverlay('crystals')}>Кристаллы и обмен</Button><Button variant="secondary" onClick={() => setOverlay('boosters')}>Бустеры</Button><Button onClick={() => setOverlay(null)}>Продолжить</Button></> : overlay === 'text-size' ? <><div className="row" style={{ flexWrap: 'wrap' }}>{[1, 1.25, 1.5, 2].map(v => <Chip key={v} selected={textScale === v} onClick={() => setTextScale(v)}>{v * 100}%</Chip>)}</div><p>Так будет выглядеть текст в приложении.</p><Button onClick={() => setOverlay(null)}>Готово</Button></> : overlay === 'skills' ? <><p>Навыки растут после подтверждённых сервером действий. Высокий навык не гарантирует согласия собеседника.</p>{world?.cast.map(p => <PersonRow key={p.id} person={p} onClick={() => { setOverlay(null); showPerson(p); }} />)}</> : overlay === 'local-like' ? <><p>Реакция сохранена на этом устройстве. На отношения и награды она пока не влияет.</p><Button onClick={() => setOverlay(null)}>Понятно</Button></> : overlay === 'quote' ? <><QuoteBlock name={quote?.name ?? ''} text={quote?.text ?? ''} /><p className="caption muted">Связь цитирования пока не поддерживается. Можно взять текст как контекст нового поста.</p><Button onClick={() => { setOverlay(null); openCompose(undefined, quote); }}>Написать с этим контекстом</Button></> : <><p>Интерфейс v7. Миры, публикации и ответы работают с сервером. Личка, кристаллы и новая система квестов появятся позже.</p><p className="caption muted">Уведомления и реакции сохраняются на этом устройстве.</p><Button onClick={() => setOverlay(null)}>Понятно</Button></>}
    </Dialog>}
  </div></div>;
}

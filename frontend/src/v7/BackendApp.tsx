import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import { activitiesForWorld, markWorldActivitiesRead, type Activity } from './activity';
import { submissionFor } from './submission';
import { screenView, setExperiments, tap, event as trackEvent } from '../app/analytics';
import { setScreen as setLogScreen } from '../app/log';
import { api, ApiError } from '../api/client';
import type { Player, WorldDocument, WorldDefinition, Post as ServerPost, FeedItem, PostResult, Address } from '../api/types';
import { worlds, worldDefinitionOf, worldDocumentOf, installWorldContent } from '../api/worldContent';
import { ageBand as ageBandOf } from '../app/ageBand';
import { AgeEntry } from './screens/AgeEntry';
import { rankCatalog } from './interests';
import { worldArt } from '../app/worldArt';
import { storyPilot, campaignFor, pilotDefinition, pilotBinding, boundCampaign, type PilotBinding, type PilotCampaign } from './story/pilot';
import { StoryDay } from './story/StoryDay';
import { blankWorld } from '../app/worldDraft';
import { pendingReferral, clearReferral } from '../app/referral';
import { draftKey, readPostDraft, savePostDraft, completePostDraft } from '../app/postDraft';
import { syncBackButton } from '../telegram';
import { worldView, playerView, feedView, detailView, errorText, readLocal, saveLocal } from './backend';
import { worlds as plannedWorlds } from './domain/worlds';
import { catalogEntries, filterCatalog } from './catalog';
import type { World, Person, Post, Approach } from './domain/types';
import { Onboarding } from './screens/Onboarding';
import { WorldIntro } from './screens/WorldIntro';
import { DesignSystem } from './screens/DesignSystem';
import { NominalSignOut, Avatar, Badge, BottomNav, Button, Chip, Dialog, EmptyState, Icon, Notice, PersonRow, PostCard, ProfileHeader, ProfileScore, Progress, QuoteBlock, ResourceBadge, ResumeWorld, ScreenHeading, SettingsRow, TextField, WorldCard } from './ui';

type Screen = 'welcome' | 'age' | 'catalog' | 'search' | 'intro' | 'onboarding' | 'hero' | 'feed' | 'goals' | 'compose' | 'thread' | 'topic' | 'profile' | 'character' | 'messages' | 'activity' | 'account';
type ReplyTarget = { postId: string; commentId: string | null; name: string; text: string };
type ReplyDraft = { text: string; requestId?: string; submittedText?: string };
const skillLabels = ['Смелость', 'Юмор', 'В тему'];
const statusLabels: Record<string, string> = { pending: 'Персонажи читают публикацию…', ready_for_moderation: 'Готовим результат…', completed: 'Публикация готова', failed: 'Публикация не отправлена' };
const titleOf: Record<Screen, string> = { catalog: 'Миры', search: 'Поиск', intro: 'Вход в мир', onboarding: 'Знакомство', welcome: 'Знакомство', age: 'Возраст', hero: 'Ваш персонаж', feed: 'Лента', goals: 'Цели', compose: 'Новая публикация', thread: 'Обсуждение', topic: 'Тема', profile: 'Профиль персонажа', character: 'Собеседник', messages: 'Личка', activity: 'Уведомления', account: 'Профиль' };

export default function BackendApp() {
  const [hash, setHash] = useState(location.hash);
  useEffect(() => { const change = () => setHash(location.hash); window.addEventListener('hashchange', change); return () => window.removeEventListener('hashchange', change); }, []);
  const [screen, setScreen] = useState<Screen>('catalog');
  const [stack, setStack] = useState<Screen[]>([]);
  const [templates, setTemplates] = useState<World[]>([]);
  const [owned, setOwned] = useState<WorldDocument[]>([]);
  const [selected, setSelected] = useState<World>();
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
  const [overlay, setOverlay] = useState<'exit' | 'resources' | 'skills' | 'text-size' | 'local-like' | 'quote' | 'about' | null>(null);
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
  useEffect(() => syncBackButton(stack.length > 0 && !pending, back), [back, stack.length, pending]);
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
    return campaign ? { ...view, cover: worldArt[storyPilot.templateId].cover, hero: worldArt[storyPilot.templateId].cover, goal: campaign.goal } : view;
  };
  const ownedView = (doc: WorldDocument) => storyView(doc, player ? pilotFor(doc, player.id) : undefined);
  const installPlayer = (p: Player) => { setPlayer(p); const doc = worldDocumentOf(p.world); const campaign = pilotFor(doc, p.id); setActiveCampaign(campaign); setWorld(storyView(doc, campaign)); return p; };
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

  const openWorld = async (w: World) => {
    if (sending.current || pending) return;
    setSelected(w); go('intro');
  };
  const startHero = (w: World) => {
    const def = worldDefinitionOf(w.id); setHeroBase(def); setDefinition(def); setHeroChoice(null); setPilotSelected(false); go('hero');
  };
  const chooseHero = (star?: WorldDefinition['stars'][number]) => {
    if (!definition || !heroBase) return;
    setHeroChoice(star?.id ?? 'custom');
    if (pilotSelected) { setDefinition(pilotDefinition(heroBase, star?.id ?? 'custom')); return; }
    if (!star) { setDefinition(heroBase); return; }
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
    setWorld(undefined); setActiveCampaign(undefined); setSelected(undefined); setPerson(undefined); setPost(undefined);
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
      const created = await api.createWorld(definition);
      const campaign = pilotSelected && heroChoice && definition.templateId === storyPilot.templateId ? campaignFor(heroChoice) : undefined;
      if (campaign && !saveLocal(`reki:v7:${created.id}:story:${created.worldInstanceId}`, pilotBinding(campaign))) setStorageError(true);
      const p = installPlayer(created); trackEvent('onboarding_step', { step: 'done', ok: true, world: p.world }); localSave('qualities', qualities);
      const own = await api.ownedWorlds(); installWorldContent(own); setOwned(own);
      setScreen('feed'); setStack([]); await loadFeed(p);
    } catch (cause) { setError(errorText(cause)); } finally { setBusy(false); }
  };
  const replyKey = (target: ReplyTarget) => `${key}:reply:${player?.world}:${target.postId}:${target.commentId ?? 'root'}`;
  const openCompose = (target?: ReplyTarget, context?: { name: string; text: string }) => {
    if (!player || sending.current) return;
    setReply(target); setQuote(context); setUnknown(false); setStatus('');
    if (target) { const saved = readLocal<ReplyDraft>(replyKey(target), { text: '' }); setDraft(saved.text); setUnknown(!!saved.requestId); }
    else { const saved = readPostDraft(draftKey(player)); setDraft(saved.text); setUnknown(!!saved.publicationId); }
    go('compose');
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
    try { const p = await api.post(id); if (threadRequest.current === request) { setPost(p); const next = activities.map(a => a.id === id ? { ...a, read: true } : a); setActivities(next); localSave('activity', next); } }
    catch (cause) { if (threadRequest.current === request) setError(errorText(cause)); }
    finally { if (threadRequest.current === request) setThreadLoading(false); }
  };
  const publish = () => {
    if (!player || !draft.trim() || sending.current) return;
    if (player.postsLeft <= 0 && !unknown) { trackEvent('limit_reached'); setOverlay('resources'); return; }
    if (reply) { void sendReply(); return; }
    const k = draftKey(player), saved = readPostDraft(k);
    const command = submissionFor(saved, draft, () => crypto.randomUUID());
    const { id, text, recovery } = command;
    if (!recovery) trackEvent('post_publish', { len: text.length, own_words: command.ownWords });
    if (!savePostDraft(k, { ...saved, text: draft, publicationId: id, submittedText: text, ownWords: true, submittedOwnWords: recovery ? saved.submittedOwnWords : true })) setStorageError(true);
    sending.current = true; setPending(true); setError(''); setStatus('pending');
    cancel.current = api.publish(text, id, event => {
      if (event.type !== 'result') return;
      trackEvent('post_result', { delta: event.result.delta, viral: event.result.viral, flop: event.result.flop });
      completePostDraft(k, id, text); setDraft(readPostDraft(k).text);
      setToast(event.result); setUnknown(false); setPending(false); sending.current = false; setStatus('completed');
      const item: Activity = { id: event.result.postId, worldId: player.world, title: event.result.headline, text: `${event.result.delta > 0 ? '+' : ''}${event.result.delta} подписчиков`, read: false };
      setActivities(old => { const next = [item, ...old.filter(a => a.id !== item.id)].slice(0, 100); localSave('activity', next); return next; });
      setScreen('feed'); setStack([]); void refresh().catch(() => setFeedError('Публикация сохранена. Обнови ленту, чтобы увидеть результат.'));
    }, (state, reason) => {
      sending.current = false; setPending(false); setUnknown(state === 'unknown');
      if (state === 'failed') { const latest = readPostDraft(k); savePostDraft(k, { text: latest.text, ownWords: true }); }
      setError(state === 'unknown' ? 'Исход отправки пока неизвестен. Проверь статус — повторный пост не создаётся.' : reason ? 'Не получилось опубликовать: проверка содержания не пройдена. Текст сохранён.' : 'Не получилось опубликовать. Текст сохранён.');
    }, setStatus, command.ownWords, command.wantId, recovery);
  };
  const sendReply = async () => {
    if (!player || !reply || sending.current) return;
    const k = replyKey(reply), saved = readLocal<ReplyDraft>(k, { text: '' });
    const requestId = saved.requestId ?? crypto.randomUUID(), text = saved.submittedText ?? draft;
    saveLocal(k, { text: draft, requestId, submittedText: text });
    sending.current = true; setPending(true); setError('');
    let terminalFailure = false;
    try {
      const controller = new AbortController(); cancel.current = () => controller.abort();
      const result = await api.reply(reply.postId, reply.commentId, requestId, text, { signal: controller.signal, onEvent: event => { if (event.status === 'failed') terminalFailure = true; } });
      if (!result.post) throw new Error('Missing reply');
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
      <ResourceBadge type="crystal" value="—" onClick={() => setOverlay('resources')} />
      <button className="profile-access" aria-label="Профиль персонажа" onClick={() => go('profile')}><Avatar name={player?.name ?? ''} size="lg" /></button></header>
    {activeCampaign && <section className="section stack story-banner"><p className="caption muted">Сюжетная проба · День 1</p><strong>{storyPilot.scenario}</strong><p>{activeCampaign.goal}</p><Button variant="secondary" onClick={() => go('goals')}>Открыть первый день</Button></section>}
    {feedError && <div className="section"><Notice title="Не удалось обновить ленту" tone="error">{feedError}</Notice></div>}
    {!feed.length && !feedLoading && !feedError && <EmptyState title="Первое слово за тобой" action={<Button onClick={() => openCompose()}>Написать пост</Button>}>{world?.seedPost || 'Расскажи свою версию истории. Здесь появятся публикации и ответы.'}</EmptyState>}
    {feed.map(item => { const view = feedView(item, playerView(player!)); return card(view.post, view.author, view.count); })}
    {feedLoading && <div className="pending-line" role="status"><span className="spinner" />Загружаем ленту…</div>}
    <div className="section"><Button variant="secondary" busy={feedLoading} onClick={() => void loadFeed(player!, cursor ?? undefined)}>{cursor ? 'Показать ещё' : 'Обновить ленту'}</Button></div>
  </>;

  const worldActivities = activitiesForWorld(activities, player?.world);
  let content: ReactNode;
  if (booting) content = <EmptyState title="Открываем твои миры"><span className="spinner" /> Ещё немного…</EmptyState>;
  else if (screen === 'catalog' || screen === 'search') {
    const entries = rankCatalog(catalogEntries(templates, plannedWorlds), qualities);
    const filtered = filterCatalog(entries, query, genre);
    content = <><ScreenHeading title={screen === 'search' ? 'Найти свой мир' : 'Миры'} />
      {screen === 'search' && <div className="search-field"><TextField label="Название или тема" value={query} onChange={e => setQuery(e.target.value)} placeholder="Магия, приключения, общение…" /></div>}
      {screen === 'catalog' && <section className="catalog-preferences"><p className="caption muted">{qualities.length ? `Для тебя · ${qualities.join(' · ')}` : 'Все миры'}</p><button className="link-button" onClick={() => { interestOnly.current = true; go('onboarding'); }}>Изменить интересы</button></section>}
      {screen === 'catalog' && owned.length > 0 && <section className="saved-worlds"><h2 className="section-title">Мои миры</h2>{owned.map(d => <ResumeWorld key={d.id} world={ownedView(d)} subtitle={d.name} onClick={() => void openWorld(ownedView(d))} />)}</section>}
      <div className="filter-strip">{['Все', ...new Set(entries.map(({ world }) => world.genre))].map(g => <Chip key={g} selected={genre === g} onClick={() => setGenre(g)}>{g}</Chip>)}</div>
      {filtered.length ? <div className="world-grid">{filtered.map(({ key, world: w, available }) => <WorldCard key={key} world={w} disabled={!available} selected={available && selected?.id === w.id} onClick={available ? () => void openWorld(w) : undefined} />)}</div> : <EmptyState title="Такого мира пока нет" action={<Button onClick={() => { setQuery(''); setGenre('Все'); if (!templates.length) void boot(); }}>Сбросить поиск</Button>}>Попробуй другое название или убери фильтр.</EmptyState>}
      {player?.hitOwnWorldMilestone && <div className="section"><Button onClick={() => { setDefinition({ ...blankWorld(), name: player.name, avatar: player.handle, stars: [{ id: crypto.randomUUID(), name: '', role: '', about: '', voice: 'Живо и по делу', avatarSeed: 'guide', trait: 1, relation: 0 }] }); go('hero'); }}>Создать свой мир</Button></div>}
    </>;
  } else if (screen === 'intro' && selected) content = <WorldIntro world={selected} busy={busy} resume={owned.some(d => d.id === selected.id)} onBack={back} onEnter={() => void enter()} />;
  else if (screen === 'welcome') content = <Onboarding key="entry-welcome" initialStep={0} onTry={() => go('age')} onDone={() => go('age')} onBack={back} />;
  else if (screen === 'age') content = <AgeEntry initialAge={entryAge} minimumAge={14} requireConsent={true} onBack={value => { setEntryAge(value); back(); }} onContinue={continueAge} />;
  else if (screen === 'onboarding') content = <Onboarding key="entry-interests" initialStep={1} initialQualities={qualities} onBack={back} onDone={completeInterests} />;
  else if (screen === 'hero' && definition && definition.templateId) content = <><ScreenHeading title="Твой профиль в мире" back={back} /><section className="section stack">
    {selected && <div className="hero-world"><img src={selected.hero} alt="" /><div className="hero-world-copy"><strong>{selected.title}</strong><p className="caption muted">{selected.description}</p></div></div>}
    {definition.templateId === storyPilot.templateId && <section className="story-choice stack"><label className="hero-choice"><input type="checkbox" checked={pilotSelected} onChange={e => { setPilotSelected(e.target.checked); setHeroChoice(null); setDefinition(heroBase); }} /><span><strong>{storyPilot.scenario}</strong><small>Новая сюжетная проба</small></span></label>{pilotSelected && <><p>{storyPilot.premise}</p><p className="caption muted">Создаётся отдельный мир. Доступны завязка, публикации и разговоры первого дня. Зачёт и продолжение появятся позже.</p></>}</section>}
    <fieldset className="hero-choices"><legend>Кем войдёшь в мир?</legend>
      {(heroBase ?? definition).stars.map(star => <label key={star.id} className={`hero-choice${heroChoice === star.id ? ' is-selected' : ''}`}><input type="radio" name="hero" checked={heroChoice === star.id} onChange={() => chooseHero(star)} /><Avatar name={star.name} size="lg" /><span><strong>{star.name}</strong><small>{pilotSelected ? campaignFor(star.id)?.request.role ?? star.role : star.role}</small></span></label>)}
      <label className={`hero-choice${heroChoice === 'custom' ? ' is-selected' : ''}`}><input type="radio" name="hero" checked={heroChoice === 'custom'} onChange={() => chooseHero()} /><span className="hero-choice-custom" aria-hidden="true">✦</span><span><strong>Свой персонаж</strong><small>Выбрать имя и роль</small></span></label>
    </fieldset>
    {heroChoice === 'custom' && <div className="stack">
      <TextField label="Имя персонажа" maxLength={80} value={definition.name} onChange={e => setDefinition({ ...definition, name: e.target.value })} />
      <TextField label="Имя канала" maxLength={80} value={definition.avatar} onChange={e => setDefinition({ ...definition, avatar: e.target.value })} />
      <TextField label="Кто ты в этом мире" maxLength={80} value={definition.role} onChange={e => setDefinition({ ...definition, role: e.target.value })} />
      <div className="hero-address"><p className="caption">Как обращаться</p><div className="row">{([['she', 'она'], ['he', 'он'], ['nick', 'по нику']] as const).map(([id, label]) => <Chip key={id} selected={address === id} onClick={() => setAddress(id)}>{label}</Chip>)}</div></div>
    </div>}
    {pilotSelected && campaignFor(heroChoice) && <div className="stack"><h2>Твоя цель</h2><p>{campaignFor(heroChoice)!.goal}</p><p className="caption muted">Собеседники: {definition.stars.map(star => star.name).join(' · ')}</p></div>}
    <p className="caption muted">Дальше — завязка твоей истории.</p>
    <Button busy={busy} disabled={!heroChoice || !definition.stars.length || (heroChoice === 'custom' && (!definition.name.trim() || !definition.avatar.trim() || !definition.role.trim()))} onClick={() => void createWorld()}>{heroChoice ? `Войти как ${definition.name.trim() || '…'} →` : 'Выбери персонажа'}</Button></section></>;
  else if (screen === 'hero' && definition) content = <><ScreenHeading title="Свой мир" back={back} /><section className="section stack"><TextField label="Имя персонажа" maxLength={80} value={definition.name} onChange={e => setDefinition({ ...definition, name: e.target.value })} /><TextField label="Имя канала" maxLength={80} value={definition.avatar} onChange={e => setDefinition({ ...definition, avatar: e.target.value })} />
    <label>Как обращаться<select value={address} onChange={e => setAddress(e.target.value as Address)}><option value="nick">По имени</option><option value="she">Она</option><option value="he">Он</option></select></label>
    <TextField label="Кто ты в этом мире" value={definition.role} maxLength={80} onChange={e => setDefinition({ ...definition, role: e.target.value })} />
    <TextField label="Название мира" value={definition.title} onChange={e => setDefinition({ ...definition, title: e.target.value })} /><label>Описание<textarea value={definition.description} onChange={e => setDefinition({ ...definition, description: e.target.value })} maxLength={2000} /></label><TextField label="Место действия" value={definition.where} onChange={e => setDefinition({ ...definition, where: e.target.value })} /><TextField label="Первая завязка" value={definition.firstContext} onChange={e => setDefinition({ ...definition, firstContext: e.target.value })} /><h2>Собеседники</h2>{definition.stars.map((star, index) => <div className="stack" key={star.id}><TextField label={`Имя собеседника ${index + 1}`} value={star.name} onChange={e => setDefinition({ ...definition, stars: definition.stars.map((s, i) => i === index ? { ...s, name: e.target.value } : s) })} /><TextField label="Роль собеседника" value={star.role} onChange={e => setDefinition({ ...definition, stars: definition.stars.map((s, i) => i === index ? { ...s, role: e.target.value } : s) })} /><TextField label="О собеседнике" value={star.about} onChange={e => setDefinition({ ...definition, stars: definition.stars.map((s, i) => i === index ? { ...s, about: e.target.value } : s) })} /></div>)}<Button variant="secondary" disabled={definition.stars.length >= 3} onClick={() => setDefinition({ ...definition, stars: [...definition.stars, { id: crypto.randomUUID(), name: '', role: '', about: '', voice: 'Живо и по делу', avatarSeed: crypto.randomUUID(), trait: 1, relation: 0 }] })}>Добавить собеседника</Button>
    <label>Градус драмы<select value={definition.drama} onChange={e => setDefinition({ ...definition, drama: e.target.value as WorldDefinition['drama'] })}><option value="soft">Мягко</option><option value="mid">Средне</option><option value="hot">Остро</option><option value="off">Без хейта</option></select></label>
    <Button busy={busy} disabled={!definition.name.trim() || !definition.avatar.trim() || !definition.role.trim() || !definition.title.trim() || !definition.stars.length || definition.stars.some(star => !star.name.trim() || !star.role.trim())} onClick={() => void createWorld()}>Начать историю</Button></section></>;
  else if (screen === 'feed' && player && world) content = renderFeed();
  else if (screen === 'goals' && activeCampaign) content = <><ScreenHeading title="Первый день" subtitle={world?.title} /><StoryDay campaign={activeCampaign} onWrite={prompt => openCompose(undefined, { name: 'Сюжетная тема', text: prompt })} onFeed={() => go('feed')} /></>;
  else if (screen === 'goals' && world) content = <><ScreenHeading title="Цели" subtitle={world.title} /><section className="goal-hero"><div className="row"><span className="goal-symbol"><Icon name="goals" /></span><h2>Ваша цель</h2></div><p>{world.goal || 'Найти свой голос и начать разговор.'}</p></section><section className="section stack"><h2 className="section-title">Темы для разговора</h2><p className="caption muted">Вдохновение для постов. Здесь нет обязательного порядка и начисления за квесты.</p>{world.quests.map((q, i) => <button className="quest-row" key={q.id} onClick={() => { setQuote({ name: 'Тема', text: q.prompt }); go('topic'); }}><span className="quest-number">{String(i + 1).padStart(2, '0')}</span><strong>{q.title}</strong><span>↗</span></button>)}<Button onClick={() => openCompose()}>Написать о своём</Button></section></>;
  else if (screen === 'compose' && player) content = <><ScreenHeading title={reply ? 'Продолжить разговор' : 'Новая публикация'} back={back} /><section className="section stack composer-body">{(reply || quote) && <QuoteBlock name={reply?.name ?? quote!.name} text={reply?.text ?? quote!.text} />}<div className="row"><Avatar name={player.name} /><strong>{player.name}</strong><span className="caption muted">Публично</span></div><label className="sr-only" htmlFor="post-text">Текст публикации</label><textarea id="post-text" className="composer-text" placeholder="Что хочешь сказать?" value={draft} onChange={e => updateDraft(e.target.value)} disabled={pending || unknown} maxLength={reply ? 1000 : 2000} autoFocus /><div className="row between caption muted"><span>Черновик сохраняется</span><span>{draft.length} / {reply ? 1000 : 2000}</span></div>{!reply && <div className="approaches">{(['ask', 'position', 'humor'] as Approach[]).map((a, i) => <Chip key={a} selected={approach === a} onClick={() => setApproach(a)}>{['Вопрос', 'Мнение', 'Шутка'][i]}</Chip>)}</div>}{quote && !reply && <p className="caption muted">Контекст помогает написать пост. Связь цитирования на сервере пока недоступна.</p>}{pending && <div className="pending-line" role="status"><span className="spinner" />{statusLabels[status] ?? 'Отправляем ответ…'}</div>}{pending && <Button variant="secondary" onClick={stopWaiting}>Остановить ожидание</Button>}<Button className="full" size="lg" disabled={!draft.trim()} busy={pending} onClick={publish}>{unknown ? 'Проверить статус' : 'Опубликовать'} <span className="caption">· 1 энергия</span></Button></section></>;
  else if (screen === 'thread' && player && world) {
    const view = post ? detailView(post, world, playerView(player)) : undefined;
    const postAuthor = post && world.cast.find(p => `Star_${p.id}` === post.authorId);
    content = <><ScreenHeading title="Обсуждение" back={back} />{threadLoading && <div className="pending-line" role="status">Загружаем обсуждение…</div>}{view && card(view.post, view.author, post!.comments.length)}{post?.comments.map(comment => {
      const author: Person = comment.me ? playerView(player) : world.cast.find(p => `Star_${p.id}` === comment.authorId) ?? { id: comment.authorId ?? comment.id, name: comment.author, handle: comment.avatar ?? '', role: comment.role ?? '', boundary: '' };
      return <div key={comment.id} className={comment.parentCommentId ? 'thread-nested' : ''}>{comment.parentCommentId && <p className="reply-to caption muted">↳ Ответ на реплику</p>}{card({ id: comment.id, parentId: post.id, authorId: author.id, text: comment.hidden ? 'Комментарий скрыт' : comment.text, createdAt: '', likes: 0 }, author, 0, true)}{comment.star && !comment.hidden && world.cast.some(p => `Star_${p.id}` === comment.authorId) && <div className="reply-action"><Button variant="ghost" onClick={() => openCompose({ postId: post.id, commentId: comment.id, name: author.name, text: comment.text })}>Ответить {author.name}</Button></div>}</div>;
    })}{post && !post.comments.length && <EmptyState title="Здесь ещё нет ответов">Персонажи могут продолжить разговор после публикации.</EmptyState>}{postAuthor && post && <div className="section"><Button className="full" onClick={() => openCompose({ postId: post.id, commentId: null, name: postAuthor.name, text: post.text })}>Начать разговор с автором</Button></div>}</>;
  } else if (screen === 'profile' && player && world) content = <><ProfileHeader name={player.name} onBack={back} /><ProfileScore score={null} skills={[player.skills.brave.level, player.skills.humor.level, player.skills.topic.level]} skillLabels={skillLabels} skillProgress={[player.skills.brave.progress, player.skills.humor.progress, player.skills.topic.progress]} /><section className="profile-details"><p className="caption muted">Общий счёт появится после подключения новой системы целей.</p><button className="profile-hint" onClick={() => setOverlay('skills')}><span>●</span><span className="text-body">Развивай навыки в разговорах</span><span>›</span></button><button className="profile-goal" onClick={() => go('goals')}><strong>Ваша цель</strong><p>{world.goal}</p></button><SettingsRow label="Подписчики" value={String(player.subs)} onClick={() => setOverlay('skills')} /><Button variant="secondary" onClick={() => { go('account'); }}>Настройки профиля</Button></section></>;
  else if (screen === 'character' && person) content = <><ProfileHeader name={person.name} avatar={person.avatar} onBack={back} /><div className="character-bio"><span className="caption muted">@{person.handle} · AI-персонаж</span><p>{person.boundary || person.role}</p>{person.relation !== undefined && <p className="caption muted">Отношения: {person.relation > 0 ? "+" : ""}{person.relation}</p>}</div>{feed.filter(item => item.author === person.name).map(item => { const view = feedView(item, playerView(player!)); return card(view.post, view.author, view.count); })}<div className="section"><Button onClick={() => { setReply(undefined); setQuote({ name: person.name, text: person.boundary || person.role }); go('topic'); }}>О чём поговорить</Button></div></>;
  else if (screen === 'topic' && world) content = <><div className="topic-header"><button className="back-button" onClick={back}>Назад</button><img className="topic-cover" src={world.hero} alt="" /><h1>{quote?.name ?? world.title}</h1><p>{quote?.text ?? world.topic}</p></div><section className="topic-people">{world.cast.map(p => <PersonRow key={p.id} person={p} detail={p.relation === undefined ? undefined : `Отношения: ${p.relation > 0 ? "+" : ""}${p.relation}`} onClick={() => showPerson(p)} />)}<Button className="full" onClick={() => openCompose(undefined, quote)}>Написать</Button></section></>;
  else if (screen === 'messages' && world) content = <><ScreenHeading title="Личка" /><EmptyState title="Сначала — общий разговор" action={<Button onClick={() => go('feed')}>Вернуться в ленту</Button>}>Личные сообщения скоро появятся.</EmptyState><section className="section"><h2 className="section-title">Собеседники</h2>{world.cast.map(p => <PersonRow key={p.id} person={p} detail={p.relation === undefined ? undefined : `Отношения: ${p.relation > 0 ? "+" : ""}${p.relation}`} onClick={() => showPerson(p)} />)}</section></>;
  else if (screen === 'activity') content = <><ScreenHeading title="Уведомления" action={worldActivities.length > 0 && <button className="link-button caption" onClick={() => { const next = markWorldActivitiesRead(activities, player?.world); setActivities(next); localSave('activity', next); }}>Прочитать всё</button>} />{!worldActivities.length && <EmptyState title="Пока тихо">Здесь появятся результаты твоих публикаций на этом устройстве.</EmptyState>}{worldActivities.map(a => <button key={a.id} className={`activity-row ${a.read ? '' : 'activity-row--unread'}`} onClick={() => void openThread(a.id)}><span className="activity-marker"><Icon name="activity" /></span><span className="grow"><strong>{a.title}</strong><p>{a.text}</p><span className="caption muted">Открыть публикацию ↗</span></span></button>)}</>;
  else if (screen === 'account') content = <><ProfileHeader name={player?.name ?? 'Твой профиль'} label="Профиль" /><section className="account-worlds"><h2 className="section-title">Мои миры</h2>{owned.length ? owned.map(d => <ResumeWorld key={d.id} world={ownedView(d)} subtitle={d.name} onClick={() => void openWorld(ownedView(d))} />) : <Button onClick={() => go('catalog')}>Выбрать мир</Button>}</section><section className="account-settings"><SettingsRow label="Ресурсы" value={`${player?.postsLeft ?? '—'} энергии`} onClick={() => setOverlay('resources')} /><SettingsRow label="Размер текста" value={`${textScale * 100}%`} onClick={() => setOverlay('text-size')} /><SettingsRow label="Интересы" value={qualities.join(' · ') || 'Выбрать'} onClick={() => { interestOnly.current = true; go('onboarding'); }} /><SettingsRow label="О новой версии" onClick={() => setOverlay('about')} /><NominalSignOut onSignOut={beginEntry} disabled={pending || busy} /></section></>;
  else content = <EmptyState title="Выбери мир" action={<Button onClick={() => go('catalog')}>К мирам</Button>}>Продолжи сохранённую историю или начни новую.</EmptyState>;

  const inWorld = !!world && !['catalog', 'search', 'account', 'intro', 'onboarding', 'welcome', 'age', 'hero'].includes(screen);
  const showNav = !booting && ['catalog', 'search', 'account', 'feed', 'goals', 'messages', 'activity'].includes(screen);
  const nav = inWorld ? [ { id: 'feed', label: 'Лента', icon: 'feed' as const }, { id: 'goals', label: 'Цели', icon: 'goals' as const }, { id: 'compose', label: 'Написать', icon: 'compose' as const }, { id: 'messages', label: 'Личка', icon: 'messages' as const }, { id: 'activity', label: 'Уведомления', icon: 'activity' as const, badge: activities.filter(a => !a.read && a.worldId === player?.world).length } ] : [ { id: 'catalog', label: 'Главная', icon: 'home' as const }, { id: 'search', label: 'Поиск', icon: 'search' as const }, { id: 'account', label: 'Профиль', icon: 'profile' as const } ];
  if (hash.startsWith('#system')) return <DesignSystem />;
  return <div className="v7-stage"><div className={`app ${showNav ? 'app--nav' : ''}`}><div className="app-body" ref={main}><div className="sr-only" tabIndex={-1} ref={focusHeading}>{titleOf[screen]}</div>{storageError && <Notice title="Не удалось сохранить на устройстве" tone="error">Не закрывай вкладку: черновик остаётся в памяти до перезагрузки.</Notice>}{error && <div className="section"><Notice title="Не получилось завершить действие" tone="error">{error}</Notice>{!booting && !templates.length && <Button onClick={() => void boot()}>Повторить</Button>}</div>}{content}</div>
    {showNav && <BottomNav items={nav.map(n => ({ ...n, disabled: pending }))} active={screen} onChange={id => { tap('tab', { to: `v7.${id}` }); id === 'compose' ? openCompose() : go(id as Screen); }} />}
    {toast && <div className="result-toast" role="status"><button className="toast-content" onClick={() => { void openThread(toast.postId); setToast(undefined); }}><span className="toast-check">✓</span><span><strong>{toast.headline}</strong><span className="caption block">{toast.delta > 0 ? '+' : ''}{toast.delta} подписчиков · Читать ответы ↗</span></span></button><button className="toast-close" aria-label="Закрыть уведомление" onClick={() => setToast(undefined)}>×</button></div>}
    {overlay && <Dialog title={{ exit: 'Выйти из мира?', resources: 'Общие ресурсы', skills: 'Навыки и отношения', 'text-size': 'Размер текста', 'local-like': 'Реакция', quote: 'Цитата', about: 'Новая версия' }[overlay]} onClose={() => setOverlay(null)}>
      {overlay === 'exit' ? <><p>Публикации и черновик сохранятся. Вернёшься с того же места.</p><Button disabled={pending} onClick={() => { feedRequest.current++; setWorld(undefined); setScreen('catalog'); setStack([]); setOverlay(null); }}>Выйти к мирам</Button><Button variant="secondary" onClick={() => setOverlay(null)}>Остаться</Button></> : overlay === 'resources' ? <><p>Энергия общая для всех миров. Публикация и ответ расходуют по 1 энергии.</p><div className="resource-details"><span>Энергия <b>{player?.postsLeft ?? '—'}</b></span><span>Кристаллы <b>—</b></span></div><p className="caption muted">Кристаллы появятся после подключения этой механики. Покупки пока недоступны.</p><Button onClick={() => setOverlay(null)}>Продолжить</Button></> : overlay === 'text-size' ? <><div className="row" style={{ flexWrap: 'wrap' }}>{[1, 1.25, 1.5, 2].map(v => <Chip key={v} selected={textScale === v} onClick={() => setTextScale(v)}>{v * 100}%</Chip>)}</div><p>Так будет выглядеть текст в приложении.</p><Button onClick={() => setOverlay(null)}>Готово</Button></> : overlay === 'skills' ? <><p>Навыки растут после подтверждённых сервером действий. Высокий навык не гарантирует согласия собеседника.</p>{world?.cast.map(p => <PersonRow key={p.id} person={p} onClick={() => { setOverlay(null); showPerson(p); }} />)}</> : overlay === 'local-like' ? <><p>Реакция сохранена на этом устройстве. На отношения и награды она пока не влияет.</p><Button onClick={() => setOverlay(null)}>Понятно</Button></> : overlay === 'quote' ? <><QuoteBlock name={quote?.name ?? ''} text={quote?.text ?? ''} /><p className="caption muted">Связь цитирования пока не поддерживается. Можно взять текст как контекст нового поста.</p><Button onClick={() => { setOverlay(null); openCompose(undefined, quote); }}>Написать с этим контекстом</Button></> : <><p>Интерфейс v7. Миры, публикации и ответы работают с сервером. Личка, кристаллы и новая система квестов появятся позже.</p><p className="caption muted">Уведомления и реакции сохраняются на этом устройстве.</p><Button onClick={() => setOverlay(null)}>Понятно</Button></>}
    </Dialog>}
  </div></div>;
}

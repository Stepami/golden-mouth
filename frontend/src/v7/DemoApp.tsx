import {LoadingIndicator} from './ui/LoadingIndicator';
import { useEffect, useRef, useState } from "react";
import type {
  Approach,
  GameState,
  Person,
  Post,
  PublishCommand,
  Quest,
  World,
  WorldId,
} from "./domain/types";
import { getWorld, skillNames, worlds } from "./domain/worlds";
import {
  applyPublication,
  enterWorld,
  initialState,
  validatePost,
} from "./domain/game";
import { loadState, persistState } from "./adapters/storage";
import { demoGateway } from "./adapters/demoGateway";
import { setTelegramBack } from "./adapters/telegram";
import {
  Chip,
  ProfileHeader,
  ProfileScore,
  Avatar,
  Badge,
  BottomNav,
  Button,
  Dialog,
  EmptyState,
  Icon,
  IconButton,
  Notice,
  PersonRow,
  PostCard,
  Progress,
  ResourceBadge,
  ScreenHeading,
} from "./ui";
import { Account } from "./screens/Account";
import { Catalog } from "./screens/Catalog";
import { WorldIntro } from "./screens/WorldIntro";
import { Onboarding } from "./screens/Onboarding";
import { Composer } from "./screens/Composer";
import { Goals } from "./screens/Goals";
import { DesignSystem } from "./screens/DesignSystem";
type Screen =
  | "catalog"
  | "search"
  | "account"
  | "intro"
  | "onboarding"
  | "feed"
  | "goals"
  | "compose"
  | "messages"
  | "activity"
  | "profile"
  | "character"
  | "thread"
  | "topic";
type Overlay =
  | "exit"
  | "resources"
  | "reset"
  | "about"
  | "skills"
  | "text-size"
  | null;
const names: Record<Screen, string> = {
  catalog: "Каталог миров",
  search: "Поиск",
  account: "Глобальный профиль",
  intro: "Вход в мир",
  onboarding: "Первое знакомство",
  feed: "Лента мира",
  goals: "Цель и квесты",
  compose: "Публикация",
  messages: "Личные сообщения",
  activity: "Уведомления",
  profile: "Профиль персонажа",
  character: "Профиль собеседника",
  thread: "Обсуждение",
  topic: "Общая тема",
};
export default function App() {
  const [state, setState] = useState<GameState>(loadState);
  const [screen, setScreen] = useState<Screen>(() =>
    loadState().activeWorldId ? "feed" : "catalog",
  );
  const [history, setHistory] = useState<Screen[]>([]);
  const [system, setSystem] = useState(location.hash.startsWith("#system"));
  const [selectedWorld, setSelectedWorld] = useState<World>(worlds[2]);
  const [overlay, setOverlay] = useState<Overlay>(null);
  const [personId, setPersonId] = useState("");
  const [threadId, setThreadId] = useState("");
  const [questId, setQuestId] = useState<string | undefined>();
  const [parentId, setParentId] = useState<string | undefined>();
  const [quoteId, setQuoteId] = useState<string | undefined>();
  const [onboardingEntersWorld, setOnboardingEntersWorld] = useState(false);
  const [approach, setApproach] = useState<Approach>("ask");
  const [pending, setPending] = useState(false);
  const pendingRef = useRef(false);
  const [error, setError] = useState<string>();
  const [toast, setToast] = useState<{
    text: string;
    postId: string;
    worldId: WorldId;
  } | null>(null);
  const [storageError, setStorageError] = useState(false);
  const [failNext, setFailNext] = useState(false);
  const [textScale, setTextScale] = useState(() => {
    try {
      const v = Number(localStorage.getItem("reki-text-scale") ?? 1);
      return [1, 1.25, 1.5, 2].includes(v) ? v : 1;
    } catch {
      return 1;
    }
  });
  useEffect(() => {
    document.documentElement.style.setProperty(
      "--text-scale",
      String(textScale),
    );
    document.documentElement.dataset.largeText = String(textScale > 1);
    try {
      localStorage.setItem("reki-text-scale", String(textScale));
    } catch {
      /* Usable without storage. */
    }
  }, [textScale]);
  const scrollRef = useRef<HTMLDivElement>(null);
  const headingRef = useRef<HTMLDivElement>(null);
  const currentCommand = useRef<PublishCommand | undefined>(undefined);
  const world = state.activeWorldId ? getWorld(state.activeWorldId) : undefined;
  const save = world ? state.saves[world.id] : undefined;
  const player: Person = {
    id: "player",
    name: save?.playerName ?? "Игрок",
    handle: "you",
    role: world?.role ?? "Игрок",
    avatar: world?.playerAvatar ?? "assets/s1-imgEllipse6.png",
    boundary: "",
  };
  const author = (id: string) =>
    id === "player"
      ? player
      : (world?.cast.find((p) => p.id === id) ?? {
          id,
          name: "Собеседник",
          handle: id,
          role: "AI-персонаж",
          boundary: "",
        });
  const go = (to: Screen) => {
    setHistory((h) => [...h, screen]);
    setScreen(to);
    setToast(null);
  };
  const back = () => {
    const previous = history.at(-1);
    setHistory((h) => h.slice(0, -1));
    setScreen(previous ?? (world ? "feed" : "catalog"));
  };
  useEffect(() => {
    const handler = () => setSystem(location.hash.startsWith("#system"));
    window.addEventListener("hashchange", handler);
    return () => window.removeEventListener("hashchange", handler);
  }, []);
  useEffect(() => {
    setStorageError(!persistState(state));
  }, [state]);
  useEffect(() => {
    scrollRef.current?.scrollTo(0, 0);
    headingRef.current?.focus({ preventScroll: true });
  }, [screen]);
  useEffect(
    () =>
      setTelegramBack(!["catalog", "feed"].includes(screen) ? back : undefined),
    [screen, history],
  );
  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 7000);
    return () => clearTimeout(t);
  }, [toast]);
  const updateSave = (
    fn: (s: NonNullable<typeof save>) => NonNullable<typeof save>,
  ) => {
    if (!world) return;
    setState((s) => {
      const previous = s.saves[world.id];
      return previous
        ? { ...s, saves: { ...s.saves, [world.id]: fn(previous) } }
        : s;
    });
  };
  const onWorld = (w: World) => {
    setSelectedWorld(w);
    go("intro");
  };
  const onEnter = (name: string) => {
    if (!state.onboarded) {
      setOnboardingEntersWorld(true);
      setSelectedWorld({ ...selectedWorld, playerName: name });
      go("onboarding");
      return;
    }
    setState((s) => enterWorld(s, selectedWorld, name));
    setHistory([]);
    setScreen("feed");
  };
  const openCompose = (q?: Quest, reply?: string, quote?: string) => {
    if (pendingRef.current) return;
    setQuestId(q?.id);
    setParentId(reply);
    setQuoteId(quote);
    setError(undefined);
    currentCommand.current = undefined;
    go("compose");
  };
  const openThread = (id: string) => {
    setThreadId(id);
    go("thread");
    updateSave((s) => ({
      ...s,
      activities: s.activities.map((a) =>
        a.postId === id ? { ...a, read: true } : a,
      ),
    }));
  };
  const like = (id: string) =>
    updateSave((s) => ({
      ...s,
      posts: s.posts.map((p) =>
        p.id === id
          ? { ...p, liked: !p.liked, likes: p.likes + (p.liked ? -1 : 1) }
          : p,
      ),
    }));
  const showAuthor = (id: string) => {
    if (id === "player") {
      go("profile");
      return;
    }
    setPersonId(id);
    go("character");
  };
  const publish = async () => {
    if (!world || !save || pendingRef.current) return;
    const validation = validatePost(save.draft);
    if (validation) {
      setError(validation);
      return;
    }
    if (state.energy < 1) {
      setOverlay("resources");
      return;
    }
    pendingRef.current = true;
    setPending(true);
    setError(undefined);
    const command = currentCommand.current ?? {
      id: crypto.randomUUID(),
      worldId: world.id,
      text: save.draft,
      approach,
      questId,
      parentId,
      quoteId,
    };
    currentCommand.current = command;
    if (failNext) {
      setFailNext(false);
      await new Promise((r) => setTimeout(r, 400));
      setError("Не удалось отправить. Черновик сохранён.");
      pendingRef.current = false;
      setPending(false);
      return;
    }
    setScreen("feed");
    setHistory([]);
    try {
      const result = await demoGateway.publish(command, world, save);
      setState((s) => applyPublication(s, command, result));
      setToast({
        text: result.activity.title,
        postId: command.id,
        worldId: command.worldId,
      });
      currentCommand.current = undefined;
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "Не удалось получить результат.",
      );
      setScreen("compose");
    } finally {
      setPending(false);
      pendingRef.current = false;
    }
  };
  const renderPost = (post: Post, compact = false) => {
    const quoted = post.quoteId
      ? save?.posts.find((p) => p.id === post.quoteId)
      : undefined;
    return (
      <PostCard
        key={post.id}
        post={post}
        author={author(post.authorId)}
        compact={compact}
        replyCount={save?.posts.filter((p) => p.parentId === post.id).length}
        onLike={() => like(post.id)}
        onReply={() => openThread(post.id)}
        onQuote={() => openCompose(undefined, undefined, post.id)}
        onAuthor={() => showAuthor(post.authorId)}
        onTopic={post.id.endsWith("-seed") ? () => go("topic") : undefined}
        quoted={
          quoted
            ? { name: author(quoted.authorId).name, text: quoted.text }
            : undefined
        }
      />
    );
  };
  const resourceDialog = (
    <>
      <p>
        Энергия и кристаллы общие для всех миров. Навыки принадлежат персонажу
        внутри выбранного мира.
      </p>
      <div className="resource-details">
        <span>
          Энергия <b>{state.energy} / 10</b>
        </span>
        <span>
          Кристаллы <b>{state.crystals}</b>
        </span>
      </div>
      <p className="caption muted">
        В демо публикация расходует 1 энергию. Покупок и реальных платежей нет.
      </p>
      <Button
        onClick={() => {
          setState((s) => ({ ...s, energy: 10 }));
          setOverlay(null);
        }}
      >
        Пополнить энергию для демо
      </Button>
    </>
  );
  let content;
  if (screen === "catalog" || screen === "search")
    content = (
      <Catalog state={state} search={screen === "search"} onWorld={onWorld} />
    );
  else if (screen === "intro")
    content = (
      <WorldIntro
        key={selectedWorld.id}
        world={selectedWorld}
        resume={!!state.saves[selectedWorld.id]}
        onBack={back}
        onEnter={onEnter}
      />
    );
  else if (screen === "onboarding")
    content = (
      <Onboarding
        initialQualities={state.qualities}
        initialStep={onboardingEntersWorld ? 0 : 1}
        onBack={back}
        onDone={(qualities) => {
          setState((s) => ({
            ...(onboardingEntersWorld
              ? enterWorld(s, selectedWorld, selectedWorld.playerName)
              : s),
            onboarded: true,
            qualities,
          }));
          setScreen(onboardingEntersWorld ? "feed" : "account");
          setHistory([]);
        }}
      />
    );
  else if (screen === "account")
    content = (
      <Account
        textScale={textScale}
        onTextSize={() => setOverlay("text-size")}
        state={state}
        onWorld={onWorld}
        onBrowse={() => go("catalog")}
        onResources={() => setOverlay("resources")}
        onInterests={() => {
          setOnboardingEntersWorld(false);
          go("onboarding");
        }}
      />
    );
  else if (world && save) {
    if (screen === "feed")
      content = (
        <>
          <header className="world-header">
            <button
              className="world-switch"
              onClick={() => setOverlay("exit")}
              aria-label="Меню текущего мира"
            >
              <strong>
                {world.title} <span>⌄</span>
              </strong>
              <span>Мой мир</span>
            </button>
            <ResourceBadge
              type="energy"
              value={`${state.energy}/10`}
              onClick={() => setOverlay("resources")}
            />
            <ResourceBadge
              type="crystal"
              value={String(state.crystals)}
              onClick={() => setOverlay("resources")}
            />
            <button
              className="profile-access"
              aria-label="Профиль персонажа"
              onClick={() => go("profile")}
            >
              <Avatar name={player.name} src={player.avatar} size="lg" />
            </button>
          </header>
          {pending && (
            <div className="pending-line" role="status">
              <LoadingIndicator/>
              Публикуем…
            </div>
          )}
          {save.posts.filter((p) => !p.parentId).map((p) => renderPost(p))}
          <div className="feed-end caption muted"></div>
        </>
      );
    else if (screen === "goals")
      content = (
        <Goals
          world={world}
          save={save}
          onQuest={(q) => {
            const existing = save.posts.find((p) => p.questId === q.id);
            existing ? openThread(existing.id) : openCompose(q);
          }}
          onProfile={() => go("profile")}
        />
      );
    else if (screen === "compose") {
      const context = save.posts.find((p) => p.id === (parentId || quoteId));
      content = (
        <Composer
          name={player.name}
          avatar={player.avatar}
          draft={save.draft}
          onDraft={(draft) => {
            updateSave((s) => ({ ...s, draft }));
            currentCommand.current = undefined;
          }}
          approach={approach}
          onApproach={(a) => {
            setApproach(a);
            currentCommand.current = undefined;
          }}
          quest={world.quests.find((q) => q.id === questId)}
          context={
            context
              ? { name: author(context.authorId).name, text: context.text }
              : undefined
          }
          error={error}
          pending={pending}
          onPublish={publish}
          onBack={back}
        />
      );
    } else if (screen === "thread") {
      const post = save.posts.find((p) => p.id === threadId);
      const direct = save.posts.filter((p) => p.parentId === threadId);
      content = (
        <>
          <ScreenHeading title="Обсуждение" back={back} />
          {post && renderPost(post)}
          <section className="thread-label">
            <h2 className="section-title">Ответы · {direct.length}</h2>
          </section>
          {direct.length ? (
            direct.map((p) => (
              <div key={p.id}>
                {renderPost(p, true)}
                {save.posts
                  .filter((x) => x.parentId === p.id)
                  .map((n) => (
                    <div key={n.id}>
                      <p className="reply-to caption muted">
                        ↳ Ответ на реплику {author(p.authorId).name}
                      </p>
                      {renderPost(n, true)}
                    </div>
                  ))}
              </div>
            ))
          ) : (
            <EmptyState title="Первое слово за тобой">
              Здесь ещё нет ответов.
            </EmptyState>
          )}
          <div className="section">
            <Button
              className="full"
              onClick={() => openCompose(undefined, threadId)}
            >
              Ответить в обсуждении
            </Button>
          </div>
        </>
      );
    } else if (screen === "activity")
      content = (
        <>
          <ScreenHeading
            title="Уведомления"
            action={
              save.activities.length > 0 ? (
                <button
                  className="link-button caption"
                  onClick={() =>
                    updateSave((s) => ({
                      ...s,
                      activities: s.activities.map((a) => ({
                        ...a,
                        read: true,
                      })),
                    }))
                  }
                >
                  Прочитать всё
                </button>
              ) : undefined
            }
          />
          {!save.activities.length ? (
            <EmptyState
              title="Пока тихо"
              action={
                <Button onClick={() => openCompose(world.quests[0])}>
                  Начать разговор
                </Button>
              }
            >
              Здесь появятся ответы и объяснения изменений после публикации.
            </EmptyState>
          ) : (
            save.activities.map((a) => (
              <button
                className={`activity-row ${a.read ? "" : "activity-row--unread"}`}
                key={a.id}
                onClick={() => openThread(a.postId)}
              >
                <span className="activity-marker">
                  <Icon name="activity" />
                </span>
                <span className="grow">
                  <strong>{a.title}</strong>
                  <p>{a.description}</p>
                  <span className="activity-deltas">
                    {a.deltas.map((n, i) =>
                      n > 0 ? (
                        <Badge key={i} tone="accent">
                          {skillNames[i]} +{n}
                        </Badge>
                      ) : null,
                    )}
                    <Badge>
                      {a.followers > 0 ? "+" : ""}
                      {a.followers} подписчика
                    </Badge>
                  </span>
                  <span className="caption muted">Открыть публикацию ↗</span>
                </span>
              </button>
            ))
          )}
        </>
      );
    else if (screen === "messages")
      content = (
        <>
          <ScreenHeading title="Личка" />
          <EmptyState
            title="Сначала — общий разговор"
            action={
              <Button onClick={() => go("feed")}>Вернуться в ленту</Button>
            }
          >
            Личные сообщения скоро появятся.
          </EmptyState>
          <div className="section">
            <h2 className="section-title">Собеседники</h2>
            {world.cast.map((p) => (
              <PersonRow
                key={p.id}
                person={p}
                onClick={() => showAuthor(p.id)}
              />
            ))}
          </div>
        </>
      );
    else if (screen === "profile")
      content = (
        <>
          <ProfileHeader
            name={player.name}
            avatar={player.avatar}
            onBack={back}
          />
          <ProfileScore
            score={Math.round((save.completedQuests.length / 3) * 100)}
            skills={save.skills}
          />
          <section className="profile-details">
            <button
              className="profile-hint"
              onClick={() => setOverlay("skills")}
            >
              <span aria-hidden="true">●</span>
              <span className="text-body">
                Чтобы эффективно выполнять цель,
                <br />
                качай три главных скилла
              </span>
              <span aria-hidden="true">›</span>
            </button>
            <button className="profile-goal" onClick={() => go("goals")}>
              <span className="row">
                <span className="goal-symbol">
                  <Icon name="goals" />
                </span>
                <strong>Ваша цель</strong>
              </span>
              <p>{world.goal}</p>
            </button>
          </section>
        </>
      );
    else if (screen === "character") {
      const p = author(personId);
      const posts = save.posts.filter((x) => x.authorId === p.id);
      content = (
        <>
          <ProfileHeader name={p.name} avatar={p.avatar} onBack={back} />
          <div className="character-bio">
            <span className="caption muted">@{p.handle} · AI-персонаж</span>
            <p>{p.boundary}</p>
          </div>
          {posts.length ? (
            posts.map((p) => renderPost(p))
          ) : (
            <p className="section muted">Публикаций пока нет.</p>
          )}
        </>
      );
    } else if (screen === "topic")
      content = (
        <>
          <div className="topic-header">
            <button className="back-button" onClick={back}>
              Назад
            </button>
            <img
              className="topic-cover"
              src={world.media ?? world.cover}
              alt=""
            />
            <h1>{world.topic}</h1>
            <p>{world.quests[0].context}</p>
          </div>
          <section className="topic-people">
            {world.cast.map((p) => (
              <PersonRow
                key={p.id}
                person={p}
                detail={save.relations[p.id]}
                onClick={() => showAuthor(p.id)}
              />
            ))}
            <Button
              className="full"
              onClick={() =>
                openCompose(
                  world.quests.find(
                    (q) => !save.completedQuests.includes(q.id),
                  ),
                )
              }
            >
              Написать
            </Button>
          </section>
        </>
      );
  }
  if (!content)
    content = (
      <EmptyState
        title="Выбери мир"
        action={
          <Button
            onClick={() => {
              setState((s) => ({ ...s, activeWorldId: null }));
              setScreen("catalog");
            }}
          >
            К мирам
          </Button>
        }
      >
        Открой сохранённую историю или начни новую.
      </EmptyState>
    );
  const worldNav = [
    { id: "feed", label: "Лента", icon: "feed" as const },
    { id: "goals", label: "Цели", icon: "goals" as const },
    {
      id: "compose",
      label: "Написать",
      icon: "compose" as const,
      disabled: pending,
    },
    { id: "messages", label: "Личка", icon: "messages" as const },
    {
      id: "activity",
      label: "Уведомления",
      icon: "activity" as const,
      badge: save?.activities.filter((a) => !a.read).length,
    },
  ];
  const globalNav = [
    { id: "catalog", label: "Главная", icon: "home" as const },
    { id: "search", label: "Поиск", icon: "search" as const },
    { id: "account", label: "Профиль", icon: "profile" as const },
  ];
  const showNav = ![
    "intro",
    "onboarding",
    "compose",
    "profile",
    "character",
    "thread",
    "topic",
  ].includes(screen);
  return (
    <>
      <div className="demo-toolbar">
        <a href="#demo" className="toolbar-brand">
          реки®
        </a>
        <div>
          <a href="#demo" aria-current={!system ? "page" : undefined}>
            Прототип
          </a>
          <a href="#system" aria-current={system ? "page" : undefined}>
            Дизайн-система
          </a>
        </div>
        <a
          className="toolbar-download"
          href="/assets/design-tokens.json"
          download
        >
          Токены ↧
        </a>
      </div>
      {system ? (
        <DesignSystem />
      ) : (
        <div className="demo-stage">
          <aside className="demo-sidebar">
            <span className="eyebrow">SOCIAL RPG / 2026</span>
            <h1 className="studio-wordmark">
              реки<span>®</span>
            </h1>
            <p className="studio-intro">
              Место, где
              <br />
              твои слова
              <br />
              двигают историю.
            </p>
            <div className="studio-rule" />
            <p className="caption muted">ИНТЕРАКТИВНЫЙ ПРОТОТИП · 0.1</p>
            <p className="muted">
              Новый интерфейс.
              <br />
              Одна система для всех миров.
            </p>
            <div className="sidebar-links">
              <a href="#system">Компоненты и токены ↗</a>
              <a href="/v7-integration.html" target="_blank" rel="noreferrer">
                Как интегрировать ↗
              </a>
            </div>
          </aside>
          <div className={`app ${showNav ? "app--nav" : ""}`}>
            <div className="app-body" ref={scrollRef}>
              <div className="sr-only" tabIndex={-1} ref={headingRef}>
                {names[screen]}
              </div>
              {storageError && (
                <Notice tone="error" title="Не удалось сохранить">
                  Хранилище браузера недоступно. Оставь вкладку открытой, чтобы
                  не потерять прогресс.
                </Notice>
              )}
              {content}
            </div>
            {showNav && (
              <BottomNav
                items={world ? worldNav : globalNav}
                active={screen}
                onChange={(id) =>
                  id === "compose" ? openCompose() : go(id as Screen)
                }
              />
            )}
            {toast && (
              <div className="result-toast" role="status">
                <button
                  className="toast-content"
                  onClick={() => {
                    setState((s) => ({ ...s, activeWorldId: toast.worldId }));
                    setThreadId(toast.postId);
                    go("thread");
                  }}
                >
                  <span className="toast-check">✓</span>
                  <span>
                    <strong>{toast.text}</strong>
                    <span className="caption block">
                      Прочитать ответы и результат ↗
                    </span>
                  </span>
                </button>
                <button
                  className="toast-close"
                  aria-label="Закрыть уведомление"
                  onClick={(e) => {
                    e.stopPropagation();
                    setToast(null);
                  }}
                >
                  ×
                </button>
              </div>
            )}
          </div>
          <aside className="demo-inspector">
            <div className="inspector-heading">
              <span className="status-light" />
              Живое демо
            </div>
            <span className="eyebrow muted">Сейчас на экране</span>
            <h2>{names[screen]}</h2>
            <p className="muted">
              {world
                ? "Состояние и отношения сохраняются отдельно для этого мира."
                : "Выбери мир, познакомься с персонажами и начни разговор."}
            </p>
            <ol className="flow-list">
              <li className={!world ? "current" : ""}>Мир и персонаж</li>
              <li className={screen === "goals" ? "current" : ""}>
                Цель и первый квест
              </li>
              <li className={screen === "compose" ? "current" : ""}>
                Твоя публикация
              </li>
              <li
                className={
                  ["feed", "thread"].includes(screen) && !!world
                    ? "current"
                    : ""
                }
              >
                Ответы и продолжение
              </li>
              <li
                className={
                  ["activity", "profile"].includes(screen) ? "current" : ""
                }
              >
                Прогресс и отношения
              </li>
            </ol>
            <p className="caption muted demo-disclosure">
              Ответы NPC в демо подготовлены заранее.
            </p>
            <details className="demo-controls">
              <summary>Проверить состояния</summary>
              <label>
                <input
                  type="checkbox"
                  checked={failNext}
                  onChange={(e) => setFailNext(e.target.checked)}
                />
                Ошибка следующей отправки
              </label>
              <button onClick={() => setOverlay("reset")}>Сбросить демо</button>
              <button onClick={() => setOverlay("about")}>
                Что работает в демо
              </button>
            </details>
            <div className="inspector-bottom">
              <span>430 PX / MOBILE FIRST</span>
              <span>REACT + CSS</span>
            </div>
          </aside>
        </div>
      )}
      {overlay && (
        <Dialog
          title={
            overlay === "exit"
              ? "Выйти из мира?"
              : overlay === "resources"
                ? "Общие ресурсы"
                : overlay === "reset"
                  ? "Начать заново?"
                  : overlay === "text-size"
                    ? "Размер текста"
                    : overlay === "skills"
                      ? "Навыки и отношения"
                      : "О демонстрации"
          }
          onClose={() => setOverlay(null)}
        >
          {overlay === "exit" ? (
            <>
              <p>
                Публикации, черновик и отношения в мире {world?.title}{" "}
                сохранятся. Вернёшься с того же места.
              </p>
              <Button
                onClick={() => {
                  setState((s) => ({ ...s, activeWorldId: null }));
                  setScreen("catalog");
                  setHistory([]);
                  setOverlay(null);
                }}
              >
                Выйти к мирам
              </Button>
              <Button variant="secondary" onClick={() => setOverlay(null)}>
                Остаться
              </Button>
            </>
          ) : overlay === "resources" ? (
            resourceDialog
          ) : overlay === "reset" ? (
            <>
              <p>
                Будут удалены только данные этого демо на текущем устройстве.
                Данные рабочего приложения не затрагиваются.
              </p>
              <Button
                variant="danger"
                disabled={pending}
                onClick={() => {
                  setState(structuredClone(initialState));
                  setScreen("catalog");
                  setHistory([]);
                  setOverlay(null);
                  setToast(null);
                  currentCommand.current = undefined;
                }}
              >
                Сбросить демо
              </Button>
            </>
          ) : overlay === "text-size" ? (
            <>
              <div className="row" style={{ flexWrap: "wrap" }}>
                {[1, 1.25, 1.5, 2].map((v) => (
                  <Chip
                    key={v}
                    selected={textScale === v}
                    onClick={() => setTextScale(v)}
                  >
                    {v * 100}%
                  </Chip>
                ))}
              </div>
              <p>Так будет выглядеть текст в приложении.</p>
              <Button onClick={() => setOverlay(null)}>Готово</Button>
            </>
          ) : overlay === "skills" ? (
            <>
              <p>
                Публикации развивают навыки. Счёт показывает прогресс трёх
                разговоров первого дня.
              </p>
              {world?.cast.map((p) => (
                <PersonRow
                  key={p.id}
                  person={p}
                  detail={save?.relations[p.id]}
                />
              ))}
              <p className="caption muted">
                Баланс демонстрационный. Высокий навык не гарантирует согласия
                собеседника.
              </p>
            </>
          ) : (
            <>
              <p>
                Это автономный прототип интерфейса. Публикации и черновики
                сохраняются в браузере. Ответы NPC и значения навыков заданы
                сценариями, а не живой AI-моделью.
              </p>
              <p>
                Доступны четыре мира, три квеста первого дня в каждом,
                публикации, ответы, цитаты, реакции, профили и история событий.
                Личные сообщения обозначены как будущая функция.
              </p>
              <Button onClick={() => setOverlay(null)}>Понятно</Button>
            </>
          )}
        </Dialog>
      )}
    </>
  );
}

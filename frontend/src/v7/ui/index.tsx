import { defaultPortrait } from '../portraits';
import { SocialAction } from "./SocialAction";
export { SocialAction } from "./SocialAction";
import {
  useEffect,
  useId,
  useRef,
  useState,
  type ButtonHTMLAttributes,
  type ReactNode,
  type InputHTMLAttributes,
} from "react";
import type { Person, Post, World } from "../domain/types";
const iconFiles = {
  home: "nav-home.svg",
  search: "nav-search.svg",
  profile: "nav-profile.svg",
  feed: "nav-feed.svg",
  goals: "nav-goals.svg",
  compose: "nav-compose.svg",
  messages: "nav-messages.svg",
  activity: "nav-activity.svg",
  heart: "s1-imgIconHeart.svg",
  comment: "s1-imgIconMessageCircle.svg",
  quote: "s1-imgIconRepeat2.svg",
  more: "s1-imgIconMoreHorizontal.svg",
  arrow: "s1-imgIconArrowUpRight.svg",
  energy: "s1-imgVector2.svg",
  crystal: "s1-imgVector4.svg",
  member: "catalog-imgIconMemberFilled.svg",
};
export type IconName = keyof typeof iconFiles;
export function Icon({ name }: { name: IconName }) {
  return (
    <span className={`icon icon--${name}`} aria-hidden="true">
      <img src={`assets/${iconFiles[name]}`} alt="" />
    </span>
  );
}
export function Button({
  variant = "primary",
  size = "md",
  busy = false,
  children,
  className = "",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "md" | "lg";
  busy?: boolean;
}) {
  return (
    <button
      className={`button button--${variant} button--${size} ${className}`}
      {...props}
      disabled={props.disabled || busy}
      aria-busy={busy || undefined}
    >
      {busy ? <span className="spinner" /> : null}
      {children}
    </button>
  );
}
export function IconButton({
  icon,
  label,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  icon: IconName;
  label: string;
}) {
  return (
    <button className="icon-button" aria-label={label} {...props}>
      <Icon name={icon} />
    </button>
  );
}
export function Chip({
  selected,
  children,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { selected?: boolean }) {
  return (
    <button
      className={`chip ${selected ? "is-selected" : ""}`}
      aria-pressed={selected}
      {...props}
    >
      {children}
    </button>
  );
}
export function Badge({
  children,
  tone = "neutral",
}: {
  children: ReactNode;
  tone?: "neutral" | "accent" | "success";
}) {
  return <span className={`badge badge--${tone}`}>{children}</span>;
}
export function Avatar({
  name,
  src,
  size = "md",
}: {
  name: string;
  src?: string;
  size?: "sm" | "md" | "lg" | "xl";
}) {
  const [generated,setGenerated] = useState<{name:string;uri:string}>();
  const [failed, setFailed] = useState(false);
  useEffect(() => { let active=true; if(!src) void defaultPortrait(name).then(uri=>{if(active)setGenerated({name,uri});}).catch(()=>{}); return()=>{active=false;}; },[name,src]);
  const image = src || (generated?.name===name ? generated.uri : undefined);
  useEffect(() => setFailed(false), [image]);
  return (
    <span className={`avatar avatar--${size}`} aria-hidden="true">
      {image && !failed ? (
        <img src={image} alt="" onError={() => setFailed(true)} />
      ) : (
        name
          .split(" ")
          .map((x) => x[0])
          .slice(0, 2)
          .join("")
      )}
    </span>
  );
}
export function Progress({
  value,
  max = 100,
  label,
}: {
  value: number;
  max?: number;
  label: string;
}) {
  const percent = Math.min(100, Math.max(0, (value / max) * 100));
  return (
    <div
      className="progress"
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={max}
      aria-valuenow={Math.min(max, value)}
    >
      <span style={{ width: `${percent}%` }} />
    </div>
  );
}
export function TextField({
  label,
  hint,
  error,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  hint?: string;
  error?: string;
}) {
  const id = useId();
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        {...props}
        aria-invalid={!!error}
        aria-describedby={hint || error ? `${id}-help` : undefined}
      />
      {(error || hint) && (
        <span
          id={`${id}-help`}
          className={error ? "field-error text-body" : "caption muted text-body"}
        >
          {error || hint}
        </span>
      )}
    </div>
  );
}
export function EmptyState({
  title,
  children,
  action,
}: {
  title: string;
  children: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="empty-state">
      <span className="empty-mark" aria-hidden="true">
        *
      </span>
      <h2 className="section-title">{title}</h2>
      <p className="muted">{children}</p>
      {action}
    </div>
  );
}
export function Notice({
  title,
  children,
  tone = "neutral",
}: {
  title: string;
  children: ReactNode;
  tone?: "neutral" | "error" | "accent";
}) {
  return (
    <div
      className={`notice notice--${tone}`}
      role={tone === "error" ? "alert" : undefined}
    >
      <strong>{title}</strong>
      <div className="caption text-body">{children}</div>
    </div>
  );
}
export function ResourceBadge({ type, value, onClick, hint }: {
  type: "energy" | "crystal"; value: string; onClick: () => void; hint?: string;
}) {
  const hintId = useId();
  const button = <button className="resource" aria-label={`${type === "energy" ? "Энергия" : "Кристаллы"}: ${value}`} aria-describedby={hint ? hintId : undefined} onClick={onClick}><Icon name={type} /><b>{value}</b></button>;
  return hint ? <span className="resource-with-hint">{button}<span className="resource-tooltip" role="tooltip" id={hintId}>{hint}</span></span> : button;
}
export function WorldCard({
  world,
  selected,
  disabled = false,
  onClick,
}: {
  world: World;
  selected?: boolean;
  disabled?: boolean;
  onClick?: () => void;
}) {
  return (
    <button
      type="button"
      className={`world-card ${selected ? "world-card--selected" : ""}`}
      disabled={disabled}
      onClick={disabled ? undefined : onClick}
      aria-label={disabled ? `${world.title} — скоро` : `Открыть мир ${world.title}`}
    >
      <img className="world-cover" src={world.cover} alt="" />
      <span className="world-title">{world.title}</span>
      <span className="world-members">
        {!disabled && <Icon name="member" />}
        {disabled ? "Скоро" : world.members || "Открыть мир"}
      </span>
    </button>
  );
}
export function PersonRow({
  person,
  onClick,
  detail,
}: {
  person: Person;
  onClick?: () => void;
  detail?: string;
}) {
  const inner = (
    <>
      <Avatar name={person.name} src={person.avatar} />
      <span className="grow">
        <strong>{person.name}</strong>
        <span className="caption muted block text-body">{detail || person.role}</span>
      </span>
      {onClick && <span aria-hidden="true">↗</span>}
    </>
  );
  return onClick ? (
    <button className="person-row" onClick={onClick}>
      {inner}
    </button>
  ) : (
    <div className="person-row">{inner}</div>
  );
}
export function QuoteBlock({ name, text }: { name: string; text: string }) {
  return (
    <blockquote className="quote-block">
      <strong>{name}</strong>
      <p>{text}</p>
    </blockquote>
  );
}
export function PostCard({
  post,
  author,
  replyCount = 0,
  onLike,
  onReply,
  onQuote,
  onAuthor,
  onTopic,
  onReport,
  quoted,
  compact = false,
}: {
  post: Post;
  author: Person;
  replyCount?: number;
  onLike?: () => void;
  onReply?: () => void;
  onQuote?: () => void;
  onAuthor?: () => void;
  onTopic?: () => void;
  onReport?: () => void;
  quoted?: { name: string; text: string };
  compact?: boolean;
}) {
  return (
    <article
      className={`post ${compact ? "post--reply" : ""}`}
      data-post-id={post.id}
    >
      <button
        className="post-avatar"
        onClick={onAuthor}
        aria-label={`Профиль: ${author.name}`}
        disabled={!onAuthor}
      >
        <Avatar name={author.name} src={author.avatar} />
      </button>
      <div className="post-content">
        <div className="row between">
          <button
            className="author-name"
            onClick={onAuthor}
            disabled={!onAuthor}
          >
            {author.name}
          </button>
          <span className="post-meta"><span className="caption muted nowrap">{post.createdAt}</span>{onReport&&<button className="post-report" aria-label={`Пожаловаться на реплику ${author.name}`} onClick={onReport}>⋯</button>}</span>
        </div>
        <div className="caption muted">
          @{author.handle.replace(/^@/, "")} · {post.authorId === "player" ? "Вы" : "AI-персонаж"}
        </div>
        <p className="post-text">{post.text}</p>
        {quoted && <QuoteBlock {...quoted} />}
        {post.image && (
          <img
            className="post-image"
            src={post.image}
            alt="Вид на реку — фотография из обсуждения"
          />
        )}
        {onTopic && (
          <button className="topic-link" onClick={onTopic}>
            Общая тема <Icon name="arrow" />
          </button>
        )}
        <div className="post-actions">
          <SocialAction
            kind="heart"
            label={post.liked ? "Убрать реакцию" : "Нравится"}
            selected={post.liked}
            count={post.likes}
            onClick={onLike}
          />
          <SocialAction
            kind="comment"
            label="Открыть обсуждение"
            count={replyCount}
            onClick={onReply}
          />
          <SocialAction
            kind="quote"
            label="Цитировать публикацию"
            onClick={onQuote}
          />
        </div>
      </div>
    </article>
  );
}
export type NavItem = {
  id: string;
  label: string;
  icon: IconName;
  badge?: number;
  disabled?: boolean;
};
export function BottomNav({
  items,
  active,
  onChange,
}: {
  items: NavItem[];
  active: string;
  onChange: (id: string) => void;
}) {
  const ref = useRef<HTMLElement>(null);
  useEffect(() => {
    const nav = ref.current;
    const app = nav?.closest<HTMLElement>(".app");
    if (!nav || !app) return;
    const measure = () => app.style.setProperty("--rendered-nav-height", `${nav.getBoundingClientRect().height}px`);
    measure();
    const observer = typeof ResizeObserver === "undefined" ? undefined : new ResizeObserver(measure);
    observer?.observe(nav);
    window.addEventListener("resize", measure);
    return () => { observer?.disconnect(); window.removeEventListener("resize", measure); app.style.removeProperty("--rendered-nav-height"); };
  }, []);
  return (
    <nav ref={ref} className="bottom-nav" aria-label="Основная навигация">
      {items.map((item) => (
        <button
          key={item.id}
          disabled={item.disabled}
          onClick={() => onChange(item.id)}
          aria-current={active === item.id ? "page" : undefined}
          className={`${active === item.id ? "is-active" : ""} ${item.id === "compose" ? "nav-compose" : ""}`}
        >
          <span className="nav-symbol">
            <Icon name={item.icon} />
            {!!item.badge && <span className="nav-dot" />}
          </span>
          <span className="nav-label">{item.label}</span>
        </button>
      ))}
    </nav>
  );
}
export function Dialog({
  title,
  children,
  onClose,
}: {
  title: string;
  children: ReactNode;
  onClose: () => void;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  useEffect(() => {
    const previous = document.activeElement as HTMLElement;
    const d = ref.current;
    d?.showModal();
    return () => {
      d?.close();
      previous?.focus();
    };
  }, []);
  return (
    <dialog
      ref={ref}
      className="dialog"
      aria-labelledby={titleId}
      onCancel={(e) => {
        e.preventDefault();
        onClose();
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div className="dialog-inner">
        <div className="row between">
          <h2 id={titleId} className="title">
            {title}
          </h2>
          <button
            className="close-button"
            onClick={onClose}
            aria-label="Закрыть"
          >
            ×
          </button>
        </div>
        {children}
      </div>
    </dialog>
  );
}
export function QuestRow({
  index,
  title,
  status,
  onClick,
}: {
  index: number;
  title: string;
  status: "done" | "active" | "locked";
  onClick: () => void;
}) {
  return (
    <button
      className={`quest-row quest-row--${status}`}
      onClick={onClick}
      disabled={status === "locked"}
    >
      <span className="quest-number">
        {status === "done" ? "✓" : String(index).padStart(2, "0")}
      </span>
      <span className="grow">
        <strong>{title}</strong>
        <span className="caption muted block">
          {status === "done"
            ? "Разговор состоялся"
            : status === "active"
              ? "Можно начать"
              : "После предыдущего разговора"}
        </span>
      </span>
      <span aria-hidden="true">{status === "locked" ? "—" : "↗"}</span>
    </button>
  );
}
export function ScreenHeading({
  title,
  subtitle,
  back,
  action,
}: {
  title: string;
  subtitle?: string;
  back?: () => void;
  action?: ReactNode;
}) {
  return (
    <header className="screen-heading">
      {back && (
        <button className="back-button" onClick={back}>
          Назад
        </button>
      )}
      <div className="row between">
        <div>
          <h1 className="title">{title}</h1>
          {subtitle && <p className="caption muted">{subtitle}</p>}
        </div>
        {action}
      </div>
    </header>
  );
}

export function ProfileHeader({
  name,
  avatar,
  onBack,
  label = "Персонаж",
}: {
  name: string;
  avatar?: string;
  onBack?: () => void;
  label?: string;
}) {
  return (
    <header className="character-heading">
      {onBack && (
        <button className="back-button" onClick={onBack}>
          Назад
        </button>
      )}
      <div className="character-identity">
        <div>
          <span className="character-kicker">{label}</span>
          <h1>{name}</h1>
        </div>
        <Avatar name={name} src={avatar} size="xl" />
      </div>
    </header>
  );
}
export function ProfileScore({
  score,
  skills,
  skillLabels = ["Аргументация", "Эмпатия", "Харизма"],
  skillProgress,
}: {
  score: number | null;
  skills: [number, number, number];
  skillLabels?: string[];
  skillProgress?: [number, number, number];
}) {
  return (
    <section className="character-score">
      <div
        className={`character-score-number ${(score ?? 0) >= 100 ? "character-score-number--full" : ""}`}
      >
        <span>Счёт</span>
        <div>
          <b>{score ?? "—"}</b>
          {score !== null && <span>/100</span>}
        </div>
      </div>
      <div className="character-skills">
        {skillLabels.map((name, i) => (
          <div key={name}>
            <div className="row between">
              <span>{name}</span>
              <span>{skills[i]}</span>
            </div>
            <Progress label={skillProgress ? `${name}: до следующего уровня` : name} value={skillProgress ? skillProgress[i] : skills[i]} max={skillProgress ? 1 : 10} />
          </div>
        ))}
      </div>
    </section>
  );
}

export function ResumeWorld({
  world,
  subtitle,
  onClick,
}: {
  world: World;
  subtitle: string;
  onClick: () => void;
}) {
  return (
    <button
      className="resume-world"
      onClick={onClick}
      aria-label={`Продолжить ${world.title}`}
    >
      <img src={world.cover} alt="" />
      <span className="grow">
        <strong>{world.title}</strong>
        <span className="caption muted block">{subtitle}</span>
      </span>
      <Icon name="arrow" />
    </button>
  );
}
export function SettingsRow({
  label,
  value,
  onClick,
  disabled=false,
}: {
  label: string;
  value?: string;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <button disabled={disabled} className="settings-row" onClick={event => { event.currentTarget.focus({ preventScroll: true }); onClick(); }}>
      <span className="grow">
        <strong>{label}</strong>
        {value && <span className="caption muted block">{value}</span>}
      </span>
      <Icon name="arrow" />
    </button>
  );
}

/** Visible account action while the real sign-out flow is being prepared. */
export function NominalSignOut({ onSignOut, disabled = false }: { onSignOut?: () => void; disabled?: boolean } = {}) {
  const [open, setOpen] = useState(false);
  return <div className="account-signout">
    <Button variant="secondary" className="full" disabled={disabled} onClick={event => { event.currentTarget.focus({ preventScroll: true }); if (onSignOut) onSignOut(); else setOpen(true); }}>Выйти из аккаунта</Button>
    {open && <Dialog title="Выход из аккаунта" onClose={() => setOpen(false)}>
      <p>Выход пока недоступен. Аккаунт остаётся подключённым.</p>
      <Button className="full" onClick={() => setOpen(false)}>Понятно</Button>
    </Dialog>}
  </div>;
}

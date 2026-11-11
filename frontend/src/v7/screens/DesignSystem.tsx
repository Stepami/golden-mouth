import {ResultHistory} from '../publication/ResultHistory';
import {player as examplePlayer,results as exampleResults} from '../../api/mock';
import {ViewFilter,FeedFilter} from '../ui/FeedFilter';
import {ActivityItem} from '../ui/ActivityItem';
import { useState } from "react";
import designTokens from "../design-tokens.json";
import {
  ResumeWorld,
  SettingsRow,
  ProfileHeader,
  ProfileScore,
  Avatar,
  Badge,
  BottomNav,
  Button,
  Chip,
  Dialog,
  EmptyState,
  Icon,
  Notice,
  PostCard,
  Progress,
  QuestRow,
  TextField,
  WorldCard,
  type IconName,
} from "../ui";
import { worlds } from "../domain/worlds";
export function DesignSystem() {
  const night: Record<string, { $value: string }> = designTokens.$extensions.reki.modes.dark;
  const colors = Object.entries(designTokens.tokens).filter(([, token]) => token.$type === "color").map(([name, token]) => [name.replace("color-", ""), night[name]?.$value ?? String(token.$value)]);
  const [activityFilter,setActivityFilter]=useState<'events'|'results'>('events');
  const [feedFilter, setFeedFilter] = useState<'all' | 'mine'>('all');
  const [selected, setSelected] = useState("Все");
  const [dialog, setDialog] = useState(false);
  const [liked, setLiked] = useState(false);
  const [input, setInput] = useState("");
  const [nav, setNav] = useState("feed");
  const [notice, setNotice] = useState(false);
  return (
    <main className="system-page">
      <div className="system-hero">
        <span className="eyebrow">REKI / DESIGN SYSTEM / NIGHT</span>
        <h1>
          Одни правила.
          <br />
          Разные миры.
        </h1>
        <p>
          Атомы, компоненты и состояния, из которых собрано работающее
          приложение. React + чистый CSS.
        </p>
        <div className="row">
          <Badge tone="accent">Night v8</Badge>
          <a href="#demo" className="link-button">
            Открыть прототип ↗
          </a>
        </div>
      </div>
      <nav className="system-nav" aria-label="Разделы дизайн-системы">
        {[
          ["foundations", "Основы"],
          ["atoms", "Атомы"],
          ["components", "Компоненты"],
          ["states", "Состояния"],
          ["handoff", "Интеграция"],
        ].map(([id, title]) => (
          <a
            key={id}
            href={`#system/${id}`}
            onClick={() =>
              setTimeout(
                () =>
                  document
                    .getElementById(id)
                    ?.scrollIntoView({ behavior: "smooth" }),
                0,
              )
            }
          >
            {title}
          </a>
        ))}
      </nav>
      <section id="foundations" className="system-section">
        <div className="system-label">
          <span>01</span>
          <h2>Основы</h2>
        </div>
        <div className="system-content">
          <h3>Цвет</h3>
          <div className="swatches">
            {colors.map(([n, v]) => (
              <div key={n}>
                <div className="swatch" style={{ background: v }} />
                <b>{n}</b>
                <code>{v}</code>
              </div>
            ))}
          </div>
          <p className="system-note">
            Цвета следуют концептам: лайм — выбор мира; жёлтый — навыки и кольцо
            профиля; фиолетовый — энергия; бирюзовый — кристаллы; розовый —
            цель. Основные кнопки светло-серые, навигация чёрная и серая.
          </p>
          <h3>TT Norms Pro · Compact / Narrow</h3>
          <div className="type-specimen">
            <span className="display">Миры начинаются с разговора</span>
            <code>Narrow · Display · 36 / 40 · 600 · −4%</code>
            <span className="title">Новый сюжет каждый день</span>
            <code>Narrow · Title · 26 / 30 · 600 · −3%</code>
            <span className="specimen-20">
              Harry Potter · Gorpcore · Twin Peaks
            </span>
            <code>Narrow · Card title · 20 / 24 · 400</code>
            <p>Пиши от себя. У персонажей есть собственное мнение.</p>
            <code>Compact · Body · 16 / 24 · 400 / 600</code>
            <span className="caption muted">
              Подпись, метаданные, второстепенная информация
            </span>
            <code>Narrow · Caption / label · 13 / 18 · 400</code>
          </div>
          <h3>Ритм и форма</h3>
          <div className="spacing-scale">
            {[4, 8, 12, 16, 20, 24, 32, 40, 48].map((x) => (
              <div key={x}>
                <span style={{ height: x }} />
                <code>{x}</code>
              </div>
            ))}
          </div>
          <div className="radii">
            {[4, 12, 16, 24, 32, 40].map((x) => (
              <div key={x} style={{ borderRadius: x }}>
                {x}
              </div>
            ))}
          </div>
          <p className="system-note">
            Базовая сетка — 4 px. Поля экрана — 20 px; каталога — 16 px. Обложка
            — 40 px. Диалог — 32 px. Основные действия — от 44 × 44 px;
            вспомогательные — от 24 px.
          </p>
        </div>
      </section>
      <section id="atoms" className="system-section">
        <div className="system-label">
          <span>02</span>
          <h2>Атомы</h2>
        </div>
        <div className="system-content">
          <h3>Действия / Button</h3>
          <div className="example-row">
            <Button onClick={() => setNotice(true)}>Основная</Button>
            <Button variant="secondary" onClick={() => setDialog(true)}>
              Вторичная
            </Button>
            <Button variant="ghost" onClick={() => setNotice(false)}>
              Нейтральная
            </Button>
            <Button disabled>Недоступная</Button>
            <Button busy>Отправляем</Button>
          </div>
          {notice && (
            <Notice title="Действие выполнено" tone="accent">
              Это тот же Button, который публикует пост в прототипе.
            </Notice>
          )}
          <h3>Фильтры / Chip</h3>
          <div className="example-row">
            {["Все", "Фэнтези", "Реализм"].map((x) => (
              <Chip
                selected={x === selected}
                onClick={() => setSelected(x)}
                key={x}
              >
                {x}
              </Chip>
            ))}
          </div>
          <h3>Переключение ленты / FeedFilter</h3>
          <FeedFilter value={feedFilter} onChange={setFeedFilter}/>
          <h3>Иконки / Icon</h3>
          <div className="icon-gallery">
            {(
              [
                "feed",
                "goals",
                "compose",
                "messages",
                "activity",
                "search",
                "profile",
                "heart",
                "comment",
                "quote",
                "trash",
              ] as IconName[]
            ).map((n) => (
              <div key={n}>
                <Icon name={n} />
                <code>{n}</code>
              </div>
            ))}
          </div>
          <p className="system-note">
            Авторские залитые силуэты — для навигации. Контурные действия —
            внутри поста. SVG из Figma, без перерисовки.
          </p>
          <h3>Аватары, статусы, прогресс</h3>
          <div className="example-row">
            {(["sm", "md", "lg", "xl"] as const).map((s) => (
              <Avatar
                key={s}
                size={s}
                name="Игрок"
                src="assets/s1-imgEllipse6.png"
              />
            ))}
            <Avatar name="Гермиона Грейнджер" src={worlds[0].cast[0].avatar} />
            <Badge>AI-персонаж</Badge>
            <Badge tone="accent">Текущий квест</Badge>
            <Badge tone="success">Готово</Badge>
          </div>
          <Progress label="Пример прогресса" value={62} />
          <h3>Ввод / TextField</h3>
          <div className="example-columns">
            <TextField
              label="Имя персонажа"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Как к тебе обращаться?"
              hint="До 40 символов"
            />
            <TextField
              label="Ошибка валидации"
              defaultValue=""
              placeholder="Название"
              error="Добавь название"
            />
          </div>
        </div>
      </section>
      <section id="components" className="system-section">
        <div className="system-label">
          <span>03</span>
          <h2>Компоненты</h2>
        </div>
        <div className="system-content">
          <div className="component-grid">
            <div>
              <h3>WorldCard / Default · Selected</h3>
              <div className="world-grid system-worlds">
                {[worlds[0], worlds[2]].map((w) => (
                  <WorldCard
                    world={w}
                    key={w.id}
                    selected={w.id === "gorpcore"}
                    onClick={() => setDialog(true)}
                  />
                ))}
              </div>
            </div>
            <div>
              <h3>QuestRow / Active · Locked · Done</h3>
              <QuestRow
                index={1}
                title="Познакомиться с компанией"
                status="done"
                onClick={() => setNotice(true)}
              />
              <QuestRow
                index={2}
                title="Договориться о границах"
                status="active"
                onClick={() => setDialog(true)}
              />
              <QuestRow
                index={3}
                title="Оставить место для ответа"
                status="locked"
                onClick={() => {}}
              />
            </div>
          </div>
          <h3>PostCard / Media</h3>
          <div className="system-phone">
            <PostCard
              post={{
                id: "specimen",
                authorId: "katya",
                text: worlds[2].seedPost,
                createdAt: "12 мин",
                image: worlds[2].media,
                likes: 12 + (liked ? 1 : 0),
                liked,
              }}
              author={worlds[2].cast[0]}
              onLike={() => setLiked(!liked)}
              onReply={() => setDialog(true)}
              onQuote={() => setDialog(true)}
              replyCount={2}
            />
          </div>
          <h3>Уведомления: события и результаты</h3>
          <ViewFilter value={activityFilter} onChange={setActivityFilter} options={[{value:'events',label:'События'},{value:'results',label:'Результаты'}]} label="Раздел уведомлений"/>
          <p className="system-note">События: ответы, реакции и серия. Результаты: сохранённые итоги постов и комментариев. В истории нет автоскрытия, повторных начислений и наградных эффектов.</p>
          <h3>История результата / PublicationResult</h3>
          <p className="system-note">Демонстрационный снимок: раскрытие не запускает наградные эффекты и начисления.</p>
          <ResultHistory items={[{id:'example',createdAt:'2026-11-11T09:00:00+03:00',text:'Проверим обещание на конкретном примере.',receipt:{worldId:'example',actionId:'example',actionKind:'post',before:examplePlayer.skills,after:examplePlayer.skills,beforeSubscribers:examplePlayer.subs,result:exampleResults.normal}}]} onOpen={()=>setDialog(true)}/>
          <h3>Уведомление / ActivityItem</h3>
          <p className="caption muted">Заголовок — до двух строк. Текст — до трёх строк с многоточием. Нажатие открывает полный ответ. Прочитанная запись остаётся в списке.</p>
          <div className="system-phone">
            <ActivityItem title="Георгина Гречихина отвечает" actorName="Георгина Гречихина" text="Идея правильная, но давай сразу договоримся о правилах. Предлагаю разбирать по одной идее и сохранять ответы участников. Полный ответ откроется в обсуждении публикации, а здесь останется короткое превью." symbol="" read={false} onOpen={()=>setDialog(true)}/>
            <ActivityItem title="Реакции на твой пост" text="Проверим обещание нового сервиса на конкретном примере, прежде чем спорить о его пользе." detail="Реакций: 12" symbol="♡" read={false} onOpen={()=>setDialog(true)}/>
            <ActivityItem title="Первый день серии" text="Начало серии отмечено. Продолжи завтра постом или комментарием." symbol="🔥" read onOpen={()=>setDialog(true)}/>
          </div>
          <h3>ProfileHeader + ProfileScore</h3>
          <div className="system-phone">
            <ProfileHeader
              name="Игрок"
              avatar="assets/s1-imgEllipse6.png"
              onBack={() => setDialog(true)}
            />
            <ProfileScore score={66} skills={[4, 3, 2]} />
          </div>
          <h3>ResumeWorld + SettingsRow</h3>
          <div className="system-phone">
            <ResumeWorld
              world={worlds[0]}
              subtitle="Гарри Поттер"
              onDelete={()=>setDialog(true)}
              onClick={() => setDialog(true)}
            />
            <SettingsRow
              label="Размер текста"
              value="100%"
              onClick={() => setDialog(true)}
            />
          </div>
          <h3>Персонажи миров</h3>
          <div className="cast-gallery">
            {worlds
              .flatMap((w) => w.cast)
              .map((p) => (
                <div key={p.id}>
                  <Avatar name={p.name} src={p.avatar} size="xl" />
                  <span>{p.name}</span>
                </div>
              ))}
          </div>
          <h3>BottomNav / World</h3>
          <div className="nav-specimen">
            <BottomNav
              active={nav}
              onChange={setNav}
              items={[
                { id: "feed", label: "Лента", icon: "feed" },
                { id: "goals", label: "Цели", icon: "goals" },
                { id: "compose", label: "Написать", icon: "compose" },
                { id: "messages", label: "Личка", icon: "messages" },
                {
                  id: "activity",
                  label: "Уведомления",
                  icon: "activity",
                  badge: 1,
                },
              ]}
            />
          </div>
        </div>
      </section>
      <section id="states" className="system-section">
        <div className="system-label">
          <span>04</span>
          <h2>Состояния</h2>
        </div>
        <div className="system-content">
          <div className="example-columns">
            <Notice title="Готовим отклики">
              Публикация отправляется. Не создаём повторный запрос.
            </Notice>
            <Notice tone="error" title="Связь прервалась">
              Черновик сохранён. Повтор доступен после проверки статуса запроса.
            </Notice>
            <Notice tone="accent" title="Разговор продолжился">
              Эмпатия +2 · 3 новых подписчика. Перейти к своему посту.
            </Notice>
          </div>
          <EmptyState title="Пока тихо">
            Здесь появятся реакции на твои публикации.
          </EmptyState>
          <Button variant="secondary" onClick={() => setDialog(true)}>
            Открыть диалог
          </Button>
          <p className="system-note">
            Диалог удерживает фокус, закрывается по Escape и возвращает фокус в
            кнопку. Учитываются reduced motion и системные safe-area.
          </p>
        </div>
      </section>
      <section id="handoff" className="system-section">
        <div className="system-label">
          <span>05</span>
          <h2>Разработчикам</h2>
        </div>
        <div className="system-content">
          <h3>Слои без зависимости от демо</h3>
          <div className="architecture">
            <span>
              theme.css<small>Токены</small>
            </span>
            <b>→</b>
            <span>
              src/ui<small>Атомы и компоненты</small>
            </span>
            <b>→</b>
            <span>
              src/screens<small>Экраны</small>
            </span>
            <b>→</b>
            <span>
              SocialGateway<small>API приложения</small>
            </span>
          </div>
          <pre>
            <code>{`import { Button, PostCard } from './ui';\nimport './theme.css';\nimport './ui/ui.css';\n\n<Button onClick={publish} busy={pending}>\n  Опубликовать\n</Button>`}</code>
          </pre>
          <p>
            Демо хранит миры и черновики в браузере. Вместо demoGateway
            подключается адаптер сгенерированного NSwag-клиента. UI получает
            готовые результаты; числа и согласие NPC не рассчитываются на
            клиенте рабочего приложения.
          </p>
          <div className="example-row">
            <a
              className="button button--secondary"
              href="/assets/design-tokens.json"
              download
            >
              Скачать токены ↧
            </a>
            <a
              className="button button--ghost"
              href="/v7-integration.html"
              target="_blank"
              rel="noreferrer"
            >
              Руководство по интеграции ↗
            </a>
          </div>
        </div>
      </section>
      {dialog && (
        <Dialog
          title="Один компонент — разные сценарии"
          onClose={() => setDialog(false)}
        >
          <p>
            Диалог подходит для выхода из мира, сведений о ресурсах и подробного
            результата.
          </p>
          <Button onClick={() => setDialog(false)}>Понятно</Button>
        </Dialog>
      )}
    </main>
  );
}

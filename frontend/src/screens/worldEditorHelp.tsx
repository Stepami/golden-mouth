import { useLayoutEffect, useRef, type ReactNode } from 'react'
import type { Player, WorldDefinition } from '../api/types'
import { displayHandle, heroHandle } from '../app/handle'
import { ownWorldRole, type WorldDraft } from '../app/worldDraft'
import { dramaOptions } from '../app/worldPrototype'
import { Avatar, Card, Comment } from '../ui'

const topics = {
  emoji: ['Значок мира', 'Стоит рядом с названием мира в списке историй и в канале.'],
  title: ['Название мира', 'По этому названию узнаешь мир в «Моих историях», канале и на экране входа.'],
  description: ['О чём этот мир', 'Опиши место и что в нём происходит. Герои учитывают описание в ответах. Если завязка пуста, канал начнётся с этого текста.'],
  summary: ['Подпись на карточке', 'Короткая подпись видна в карточке редактора и в настройках мира. Если оставить пустой, подставится начало описания.'],
  where: ['Где всё происходит', 'Это место герои получают в контексте мира. Оно также подставляется после «в» на карточке, которой можно поделиться.'],
  firstContext: ['С чего начинается история', 'Эта сцена открывает канал перед первым постом. Герои опираются на неё, даже пока в канале пусто.'],
  firstPrompt: ['Подсказка для первого поста', 'Вопрос появится под завязкой в канале. Он даёт повод написать пост и может подсказать ход после короткого ответа героя.'],
  name: ['Имя персонажа', 'От этого имени ты пишешь посты и ответы. Оно видно в шапке канала и рядом с твоими сообщениями.'],
  role: ['Роль персонажа', 'Роль видна при выборе персонажа и в списке историй. Она помогает героям понять, чем ты занимаешься в мире. Характер задают твои посты и ответы.'],
  avatar: ['Ник персонажа', 'Виден под именем в канале. По нику и стилю собирается портрет персонажа.'],
  copyPersona: ['Скопировать персонажа', 'Кнопка подставит имя, ник и роль из мира с пометкой «Текущая» в «Моих историях». После копирования поля можно отредактировать. Подписчики и прогресс у нового мира свои.'],
  avatarStyle: ['Стиль портретов', 'Меняет внешний вид аватарок твоего персонажа и героев в канале, ленте и ответах.'],
  address: ['Как к тебе обращаться', 'Герои учитывают выбранное обращение в ответах. Настройка применяется к твоему профилю и в других мирах.'],
  personalization: ['Персонализация аватарки', 'Учитывает возраст и обращение в тех стилях портрета, где есть такие детали. При выключении портрет зависит от ника и стиля.'],
  heroes: ['Герои мира', 'Герои могут писать в ленте и отвечать на посты. Если их несколько, перед созданием мира можно сыграть за одного из них. Остальные останутся собеседниками.'],
  heroName: ['Имя героя', 'Видно у постов героя в ленте и рядом с его ответами.'],
  heroRole: ['Чем занимается герой', 'Занятие видно рядом с героем при выборе персонажа. Герой учитывает его, когда пишет посты и отвечает.'],
  heroAbout: ['Характер и история героя', 'Опиши, что важно герою и что уже произошло в его жизни. Это контекст, на котором он строит ответы и свои посты.'],
  heroVoice: ['Как герой общается', 'Опиши манеру речи. Например, отвечает коротко и с сухим юмором. Герой учитывает её, когда пишет.'],
  heroNickname: ['Ник героя', 'Если задан, его видно рядом с именем в ответе. По нику можно упомянуть героя в посте через @.'],
  trait: ['Главная черта героя', 'Это подсказка для ИИ о том, как герой обычно реагирует. Характер и история добавляют к ней детали.'],
  difficulty: ['Сложность', 'На высокой сложности сильный пост может принести больше подписчиков, а риск провала выше. Настройка действует на новые действия.'],
  drama: ['Градус драмы', 'Влияет на то, насколько острыми могут быть споры и реакции. «Без хейта» отключает хейт-реакции.'],
  rule: ['Особенности мира', 'Правила видны в настройках мира и передаются героям. Опиши, что здесь возможно и как это работает.'],
  postIdea: ['Идея для поста', 'Первые идеи видны под полем поста в блоке «О чём написать». Идею можно подставить в пост и дописать.'],
  demoPost: ['Пример поста', 'Это образец текста для героев. Они получают его как контекст, отдельно от уже случившихся событий и опубликованных постов.'],
  achievementTitle: ['Название достижения', 'Название оформляет карточку достижения. Условие получения задаёт игра.'],
  achievementEmoji: ['Значок достижения', 'Значок виден в коллекции после открытия карточки. У закрытой карточки вместо значка стоит вопросительный знак.'],
  achievements: ['Достижения коллекции', 'Здесь можно оформить карточки. Условия получения задаёт игра. Название и значок сами по себе достижение не открывают.'],
} as const

export const heroTraits = ['Смелость', 'Поддержка', 'Загадка', 'Воображение', 'Любопытство', 'Энергия', 'Ирония и расчёт', 'Сухой юмор', 'Самоирония', 'Азарт', 'Внимание']
const traitReplies = [
  'Ещё никто не пробовал открыть башню. Самое время.',
  'Давай разберём карту вместе. С чего начнём?',
  'Башню закрыли по причине, о которой почти никто не помнит.',
  'А если карта нарисована для тех, кто смотрит на неё через воду?',
  'Когда на карте появилась эта отметка? Кто-нибудь успел её сфотографировать?',
  'Собираемся у башни через час! Кто берёт фонарь?',
  'Наконец-то приглашение, которое стоит проверить. Сначала выясним, кто его оставил.',
  'Карта промокла. Надеюсь, башня умеет ждать.',
  'Я снова перепутал правый берег с левым. Хорошо, что мост всего один.',
  'Спорим, я найду вход прежде, чем погаснет маяк?',
  'Посмотри на угол карты. Там такая же отметка, как на двери.',
]

type Topic = keyof typeof topics
export type WorldHelpTarget = {
  topic: Topic
  value?: string
  star?: WorldDefinition['stars'][number]
  achievement?: WorldDefinition['achievements'][number]
}

export function WorldCardPreview({ title, emoji, description }: { title: string; emoji: string; description: string }) {
  return <div className="card world-editor-preview" aria-label="Карточка мира">
    <span aria-hidden="true">{emoji}</span><div><b>{title}</b><p className="small muted">{description}</p></div>
  </div>
}

function Example({ title, children }: { title: string; children: ReactNode }) {
  return <div className="world-help-example"><p className="small muted">{title}</p>{children}</div>
}

export function WorldEditorHelp({ target, draft, source, onClose }: {
  target: WorldHelpTarget; draft: WorldDraft; source?: { player: Player; title: string }; onClose: () => void
}) {
  const [title, copy] = topics[target.topic]
  const dialog = useRef<HTMLDialogElement>(null)
  useLayoutEffect(() => {
    const element = dialog.current
    const previous = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    element?.showModal()
    return () => { element?.close(); document.body.style.overflow = previous }
  }, [])
  return <dialog ref={dialog} className="world-switch-dialog world-help-dialog" aria-labelledby="world-help-title"
    onCancel={event => { event.preventDefault(); onClose() }} onClick={event => { if (event.target === event.currentTarget) onClose() }}>
    <div className="world-help-sheet">
      <h2 id="world-help-title">{title}</h2>
      <p>{copy}</p>
      <HelpPreview target={target} draft={draft} source={source} />
      <button type="button" className="btn secondary" onClick={onClose} autoFocus>Понятно</button>
    </div>
  </dialog>
}

function HelpPreview({ target: { topic, value, star, achievement }, draft, source }: {
  target: WorldHelpTarget; draft: WorldDraft; source?: { player: Player; title: string }
}) {
  const d = draft.definition
  const title = d.title.trim() || 'Город маяков'
  const emoji = d.emoji.trim() || '✨'
  const name = d.name.trim() || 'Мира'
  const role = ownWorldRole(draft) || 'Смотритель маяка'
  const handle = displayHandle(d.avatar) || '@mira'
  const avatarStyle = d.avatarStyle
  const profile = { address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }
  const channelIntro = d.firstContext.trim() || d.description.trim() || 'Сегодня погас старый маяк. У его двери лежит карта с новой отметкой.'
  const reply = (text: string) => <Example title="Условный ответ героя"><Card>
    <Comment seed={star?.avatarSeed || star?.id || 'sasha'} avatarStyle={avatarStyle} name={star?.name.trim() || 'Саша'}
      handle={star?.avatarSeed ? heroHandle(star.avatarSeed) : ''} star text={text} />
  </Card></Example>
  const persona = (caption: string, showRole = false) => <Example title={caption}><Card>
    <div className="editor-preview"><Avatar seed={d.avatar || 'mira'} avatarStyle={avatarStyle} profile={profile} />
      <div><b>{name}</b><p className="small muted">{showRole ? role : handle}</p>{!showRole && <p className="small muted">{emoji} {title}</p>}</div>
    </div>
  </Card></Example>
  const context = (text: string) => <Example title="Контекст для героев"><Card><p>{text}</p></Card></Example>
  switch (topic) {
    case 'title': case 'emoji': case 'summary':
      return <Example title="Карточка мира"><WorldCardPreview title={title} emoji={emoji}
        description={(d.summary !== 'Твоя история' && d.summary.trim()) || d.description.trim().slice(0, 200) || 'Город, где маяки помнят голоса тех, кто ушёл в море.'} /></Example>
    case 'description': case 'firstContext': case 'firstPrompt':
      return <Example title={topic === 'description' && d.firstContext.trim() ? 'Настройки мира' : 'Канал перед первым постом'}>
        <Card><b>{topic === 'description' && d.firstContext.trim() ? 'Все особенности мира' : 'Завязка'}</b>
          <p>{topic === 'description' && d.firstContext.trim() ? d.description.trim() || 'Маяки помнят голоса тех, кто ушёл в море.' : channelIntro}</p>
          {topic !== 'description' && <p className="muted">{d.firstPrompt.trim() || (topic === 'firstPrompt' ? 'Что ты расскажешь читателям о находке у маяка?' : '')}</p>}
        </Card>
      </Example>
    case 'where':
      return <Example title="Карточка «Поделиться»"><Card><p>А ты кем будешь в {d.where.trim() || 'городе маяков'}?</p></Card></Example>
    case 'name': case 'avatar': case 'avatarStyle': case 'personalization':
      return persona('Шапка канала')
    case 'role':
      return persona('Выбор персонажа', true)
    case 'copyPersona':
      return source ? <Example title={`Мир «${source.title}» · Текущая история`}><Card>
        <div className="editor-preview"><Avatar seed={source.player.handle} avatarStyle={source.player.avatarStyle} />
          <div><b>{source.player.name}</b><p className="small muted">{displayHandle(source.player.handle)}</p><p>{source.player.role}</p></div>
        </div>
      </Card></Example> : null
    case 'address':
      return reply(draft.address === 'she' ? `${name}, ты нашла карту?` : draft.address === 'he' ? `${name}, ты нашёл карту?` : `${name}, что на карте?`)
    case 'heroes': case 'heroName':
      return reply('На карте отмечен старый маяк. Проверим, что там?')
    case 'heroNickname':
      return <Example title="Как виден ник в ответе"><Card>
        <Comment seed={star?.avatarSeed || star?.id || 'sasha'} avatarStyle={avatarStyle} name={star?.name.trim() || 'Саша'}
          handle={heroHandle(star?.avatarSeed) || '@sasha'} star text="На карте отмечен старый маяк. Проверим, что там?" />
      </Card></Example>
    case 'heroRole':
      return <Example title="Выбор персонажа"><Card><div className="editor-preview">
        <Avatar seed={star?.avatarSeed || star?.id || 'sasha'} avatarStyle={avatarStyle} mode="default" />
        <div><b>{star?.name.trim() || 'Саша'}</b><p className="small muted">{star?.role.trim() || 'Картограф'}</p></div>
      </div></Card></Example>
    case 'heroAbout':
      return context(star?.about.trim() || 'Вырос среди рыбаков. Помнит все тропы к маяку и боится потерять последнюю карту.')
    case 'heroVoice':
      return context(star?.voice.trim() || 'Говорит короткими фразами и шутит без улыбки.')
    case 'trait':
      return <><b>{heroTraits[star?.trait ?? 1]}</b>{reply(traitReplies[star?.trait ?? 1])}</>
    case 'difficulty':
      return <Example title="Выбранная сложность"><Card><b>{{ easy: 'Легко', normal: 'Средне', hard: 'Сложно' }[d.difficulty]}</b></Card></Example>
    case 'drama': {
      const option = dramaOptions.find(item => item.id === d.drama)
      return option && <Example title="Пример ситуации"><Card><b>{option.icon} {option.title}</b><p>{option.example}</p></Card></Example>
    }
    case 'rule':
      return context(value?.trim() || 'Магия работает только после заката.')
    case 'postIdea':
      return <Example title="В редакторе поста"><Card><p className="small muted">О чём написать</p><div className="opt world-help-idea">{value?.trim() || 'У двери маяка я заметил странную отметку…'}</div></Card></Example>
    case 'demoPost':
      return context(value?.trim() || 'Вчера маяк погас раньше рассвета. Кто слышал, что случилось?')
    case 'achievementTitle': case 'achievementEmoji': case 'achievements':
      return <Example title="Карточка в коллекции"><Card><div className="world-help-achievement">
        <div><span className="slot" aria-hidden="true">?</span><span className="small muted">Закрыта</span></div>
        <div><span className="slot on" aria-hidden="true">{achievement?.emoji.trim() || '⭐'}</span><span className="small muted">Открыта</span></div>
        <b>{achievement?.title.trim() || 'Первый залёт'}</b>
      </div></Card></Example>
  }
}

import type {
  GameState,
  World,
  WorldSave,
  PublishCommand,
  PublishResult,
  Approach,
} from "./types";
export const initialState: GameState = {
  version: 1,
  onboarded: false,
  qualities: [],
  activeWorldId: null,
  saves: {},
  energy: 10,
  crystals: 4,
};
export function createSave(world: World, name = world.playerName): WorldSave {
  return {
    playerName: name,
    posts: [
      {
        id: `${world.id}-seed`,
        authorId: world.cast[0].id,
        text: world.seedPost,
        createdAt: "Сегодня",
        image: world.media,
        likes: 12,
      },
    ],
    draft: "",
    completedQuests: [],
    skills: [0, 0, 0],
    followers: 24,
    activities: [],
    relations: Object.fromEntries(world.cast.map((p) => [p.id, p.boundary])),
  };
}
export function enterWorld(
  state: GameState,
  world: World,
  name?: string,
): GameState {
  return {
    ...state,
    activeWorldId: world.id,
    saves: {
      ...state.saves,
      [world.id]: state.saves[world.id] ?? createSave(world, name),
    },
  };
}
export function validatePost(text: string): string | undefined {
  if (!text.trim()) return "Добавь текст перед публикацией.";
  if (text.length > 2000)
    return "В публикации должно быть не больше 2 000 символов.";
}
// Demo outcomes are authored fixtures. Free text is never represented as LLM-evaluated.
export function simulateResponse(
  command: PublishCommand,
  world: World,
): PublishResult {
  const deltas: Record<Approach, [number, number, number]> = {
    ask: [1, 2, 0],
    position: [2, 0, 1],
    humor: [0, 1, 2],
  };
  const lines: Record<Approach, string[]> = {
    ask: [
      "Спасибо, что сначала спрашиваешь. Можно обсудить детали — для меня важно сохранить озвученные условия.",
      "Поддержу разговор, но пока не обещаю участие. Давайте сначала договоримся о правилах.",
      "Согласен с тем, что решение нужно подтвердить каждому. Не будем говорить за всю компанию.",
    ],
    position: [
      "Понятная позиция. Но мне всё ещё нужны детали: одного заявления недостаточно.",
      "Не со всем согласен. Мои условия остаются прежними, давайте их не пропускать.",
      "Я услышал предложение. Вернёмся к нему, когда будут ответы на вопросы.",
    ],
    humor: [
      "Забавно. А теперь давай к тому, о чём мы договаривались?",
      "Шутка не отвечает на мой вопрос. Мне по-прежнему важно, чтобы условия подходили всем.",
      "Разрядить разговор получилось. Но с решением я пока не спешу.",
    ],
  };
  const root = {
    id: command.id,
    authorId: "player",
    text: command.text.trim(),
    createdAt: "Только что",
    parentId: command.parentId,
    quoteId: command.quoteId,
    questId: command.questId,
    approach: command.approach,
    likes: 0,
  };
  const replies = world.cast.map((p, i) => ({
    id: `${command.id}-npc-${i}`,
    authorId: p.id,
    text: lines[command.approach][i],
    createdAt: "Только что",
    parentId: i === 2 ? `${command.id}-npc-1` : command.id,
    likes: 0,
  }));
  const title =
    command.approach === "ask"
      ? "Разговор продолжился"
      : command.approach === "position"
        ? "Позиция вызвала спор"
        : "Юмор заметили, вопрос остался";
  return {
    commandId: command.id,
    posts: [root, ...replies],
    activity: {
      id: `${command.id}-result`,
      postId: command.id,
      title,
      description: `За публикацию «${command.text.trim().slice(0, 70)}${command.text.length > 70 ? "…" : ""}». ${command.approach === "ask" ? "Открытый вопрос дал собеседникам место для ответа." : "Собеседники откликнулись, но не подтвердили участие."}`,
      deltas: deltas[command.approach],
      followers:
        command.approach === "ask"
          ? 3
          : command.approach === "position"
            ? -1
            : 1,
      read: false,
    },
    relations: Object.fromEntries(
      world.cast.map((p) => [
        p.id,
        command.approach === "ask"
          ? "Готовы продолжить разговор. " + p.boundary
          : p.boundary,
      ]),
    ),
  };
}
export function applyPublication(
  state: GameState,
  command: PublishCommand,
  result: PublishResult,
): GameState {
  const save = state.saves[command.worldId];
  if (
    !save ||
    result.commandId !== command.id ||
    save.posts.some((p) => p.id === command.id)
  )
    return state;
  const questAlreadyDone =
    !!command.questId && save.completedQuests.includes(command.questId);
  const deltas = questAlreadyDone ? [0, 0, 0] : result.activity.deltas;
  return {
    ...state,
    energy: Math.max(0, state.energy - 1),
    saves: {
      ...state.saves,
      [command.worldId]: {
        ...save,
        posts: [...result.posts, ...save.posts],
        draft: "",
        completedQuests:
          command.questId && !questAlreadyDone
            ? [...save.completedQuests, command.questId]
            : save.completedQuests,
        skills: save.skills.map((n, i) => n + deltas[i]) as [
          number,
          number,
          number,
        ],
        followers: Math.max(0, save.followers + result.activity.followers),
        activities: [
          { ...result.activity, deltas: deltas as [number, number, number] },
          ...save.activities,
        ],
        relations: { ...save.relations, ...result.relations },
      },
    },
  };
}

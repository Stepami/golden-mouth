export type WorldId = string;
export type Approach = "ask" | "position" | "humor";
export type Person = {
  id: string;
  name: string;
  handle: string;
  avatar?: string;
  role: string;
  boundary: string;
  relation?: number;
  subscribers?: number;
};
export type Quest = {
  id: string;
  title: string;
  context: string;
  prompt: string;
  draft: string;
};
export type World = {
  id: WorldId;
  title: string;
  genre: string;
  cover: string;
  hero: string;
  members: string;
  tagline: string;
  description: string;
  introTitles?: string[];
  goal: string;
  role: string;
  playerName: string;
  playerAvatar?: string;
  cast: Person[];
  quests: Quest[];
  seedPost: string;
  topic: string;
  media?: string;
};
export type Post = {
  id: string;
  authorId: string;
  text: string;
  createdAt: string;
  image?: string;
  parentId?: string;
  quoteId?: string;
  questId?: string;
  approach?: Approach;
  likes: number;
  liked?: boolean;
};
export type Activity = {
  id: string;
  postId: string;
  title: string;
  description: string;
  deltas: [number, number, number];
  followers: number;
  read: boolean;
};
export type WorldSave = {
  playerName: string;
  posts: Post[];
  draft: string;
  completedQuests: string[];
  skills: [number, number, number];
  followers: number;
  activities: Activity[];
  relations: Record<string, string>;
};
export type GameState = {
  version: 1;
  onboarded: boolean;
  qualities: string[];
  activeWorldId: WorldId | null;
  saves: Partial<Record<WorldId, WorldSave>>;
  energy: number;
  crystals: number;
};
export type PublishCommand = {
  id: string;
  worldId: WorldId;
  text: string;
  approach: Approach;
  questId?: string;
  parentId?: string;
  quoteId?: string;
};
export type PublishResult = {
  commandId: string;
  posts: Post[];
  activity: Activity;
  relations: Record<string, string>;
};
export interface SocialGateway {
  publish(
    command: PublishCommand,
    world: World,
    save: WorldSave,
    signal?: AbortSignal,
  ): Promise<PublishResult>;
}

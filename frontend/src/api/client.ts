import { clearLandingWorld } from '../app/landing/experiment'
import { completeLandingPost } from '../app/landing/experiment';
// Р¤Р°СЃР°Рґ API РїРѕРІРµСЂС… NSwag Fetch-РєР»РёРµРЅС‚Р°.
// Р’Рѕ С„СЂРѕРЅС‚Рµ РќР•Рў СЃРµРєСЂРµС‚РѕРІ: runtime config РІРёРґРµРЅ РІ Р±СЂР°СѓР·РµСЂРµ. РљР»СЋС‡Рё LLM Рё С‚РѕРєРµРЅ Р±РѕС‚Р° Р¶РёРІСѓС‚ С‚РѕР»СЊРєРѕ РЅР° Р±СЌРєРµРЅРґРµ.
import { clearPlayerDrafts } from '../app/postDraft';
import { clearWorldDrafts } from '../app/worldDraft';
import { clearAvatarPersonalization } from '../app/avatarPersonalization';
import { clearReferral } from '../app/referral';
import { clearTokens, logout as revokeSession, registerPlayer } from './auth';
import { clearApiCache } from '../app/useApi';
import { setAnalyticsWorld, setTelemetryPlayerAvailable, trackLanding } from '../app/analytics';
import { clearWorldContent, installWorldContent, installWorldTemplates, postIdeasForWorld } from './worldContent';
import { error } from '../app/log';
import { sdk } from './transport';
import type * as Wire from './generated/reki-api';
import { SwaggerException } from './generated/reki-api';
import * as map from './mappers';
import { ApiError, data, stream } from './transport';
export { ApiError } from './transport';
import { sessionGeneration, assertSession, endSession } from './session';
import { parse } from 'valibot';
import { starGenerationEvent } from './streamEvents';
import type { PlayerDocument, PostsPage, Challenge, Dm, FeedItem, Friend, Player, Post, PostDetailResponse, WorldDocument, WorldDefinition, WorldId, WorldUpdate } from './types';
let sessionPlayer: Player | undefined;
let sessionIdentity: PlayerDocument | undefined;
let ownedWorldSnapshot: WorldDocument[] | undefined;
let templates: WorldDocument[] = [];
let templatesRequest: Promise<void> | undefined;
const worldId = (): string => {
    if (!sessionPlayer?.worldInstanceId)
        throw new Error('World must be loaded before this request');
    return sessionPlayer.worldInstanceId;
};
const playerAuthorId = (): string => {
    if (!sessionPlayer) throw new Error('Player must be loaded before this request');
    return `Player_${sessionPlayer.id.replaceAll('-', '').toLowerCase()}`;
};
const worldAuthorIds = (id: string): string[] => {
    const selected = ownedWorldSnapshot?.find(world => world.id === id);
    if (!selected) throw new Error('World must be loaded before this request');
    return [playerAuthorId(), ...selected.stars.map(star => `Star_${star.id}`)];
};
async function fetchTemplates(): Promise<void> {
    try {
        const loaded = (await data(sdk.worlds.getWorldTemplates(), false)).map(map.world);
        if (!loaded.length)
            throw new Error('No world templates returned');
        installWorldTemplates(loaded);
        templates = loaded;
    }
    catch (cause) {
        // A template 404 is a configuration failure, not a missing player.
        throw new Error('Unable to load world templates', { cause });
    }
}
function loadTemplates(): Promise<void> {
    if (templates.length)
        return Promise.resolve();
    return templatesRequest ??= fetchTemplates().finally(() => { templatesRequest = undefined; });
}
function rememberPlayer(player: Player): Player {
    try {
        if (player.worldInstanceId)
            localStorage.setItem(`reki:selected-world:v2:${player.id}`, player.worldInstanceId);
    }
    catch { /* memory fallback */ }
    setTelemetryPlayerAvailable(true);
    sessionIdentity = { ...player, energy: player.postsLeft };
    sessionPlayer = player;
    setAnalyticsWorld(player.worldInstanceId, player.difficulty);
    return player;
}
async function readSse<T>(response: Response, decode: (value: unknown) => T, onEvent: (event: T) => void | Promise<void>): Promise<void> {
    const generation = sessionGeneration();
    if (!response.body)
        throw new Error('Response has no event stream');
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    for (;;) {
        const { done, value } = await reader.read();
        buffer += done ? decoder.decode() : decoder.decode(value, { stream: true });
        for (;;) {
            const match = /\r?\n\r?\n/.exec(buffer);
            if (!match)
                break;
            const frame = buffer.slice(0, match.index);
            buffer = buffer.slice(match.index + match[0].length);
            const data = frame.split(/\r?\n/).filter(line => line.startsWith('data:')).map(line => line.slice(5).trimStart()).join('\n');
            if (data) {
                assertSession(generation);
                await onEvent(decode(JSON.parse(data)));
            }
        }
        if (done)
            break;
    }
}
async function generateStar(postId: string, replyWorldId: string, authorId: string, sourceCommentId: string | null, additional: boolean, signal?: AbortSignal, onTyping?: (authorId: string | null) => void) {
    for (let attempt = 0; attempt < 2; attempt++) {
        try {
            onTyping?.(authorId);
            let completed: { text: string; additionalStarId?: string | null } | undefined;
            const response = await stream({ url: '/posts/{postId}/replies/generation', path: { postId }, query: { worldId: replyWorldId }, body: { authorId, sourceCommentId, additional } }, signal);
            await readSse(response, value => parse(starGenerationEvent, value), event => {
                if (event.status === 'failed') {
                    const failure = new Error(event.code ?? 'ai_unavailable');
                    failure.name = event.code ?? 'ai_unavailable';
                    throw failure;
                }
                if (event.status === 'completed' && event.text) completed = { text: event.text, additionalStarId: event.additionalStarId };
            });
            if (!completed) throw new Error('Generation stream ended before completion');
            return completed;
        } catch (cause) {
            const code = cause instanceof Error ? cause.name : '';
            if (attempt === 1 || signal?.aborted || code === 'AbortError' || code === 'unsafe_generated_content' || code === 'unsafe_input' || code === 'crisis' || (cause instanceof ApiError && cause.status >= 400 && cause.status < 500)) throw cause;
        }
    }
    throw new Error('Generation failed');
}
const playerRequests = new Map<string, Promise<Player>>();
async function loadPlayer(worldId?: string): Promise<Player | undefined> {
    const generation = sessionGeneration();
    const profile = await data(sdk.player.getCurrentPlayer());
    const streak = await data(sdk.player.getPlayerStreak());
    assertSession(generation);
    const player = map.player(profile, streak ?? null);
    if (sessionIdentity?.id !== player.id)
        ownedWorldSnapshot = undefined;
    sessionIdentity = player;
    setTelemetryPlayerAvailable(true);
    clearReferral();
    const ownedWorlds = (await data(sdk.worlds.getOwnedWorlds())).map(map.world);
    assertSession(generation);
    ownedWorldSnapshot = ownedWorlds;
    let selected: string | undefined;
    try {
        selected = localStorage.getItem(`reki:selected-world:v2:${player.id}`) ?? undefined;
    }
    catch { /* memory fallback */ }
    const world = ownedWorlds.find(item => item.id === (worldId ?? sessionPlayer?.worldInstanceId ?? selected)) ?? ownedWorlds[0];
    if (!world)
        return undefined;
    installWorldContent([world]);
    return rememberPlayer({ ...player, postsLeft: player.energy, postsCap: Math.max(player.energy, player.settings.plus ? 20 : 10), worldInstanceId: world.id, difficulty: world.difficulty, avatarStyle: world.avatarStyle,
        world: world.id, name: world.name, handle: world.avatar, role: world.role, subs: world.subscribers,
        collection: world.achievements, bio: '', type: 'СЃРІРѕР№ РіРѕР»РѕСЃ',
        voice: world.voice, drama: world.drama,
    });
}
export const api = {
    clearSession: () => {
        const playerId = sessionIdentity?.id ?? sessionPlayer?.id;
        endSession();
        playerRequests.clear();
        clearApiCache();
        if (playerId) {
            clearPlayerDrafts(playerId);
            clearAvatarPersonalization(playerId);
            try {
                localStorage.removeItem(`reki:selected-world:v2:${playerId}`);
                localStorage.removeItem(`reki:own-world-unlock-seen:${playerId}`);
            }
            catch { /* optional storage */ }
        }
        clearWorldDrafts('onboarding');
        clearWorldContent();
        sessionPlayer = undefined;
        sessionIdentity = undefined;
        ownedWorldSnapshot = undefined;
        setTelemetryPlayerAvailable(false);
        setAnalyticsWorld(undefined);
    },
    logout: async () => {
        await revokeSession();
        clearLandingWorld();
        api.clearSession();
    },
    loadWorldTemplates: loadTemplates,
    invitation: async (signal?: AbortSignal): Promise<number> => Number((await data(sdk.player.getInvitation(signal))).bonus),
    restore: (): Promise<Player | undefined> => loadPlayer(),
    me: (worldId?: string): Promise<Player> => {
        const key = worldId ?? sessionPlayer?.worldInstanceId ?? '';
        const pending = playerRequests.get(key);
        if (pending)
            return pending;
        const request = loadPlayer(worldId).then(player => {
            if (!player) throw new Error('World must be created before this request');
            return player;
        }).finally(() => {
            if (playerRequests.get(key) === request)
                playerRequests.delete(key);
        });
        playerRequests.set(key, request);
        return request;
    },
    registerWebPush: async (subscription: Wire.WebPushSubscriptionRequest) => data(sdk.player.registerWebPush(subscription, AbortSignal.timeout(15000)).then(response => {
        if (response.status !== 200) throw new Error('Web Push registration was not confirmed');
        return response;
    })),
    exchangeEnergy: async () => data(sdk.player.exchangeEnergy()),
    claimOnboardingBonus: (postId: string, signal?: AbortSignal): Promise<boolean> => data(sdk.posts.claimOnboardingBonus(postId, signal)),
    playerId: (): string | undefined => sessionIdentity?.id,
    hasPlayer: (): boolean => sessionIdentity !== undefined,
    onboardingProfile: () => sessionIdentity,
    onboarding: async (body: Wire.OnboardingRequest): Promise<PlayerDocument> => {
        if (sessionIdentity)
            throw new Error('Onboarding has already been completed');
        const generation = sessionGeneration();
        const profile = await registerPlayer(body);
        assertSession(generation);
        const streak = await data(sdk.player.getPlayerStreak());
        assertSession(generation);
        sessionIdentity = map.player(profile, streak ?? null);
        setTelemetryPlayerAvailable(true);
        return sessionIdentity;
    },
    createWorld: async (definition: WorldDefinition): Promise<Player> => {
        const world = map.world(await data(sdk.worlds.createWorld({ ...definition, postIdeas: postIdeasForWorld(definition) })));
        return api.me(world.id);
    },
    deleteWorld: async (id: string) => {
        await data(sdk.worlds.deleteWorld(id));
        ownedWorldSnapshot = undefined;
        sessionPlayer = undefined;
        clearApiCache();
    },
    deletePost: async (id: string) => {
        await data(sdk.posts.deletePost(id));
        clearApiCache();
    },
    createPost: async (text: string, ownWords: boolean, repostId?: string) => {
        const response = await data(sdk.posts.createPost(worldId(), { text, ownWords, repostId: repostId ?? null }));
        const { postId: id, ...post } = map.post(response.post);
        if (!repostId && completeLandingPost()) trackLanding('landing_first_post');
        return { post: { ...post, id }, result: map.result(response.result), predictedAuthorId: response.predictedAuthorId };
    },
    initialComment: async (postId: string, authorId: string, signal?: AbortSignal): Promise<Post> => {
        const currentWorld = worldId();
        const generated = await generateStar(postId, currentWorld, authorId, null, false, signal);
        const saved = await data(sdk.posts.replyToPost(postId, currentWorld, { authorId, commentId: null, requestId: crypto.randomUUID(), text: generated.text }, signal));
        const { postId: id, ...post } = map.post(saved.post);
        return { ...post, id };
    },
    reply: async (postId: string, commentId: string | null, requestId: string, text: string, starAuthorId: string, options: {
        signal?: AbortSignal;
        onSaved?: (saved: { post: PostDetailResponse; energyLeft: number; subscribers: number }) => void;
        onTyping?: (authorId: string | null) => void;
        additionalStarId?: string;
        onAdditionalStar?: (starId: string | null) => void;
    } = {}): Promise<{ post: PostDetailResponse; energyLeft: number; subscribers: number }> => {
        const player = sessionPlayer;
        const replyWorldId = player?.worldInstanceId;
        if (!player || !replyWorldId) throw new Error('World must be loaded before this request');
        const save = async (authorId: string, parentCommentId: string | null, id: string, content: string) => {
            const result = await data(sdk.posts.replyToPost(postId, replyWorldId, { authorId, commentId: parentCommentId, requestId: id, text: content }, options.signal));
            const saved = { post: map.post(result.post), energyLeft: Number(result.energyLeft), subscribers: Number(result.subscribers) };
            if (sessionPlayer?.id === player.id && sessionPlayer.worldInstanceId === replyWorldId)
                sessionPlayer = { ...sessionPlayer, postsLeft: saved.energyLeft, subs: saved.subscribers };
            options.onSaved?.(saved);
            return { ...saved, commentId: result.commentId };
        };
        const playerReply = await save(`Player_${player.id.replaceAll('-', '')}`, commentId, requestId, text);
        try {
            const mainId = `${requestId}-star`;
            const main = playerReply.post.comments.some(comment => comment.requestId === mainId) && options.additionalStarId
                ? { text: '', additionalStarId: options.additionalStarId }
                : await generateStar(postId, replyWorldId, starAuthorId, playerReply.commentId, false, options.signal, options.onTyping);
            options.onAdditionalStar?.(main.additionalStarId ?? null);
            let latest = playerReply.post.comments.some(comment => comment.requestId === mainId)
                ? playerReply : await save(starAuthorId, playerReply.commentId, mainId, main.text);
            if (main.additionalStarId) {
                const extraId = `${requestId}-extra`;
                if (!latest.post.comments.some(comment => comment.requestId === extraId)) {
                    const additionalAuthorId = `Star_${main.additionalStarId}`;
                    const additional = await generateStar(postId, replyWorldId, additionalAuthorId, playerReply.commentId, true, options.signal, options.onTyping);
                    latest = await save(additionalAuthorId, null, extraId, additional.text);
                }
            }
            return latest;
        } finally {
            options.onTyping?.(null);
        }
    },
    report: (commentId: string, reason: string) => data(sdk.telemetry.reportComment({ commentId, reason })),
    plusInterest: async (interest = true): Promise<{
        bonus: number;
    }> => {
        return data(sdk.player.claimPlusInterest({ interest })).then(value => ({ bonus: Number(value.bonus) }));
    },
    referral: (): Promise<{
        code: string;
    }> => data(sdk.player.getReferralCode()),
    settings: async (patch: Partial<Player['settings']> & {
        address?: Player['address'];
    }) => {
        await data(sdk.player.updateSettings(patch));
        if (sessionPlayer) {
            const { address, ...settings } = patch;
            sessionPlayer = { ...sessionPlayer, address: address ?? sessionPlayer.address, settings: { ...sessionPlayer.settings, ...settings } };
        }
    },
    feed: async (selectedWorldId: string | undefined, cursor?: string, signal?: AbortSignal): Promise<PostsPage<FeedItem>> => {
        const id = selectedWorldId ?? worldId();
        const authors = worldAuthorIds(id);
        const page = await data(sdk.posts.getPosts(id, authors, cursor, undefined, undefined, signal));
        return { ...page, items: page.items.map(map.feed) };
    },
    channelPosts: async (selectedWorldId: string | undefined, cursor?: string, signal?: AbortSignal): Promise<PostsPage<FeedItem>> => {
        const page = await data(sdk.posts.getPosts(selectedWorldId ?? worldId(), [playerAuthorId()], cursor, undefined, undefined, signal));
        return { ...page, items: page.items.map(map.feed) };
    },
    likedPosts: async (selectedWorldId: string | undefined, cursor?: string, signal?: AbortSignal): Promise<PostsPage<FeedItem>> => {
        const player = sessionPlayer;
        const author = playerAuthorId();
        const id = selectedWorldId ?? worldId();
        const page = await data(sdk.posts.getPosts(id, worldAuthorIds(id), cursor, author, undefined, signal));
        if (sessionPlayer?.id !== player?.id || sessionPlayer?.worldInstanceId !== player?.worldInstanceId)
            throw new DOMException('World has changed', 'AbortError');
        return { ...page, items: page.items.map(map.feed) };
    },
    likePost: async (postId: string, signal?: AbortSignal): Promise<void> => {
        const player = sessionPlayer;
        try {
            await data(sdk.posts.likePost(postId, { authorId: playerAuthorId() }, signal));
            if (sessionPlayer?.id !== player?.id || sessionPlayer?.worldInstanceId !== player?.worldInstanceId)
                throw new DOMException('World has changed', 'AbortError');
        } finally {
            clearApiCache();
        }
    },
    togglePostLike: async (postId: string, signal?: AbortSignal): Promise<PostDetailResponse> => {
        await api.likePost(postId, signal);
        const saved = await api.post(postId);
        if (!saved) throw new Error('Post is unavailable');
        const { id, ...detail } = saved;
        return { ...detail, postId: id };
    },
    post: async (postId: string): Promise<Post | null> => {
        try {
            const response = await data(sdk.posts.getPost(postId, worldId()));
            if (response == null) return null;
            const { postId: id, ...detail } = map.post(response);
            return { ...detail, id };
        } catch (cause) {
            if (cause instanceof SwaggerException && cause.status === 204) return null;
            throw cause;
        }
    },
    friends: (): Promise<Friend[]> => data(sdk.social.getFriends()).then(friends => friends.map(map.friend)),
    dm: (starId: string): Promise<Dm> => data(sdk.social.getDirectMessages(starId, worldId())).then(map.dm),
    dmSend: (starId: string, text: string): Promise<{
        reply?: string;
        repliesLeft: number;
    }> => data(sdk.social.sendDirectMessage(starId, worldId(), { text })).then(value => ({ ...value, reply: value.reply ?? undefined, repliesLeft: Number(value.repliesLeft) })),
    forget: (starId: string, fact: string) => data(sdk.social.forgetStarMemory(starId, worldId(), { fact })),
    challenge: (): Promise<Challenge | null> => data(sdk.social.getActiveChallenge()).then(map.challenge),
    startChallenge: (friendId: string) => data(sdk.social.startChallenge({ friendId })),
    ownedWorldsSnapshot: (): WorldDocument[] | undefined => ownedWorldSnapshot ? structuredClone(ownedWorldSnapshot) : undefined,
    ownedWorlds: async (): Promise<WorldDocument[]> => { const generation = sessionGeneration(); const found = await data(sdk.worlds.getOwnedWorlds()).then(worlds => worlds.map(map.world)); assertSession(generation); ownedWorldSnapshot = found; return found; },
    saveWorld: async (worldId: string, definition: WorldDefinition): Promise<Player> => {
        await data(sdk.worlds.updateWorld(worldId, definition));
        clearApiCache();
        return api.me(worldId);
    },
    updateWorld: async (patch: WorldUpdate) => {
        if (!sessionPlayer?.worldInstanceId)
            throw new Error('World must be loaded before this request');
        const current = (await api.ownedWorlds()).find(world => world.id === sessionPlayer!.worldInstanceId);
        if (!current)
            throw new Error('World not found');
        const { id, subscribers: _subs, lock: _lock, ...definition } = current;
        return api.saveWorld(id, { ...definition, templateId: null, ...patch });
    },
    deleteAccount: async () => {
        await data(sdk.player.deleteCurrentPlayer());
        // Invalidate requests only after the server confirms deletion. Failed deletion keeps the game intact.
        api.clearSession();
        clearTokens();
        clearReferral();
    },
};

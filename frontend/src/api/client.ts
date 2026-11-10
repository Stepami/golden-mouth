// Р¤Р°СЃР°Рґ API РїРѕРІРµСЂС… NSwag Fetch-РєР»РёРµРЅС‚Р°.
// Р’Рѕ С„СЂРѕРЅС‚Рµ РќР•Рў СЃРµРєСЂРµС‚РѕРІ: runtime config РІРёРґРµРЅ РІ Р±СЂР°СѓР·РµСЂРµ. РљР»СЋС‡Рё LLM Рё С‚РѕРєРµРЅ Р±РѕС‚Р° Р¶РёРІСѓС‚ С‚РѕР»СЊРєРѕ РЅР° Р±СЌРєРµРЅРґРµ.
import { clearPlayerDrafts } from '../app/postDraft';
import { clearWorldDrafts } from '../app/worldDraft';
import { clearAvatarPersonalization } from '../app/avatarPersonalization';
import { clearReferral } from '../app/referral';
import { registerPlayer, clearTokens, logout as revokeSession } from './auth';
import { clearApiCache } from '../app/useApi';
import { setAnalyticsWorld, setTelemetryPlayerAvailable } from '../app/analytics';
import { clearWorldContent, installWorldContent, installWorldTemplates } from './worldContent';
import { error } from '../app/log';
import { sdk } from './transport';
import type * as Wire from './generated/reki-api';
import * as map from './mappers';
import { ApiError, data, stream } from './transport';
export { ApiError } from './transport';
import { sessionGeneration, assertSession, endSession } from './session';
import { parse } from 'valibot';
import { starGenerationEvent } from './streamEvents';
import type { PlayerDocument, PostsPage, Challenge, ChannelPost, Dm, FeedItem, Friend, GameEvent, LiveEvent, Player, Post, PostDetailResponse, PublicationState, PublicationStatus, PublicationFailure, WorldDocument, WorldDefinition, WorldId, WorldUpdate } from './types';
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
    sessionIdentity = { ...player, energy: player.postsLeft, experiments: player.experiments ?? {} };
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
async function loadPlayer(worldId?: string): Promise<Player> {
    const generation = sessionGeneration();
    const player = map.player(await data(sdk.player.getCurrentPlayer()));
    if (sessionIdentity?.id !== player.id)
        ownedWorldSnapshot = undefined;
    sessionIdentity = player;
    setTelemetryPlayerAvailable(true);
    const [ownedWorlds, invitation] = await Promise.all([
        data(sdk.worlds.getOwnedWorlds()).then(worlds => worlds.map(map.world)),
        data(sdk.player.getInvitation()).then(value => ({ bonus: Number(value.bonus) })),
    ]);
    assertSession(generation);
    ownedWorldSnapshot = ownedWorlds;
    let selected: string | undefined;
    try {
        selected = localStorage.getItem(`reki:selected-world:v2:${player.id}`) ?? undefined;
    }
    catch { /* memory fallback */ }
    const world = ownedWorlds.find(item => item.id === (worldId ?? sessionPlayer?.worldInstanceId ?? selected)) ?? ownedWorlds[0];
    if (!world)
        throw new ApiError(404, 'GET', '/worlds');
    // Saved-world routes also need the authoritative template match for covers/guidance.
    // A catalogue outage must not prevent opening the actual saved world.
    await loadTemplates().catch(() => undefined);
    assertSession(generation);
    installWorldContent([world]);
    return rememberPlayer({ ...player, postsLeft: player.energy, postsCap: player.energyWallet?.cap ?? Math.max(player.energy, player.settings.plus ? 20 : 10), firstPostInviteBonus: invitation.bonus, worldInstanceId: world.id, difficulty: world.difficulty, avatarStyle: world.avatarStyle,
        world: world.id, name: world.name, handle: world.avatar, role: world.role, subs: world.subscribers,
        wants: world.wants, collection: world.achievements, bio: '', type: 'СЃРІРѕР№ РіРѕР»РѕСЃ',
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
        api.clearSession();
    },
    energyWallet: () => data(sdk.player.getCurrentPlayer()).then(player=>{if(!player.energyWallet)throw Object.assign(new Error('Wallet is not supported by this API'),{code:'wallet_unavailable'});return map.energyWallet(player.energyWallet);}),
    exchangeEnergy: () => data(sdk.player.exchangeEnergy()).then(value=>({...value,wallet:value.wallet?map.energyWallet(value.wallet):null})),
    loadWorldTemplates: loadTemplates,
    me: (worldId?: string): Promise<Player> => {
        const key = worldId ?? sessionPlayer?.worldInstanceId ?? '';
        const pending = playerRequests.get(key);
        if (pending)
            return pending;
        const request = loadPlayer(worldId).finally(() => {
            if (playerRequests.get(key) === request)
                playerRequests.delete(key);
        });
        playerRequests.set(key, request);
        return request;
    },
    onboardingProfile: () => sessionIdentity,
    playerId: (): string | undefined => sessionIdentity?.id,
    hasPlayer: (): boolean => sessionIdentity !== undefined,
    onboarding: async (body: Wire.OnboardingRequest): Promise<PlayerDocument> => {
        if (sessionIdentity)
            throw new Error('Onboarding has already been completed');
        sessionIdentity =
            map.player(await registerPlayer(body));
        setTelemetryPlayerAvailable(true);
        return sessionIdentity;
    },
    createWorld: async (definition: WorldDefinition): Promise<Player> => {
        const world = map.world(await data(sdk.worlds.createWorld(definition)));
        return api.me(world.id);
    },
    /** Р“РµРЅРµСЂР°С†РёСЏ, Р·Р°С‚РµРј РјРѕРґРµСЂР°С†РёСЏ Рё Р°РЅРёРјР°С†РёСЏ РјР°СЃСЃРёРІР° СЂРµР°РєС†РёР№; РїСЂРё РѕР±СЂС‹РІРµ СЃРІРµСЂСЏРµРј СЃРѕС…СЂР°РЅС‘РЅРЅС‹Р№ СЃС‚Р°С‚СѓСЃ. */
    publish(text: string, _id: string, onEvent: (e: LiveEvent) => void, onError: (state: 'failed' | 'unknown', reason?: PublicationFailure) => void, onStatus?: (status: PublicationStatus) => void, ownWords = true, wantId?: string, recoveryOnly = false): () => void {
        let cancelled = false;
        const timer = setTimeout(() => {
            if (recoveryOnly) { onError('unknown'); return; }
            onStatus?.('generating');
            void api.createPost(text, ownWords, wantId).then(async response => {
                if (cancelled) return;
                onStatus?.('completed');
                onEvent({type:'result',result:response.result});
                const updatedPost = await api.initialComment(response.post.id, response.predictedAuthorId).catch(() => undefined);
                if (cancelled) return;
                const comment = updatedPost?.comments.at(-1);
                if (comment) onEvent({type: 'comment', comment});
            }).catch(() => { if (!cancelled) onError('unknown'); });
        }, 0);
        return () => { cancelled = true; clearTimeout(timer); };
    },
    createPost: async (text: string, ownWords: boolean, wantId?: string) => {
        const response = await data(sdk.posts.createPost(worldId(), { text, ownWords, wantId: wantId ?? null }));
        const { postId: id, ...post } = map.post(response.post);
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
        const generate = async (authorId: string, sourceCommentId: string, additional: boolean) => {
            for (let attempt = 0; attempt < 2; attempt++) {
                try {
                    options.onTyping?.(authorId);
                    let completed: { text: string; additionalStarId?: string | null } | undefined;
                    const response = await stream({ url: '/posts/{postId}/replies/generation', path: { postId }, query: { worldId: replyWorldId }, body: { authorId, sourceCommentId, additional } }, options.signal);
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
                    if (attempt === 1 || options.signal?.aborted || code === 'AbortError' || code === 'unsafe_generated_content' || code === 'unsafe_input' || code === 'crisis' || (cause instanceof ApiError && cause.status >= 400 && cause.status < 500)) throw cause;
                }
            }
            throw new Error('Generation failed');
        };
        const playerReply = await save(`Player_${player.id.replaceAll('-', '')}`, commentId, requestId, text);
        try {
            const mainId = `${requestId}-star`;
            const main = playerReply.post.comments.some(comment => comment.requestId === mainId) && options.additionalStarId
                ? { text: '', additionalStarId: options.additionalStarId }
                : await generate(starAuthorId, playerReply.commentId, false);
            options.onAdditionalStar?.(main.additionalStarId ?? null);
            let latest = playerReply.post.comments.some(comment => comment.requestId === mainId)
                ? playerReply : await save(starAuthorId, playerReply.commentId, mainId, main.text);
            if (main.additionalStarId) {
                const extraId = `${requestId}-extra`;
                if (!latest.post.comments.some(comment => comment.requestId === extraId)) {
                    const additionalAuthorId = `Star_${main.additionalStarId}`;
                    const additional = await generate(additionalAuthorId, playerReply.commentId, true);
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
        const page = await data(sdk.posts.getFeed(selectedWorldId ?? worldId(), cursor, undefined, signal));
        return { ...page, items: page.items.map(map.feed) };
    },
    channelPosts: async (selectedWorldId: string | undefined, cursor?: string, signal?: AbortSignal): Promise<PostsPage<ChannelPost>> => {
        const page = await data(sdk.posts.getChannelPosts(selectedWorldId ?? worldId(), cursor, undefined, signal));
        return { ...page, items: page.items.map(map.channelPost) };
    },
    post: async (postId: string): Promise<Post> => {
        return data(sdk.posts.getPost(postId, worldId())).then(map.post).then(({ postId: id, ...detail }) => ({ ...detail, id }));
    },
    friends: (): Promise<Friend[]> => data(sdk.social.getFriends()).then(friends => friends.map(map.friend)),
    dm: (starId: string): Promise<Dm> => data(sdk.social.getDirectMessages(starId, worldId())).then(map.dm),
    dmSend: (starId: string, text: string): Promise<{
        reply?: string;
        repliesLeft: number;
    }> => data(sdk.social.sendDirectMessage(starId, worldId(), { text })).then(value => ({ ...value, reply: value.reply ?? undefined, repliesLeft: Number(value.repliesLeft) })),
    forget: (starId: string, fact: string) => data(sdk.social.forgetStarMemory(starId, worldId(), { fact })),
    eventToday: (): Promise<GameEvent> => data(sdk.events.getTodayEvent(worldId())),
    answerEvent: (id: string, text: string) => data(sdk.events.answerEvent(id, worldId(), { text })),
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

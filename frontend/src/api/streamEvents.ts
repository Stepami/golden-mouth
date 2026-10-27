import * as v from 'valibot'
import type { PublicationStatusLiveEvent, ReplyLiveEvent } from './generated/reki-api'

// SSE bypasses the generated Fetch Blob decoder so events remain incremental.
// Validate frames before passing their generated DTO types to the UI.
const numeric = v.pipe(v.union([v.number(), v.pipe(v.string(), v.regex(/^-?\d+(?:\.\d+)?$/))]), v.transform(Number), v.finite())
const failure = v.object({ code: v.string(), category: v.string(), policyVersion: v.string() })
const reaction = v.object({ emoji: v.string(), count: numeric })
const commentReaction = v.object({ authorId: v.string(), author: v.string(), avatar: v.string(), star: v.boolean(), me: v.boolean(), emoji: v.string() })
const comment = v.object({
  id: v.string(), author: v.string(), avatar: v.string(), handle: v.string(), text: v.string(),
  star: v.boolean(), me: v.boolean(), hidden: v.boolean(), parentCommentId: v.nullable(v.string()),
  requestId: v.nullable(v.string()), authorId: v.string(), role: v.string(), depth: numeric, reactions: v.array(commentReaction),
})
const post = v.object({ postId: v.string(), authorId: v.string(), text: v.string(), views: numeric, reactions: v.array(reaction), comments: v.array(comment) })
export const publicationEvent = v.object({
  type: v.picklist(['pending', 'ready_for_moderation', 'completed', 'failed']), publicationId: v.string(), failure: v.nullish(failure),
}) satisfies v.GenericSchema<unknown, PublicationStatusLiveEvent>
export const replyEvent = v.object({
  status: v.picklist(['generating', 'completed', 'failed']), requestId: v.string(), text: v.nullable(v.string()),
  post: v.nullable(post), energyLeft: v.nullable(numeric), subscribers: v.nullable(numeric), code: v.nullable(v.string()),
}) satisfies v.GenericSchema<unknown, ReplyLiveEvent>

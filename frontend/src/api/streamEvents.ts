import * as v from 'valibot'
import type { PublicationStatusLiveEvent, ReplyGenerationLiveEvent } from './generated/reki-api'

// SSE bypasses the generated Fetch Blob decoder so events remain incremental.
// Validate frames before passing their generated DTO types to the UI.
const failure = v.object({ code: v.string(), category: v.string(), policyVersion: v.string() })
export const publicationEvent = v.object({
  type: v.picklist(['pending', 'ready_for_moderation', 'completed', 'failed']), publicationId: v.string(), failure: v.nullish(failure),
}) satisfies v.GenericSchema<unknown, PublicationStatusLiveEvent>
export const starGenerationEvent = v.object({
  status: v.picklist(['generating', 'completed', 'failed']), authorId: v.string(),
  text: v.nullable(v.string()), additionalStarId: v.nullable(v.string()), code: v.nullable(v.string()),
}) satisfies v.GenericSchema<unknown, ReplyGenerationLiveEvent>

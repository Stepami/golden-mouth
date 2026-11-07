import * as v from 'valibot'
import type { ReplyGenerationLiveEvent } from './generated/reki-api'

// SSE bypasses the generated Fetch Blob decoder so events remain incremental.
// Validate frames before passing their generated DTO types to the UI.
export const starGenerationEvent = v.object({
  status: v.picklist(['generating', 'completed', 'failed']), authorId: v.string(),
  text: v.nullable(v.string()), additionalStarId: v.nullable(v.string()), code: v.nullable(v.string()),
}) satisfies v.GenericSchema<unknown, ReplyGenerationLiveEvent>

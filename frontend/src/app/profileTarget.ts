import type { Player } from '../api/types'
import { contentOf } from '../api/worldContent'

/** Use stable server author IDs; names and avatars are not identities. */
export function profileTarget(authorId: string | undefined, player: Player): string | undefined {
  if (authorId === `Player_${player.id.replaceAll('-', '')}`) return 'me'
  return contentOf(player.world).stars.find(star => authorId === `Star_${star.id}`)?.id
}

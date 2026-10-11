/** Non-overlapping foreground intervals; stop counting after 60s without interaction. */
export class ActiveTime {
  private since: number
  private lastInteraction: number
  private visible: boolean
  private pending = 0
  constructor(now: number, visible: boolean) {
    this.since = now; this.lastInteraction = now; this.visible = visible
  }
  private advance(now: number) {
    if (this.visible) this.pending += Math.max(0, Math.min(now, this.lastInteraction + 60_000) - this.since)
    this.since = now
  }
  take(now: number): number {
    this.advance(now)
    const ms = Math.min(30_000, Math.floor(this.pending))
    this.pending = 0
    return ms
  }
  interact(now: number) { this.advance(now); this.lastInteraction = now }
  visibility(now: number, visible: boolean) {
    this.advance(now); this.visible = visible
    if (visible) this.lastInteraction = now
  }
}

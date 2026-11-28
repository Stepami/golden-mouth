import type { WorldId } from '../api/types'
import type { CrowdCue } from './resultHaptics'
import { avatarUri } from './avatarRenderer'

const pastels = ['#c2aaf0', '#9787d8', '#8ca5cd', '#d99bb7', '#a3c2b9', '#b7b1ce', '#8496bc', '#bca5db']
const coats = ['#ad91db', '#6d68a4', '#b56d91', '#7186b0', '#5e8e89', '#7f77ae', '#a798c7', '#bd869c']
const cheers = ['Класс!', 'Я твой фанат!', 'Люблю этот канал!', 'Вот это пост!', 'Огонь!', 'Браво!', 'Ещё пост!', 'Ждём продолжения!', 'В самое сердце!', 'Подписка!', 'В избранное!', 'Это моё!', 'Хочу ещё!', 'Супер!', 'Сильный пост!', 'Уже делюсь!']
const boos = ['Мимо', 'Не моё', 'Фу-фу!']
const cardColors = ['#d4fae7', '#e6dafa', '#ffdca8']
const scatter = (index: number) => ((Math.sin(index * 79.719 + 2.45) * 48193.71) % 1 + 1) % 1
const clamp = (value: number) => Math.min(1, Math.max(0, value))
const ease = (value: number) => 1 - (1 - value) ** 3

function crowdRows(amount: number): number[] {
  if (amount >= 500_000) return [30, 29, 27, 25, 23, 21, 19, 17, 15, 13, 11, 9]
  if (amount >= 100_000) return [22, 21, 19, 16, 14, 12, 10, 9]
  if (amount >= 50_000) return [15, 14, 13, 12, 10, 9]
  if (amount > 38) return [10, 9, 8, 7, 6]
  if (amount <= 7) return [amount]
  const targets = [9, 8, 8, 7, 6].slice(-Math.ceil(amount / 8))
  const total = targets.reduce((sum, row) => sum + row, 0)
  let cumulative = 0
  let used = 0
  return targets.map(row => {
    cumulative += row
    const next = Math.round(amount * cumulative / total)
    const columns = next - used
    used = next
    return columns
  })
}

function drawHead(ctx: CanvasRenderingContext2D, img: HTMLImageElement) {
  const sample = document.createElement('canvas')
  const width = sample.width = img.naturalWidth
  const height = sample.height = img.naturalHeight
  const pixels = sample.getContext('2d', { willReadFrequently: true })!
  pixels.drawImage(img, 0, 0)
  const { data } = pixels.getImageData(0, 0, width, height)
  let left = width, right = -1, top = height, bottom = -1
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (data[(y * width + x) * 4 + 3] <= 8) continue
      left = Math.min(left, x); right = Math.max(right, x)
      top = Math.min(top, y); bottom = Math.max(bottom, y)
    }
  }
  if (right < 0) return false
  const headWidth = right - left + 1, headHeight = bottom - top + 1
  const scale = Math.min(26 / headWidth, 26 / headHeight)
  const w = headWidth * scale, h = headHeight * scale
  ctx.drawImage(sample, left, top, headWidth, headHeight, 20 - w / 2, 30 - h, w, h)
  return true
}

async function makeSprite(index: number, world: WorldId, avatarStyle?: string) {
  const sprite = document.createElement('canvas')
  sprite.width = 80
  sprite.height = 112
  const ctx = sprite.getContext('2d')!
  ctx.scale(2, 2)
  const color = index % coats.length
  ctx.fillStyle = coats[color]
  ctx.beginPath()
  ctx.roundRect(4, 26, 32, 29, [12, 12, 3, 3])
  ctx.fill()
  ctx.strokeStyle = pastels[color] + '7a'
  ctx.beginPath()
  ctx.moveTo(12, 39)
  ctx.lineTo(10, 53)
  ctx.moveTo(29, 39)
  ctx.lineTo(31, 53)
  ctx.stroke()
  try {
    const img = new Image()
    const seed = avatarStyle === 'lorelei' ? `vecherelo-gallery-neighbor-${index + 1}` : `result-crowd-${index}`
    img.src = await avatarUri(seed, world, undefined, avatarStyle, 'head')
    await img.decode()
    if (drawHead(ctx, img)) return sprite
  } catch { /* Keep a silhouette if the avatar cannot load. */ }
  ctx.fillStyle = pastels[color]
  ctx.beginPath()
  ctx.ellipse(20, 17, 10, 13, 0, 0, 2 * Math.PI)
  ctx.fill()
  return sprite
}

type Figure = {
  id: number; x: number; y: number; size: number; depth: number; arrival: number; departure: number; side: number; duration: number
  exchange?: number
  sign?: { index: number; text: string; start: number }
}

type Placard = { figure: Figure; x: number; y: number; raise: number; opacity: number }

export async function prepareCrowd({ ctx, delta, world, avatarStyle, offset }: {
  ctx: CanvasRenderingContext2D; delta: number; world: WorldId; avatarStyle?: string; offset: number
}) {
  const amount = Math.abs(delta)
  const loss = delta < 0
  const end = loss ? 4400 : amount < 12 ? 11_100 : amount <= 38 ? 11_500 : 12_200
  const activeUntil = loss ? end : amount < 12 ? 3500 : amount <= 38 ? 4800 : 6900
  const rows = crowdRows(amount)
  const count = rows.reduce((sum, row) => sum + row, 0)
  const spriteLimit = avatarStyle === 'lorelei' ? 20 : 24
  const sprites = await Promise.all(Array.from({ length: Math.min(count, spriteLimit) }, (_, index) => makeSprite(index, world, avatarStyle)))
  const waves = amount >= 500_000 ? 5 : amount >= 100_000 ? 3 : amount >= 38 ? 2 : 1
  const canvas = ctx.canvas
  let people: Figure[] = []
  let width = 0
  let height = 0
  let light: CanvasGradient

  function resize() {
    width = canvas.clientWidth
    height = canvas.clientHeight
    if (!width || !height) return
    const dpr = Math.min(2, window.devicePixelRatio || 1)
    canvas.width = Math.round(width * dpr)
    canvas.height = Math.round(height * dpr)
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0)
    light = ctx.createRadialGradient(width / 2, 8, 3, width / 2, 41, width * .68)
    light.addColorStop(0, loss ? '#3b2d42' : '#373052')
    light.addColorStop(.55, '#22223a')
    light.addColorStop(1, '#15151f')
    const verticalScale = Math.min(1, height / 124)
    people = []
    for (const [row, columns] of rows.entries()) {
      const depth = rows.length === 1 ? .88 : row / (rows.length - 1)
      const spacing = Math.min(45, (width - 16) / columns)
      const margin = (width - spacing * columns) / 2
      for (let col = 0; col < columns; col++) {
        const id = people.length
        const x = margin + (col + .5) * spacing + (scatter(id + 47) - .5) * spacing * .5
        const wave = Math.min(waves - 1, Math.floor((depth * .70 + scatter(id + 912) * .30) * waves))
        const size = Math.min(34, spacing * (.74 + scatter(id + 19) * .39)) * verticalScale
        const y = Math.max(size * 1.4 + 4, depth * height + (39 - depth * 48 + scatter(id + 140) * 7) * verticalScale)
        people.push({
          id, x, y, size,
          depth, side: x < width / 2 ? -1 : 1, duration: (loss ? 780 : 570) + scatter(id + 170) * 280,
          arrival: wave * (amount >= 500_000 ? 170 : 225) + scatter(id + 80) * 230,
          departure: 1940 + (id % 4) * 225 + scatter(id + 80) * 270,
        })
      }
    }

    if (!loss && amount >= 1_000_000) {
      const rotating = [...people].sort((a, b) => scatter(a.id + 812) - scatter(b.id + 812)).slice(0, 72)
      rotating.forEach((figure, index) => { figure.exchange = 2270 + Math.floor(index / 24) * 245 + scatter(index + 333) * 130 })
    }

    if (loss) {
      const holders = count <= 2 ? [0] : count === 38 ? [16, 22, 33] : [Math.floor(count / 5), Math.floor(count / 2), Math.floor(count * .8)]
      holders.forEach((id, index) => {
        people[id].sign = { index, text: count <= 2 ? 'Не моё' : boos[index], start: 420 + index * 230 }
      })
    } else {
      const slots = amount < 12 ? 2 : amount <= 38 ? 4 : 7
      const anchors = [.25, .72, .44, .82, .20, .57, .37]
      for (let index = 0; index < Math.min(slots, count); index++) {
        const cost = (figure: Figure) => Math.abs(figure.x / width - anchors[index]) * 2 + Math.abs(figure.depth - .64) * .7 + figure.arrival * .0006
        const holder = people.filter(figure => figure.exchange === undefined && !figure.sign).sort((a, b) => cost(a) - cost(b))[0]
        holder.sign = { index, text: cheers[(offset + index) % cheers.length], start: Math.max(1260 + index * 690, holder.arrival + 530) }
      }
    }

    people.sort((a, b) => a.y - b.y)
  }

  function drawFigure(figure: Figure, x: number, y: number, travel: number, opacity: number, alternate = false) {
    const size = figure.size * (.8 + .2 * travel)
    const spriteIndex = avatarStyle === 'lorelei' ? figure.id * 7 + Math.floor(figure.id / 5) : figure.id
    const index = (spriteIndex + (alternate ? 9 : 0)) % sprites.length
    ctx.globalAlpha = opacity * (.57 + .43 * figure.depth)
    ctx.drawImage(sprites[index], x - size / 2, y - size * 1.4, size, size * 1.4)
  }

  function drawPlacard({ figure, x: holderX, y: feetY, raise, opacity }: Placard) {
    const { size, sign } = figure
    if (!sign || holderX < -size || holderX > width + size) return
    ctx.save()
    ctx.globalAlpha = opacity
    ctx.font = 'bold 11px system-ui'
    const boardWidth = Math.ceil(ctx.measureText(sign.text).width) + 16
    const side = holderX < width / 2 ? 1 : -1
    const shoulderX = holderX + side * size * .26
    const shoulderY = feetY - size * .59
    const gripX = shoulderX + side * (8 + raise * 7)
    const gripY = shoulderY - 7 - raise * 5
    const y = Math.max(15, gripY - 22 - raise * 19)
    const x = Math.max(boardWidth / 2 + 7, Math.min(width - boardWidth / 2 - 7, gripX - (gripY - y) * side * (.06 + .06 * raise)))
    ctx.lineCap = 'round'
    ctx.lineJoin = 'round'
    ctx.strokeStyle = '#181522'
    ctx.lineWidth = 6
    ctx.beginPath()
    ctx.moveTo(shoulderX, shoulderY)
    ctx.lineTo(gripX - side * 4, shoulderY + 1)
    ctx.lineTo(gripX, gripY)
    ctx.stroke()
    ctx.strokeStyle = loss ? '#af789e' : '#a18cc7'
    ctx.lineWidth = 3.5
    ctx.stroke()
    ctx.strokeStyle = '#2b211e'
    ctx.lineWidth = 4.5
    ctx.beginPath()
    ctx.moveTo(x, y + 6)
    ctx.lineTo(gripX, gripY + 7)
    ctx.stroke()
    ctx.strokeStyle = '#dfba87'
    ctx.lineWidth = 2.5
    ctx.stroke()
    ctx.fillStyle = '#ead5c8'
    ctx.beginPath()
    ctx.arc(gripX, gripY, 3.2, 0, 2 * Math.PI)
    ctx.fill()
    ctx.translate(x, y)
    ctx.rotate(side * [-.08, .04, -.06][sign.index % 3])
    ctx.fillStyle = loss ? '#f1d6e5' : cardColors[sign.index % 3]
    ctx.strokeStyle = loss ? '#67384c' : '#514465'
    ctx.lineWidth = 1.5
    ctx.beginPath()
    ctx.roundRect(-boardWidth / 2, -11, boardWidth, 22, 2)
    ctx.fill()
    ctx.stroke()
    ctx.fillStyle = loss ? '#4b2941' : '#22202e'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.fillText(sign.text, 0, 0)
    ctx.restore()
  }

  function draw(time: number, still: boolean) {
    if (!width || !height) return
    ctx.globalAlpha = 1
    ctx.fillStyle = light
    ctx.fillRect(0, 0, width, height)
    for (const figure of people) {
      const { x, y, side, arrival, departure, duration, exchange, sign } = figure
      const edge = side < 0 ? -30 : width + 30
      if (loss) {
        const progress = clamp((time - departure) / duration)
        if (progress === 1) continue
        const leavingX = x + (edge - x) * ease(progress)
        const leavingY = y + Math.sin(time * .025 + figure.id * .91) * (progress > 0 ? 1.6 : .6)
        drawFigure(figure, leavingX, leavingY, 1, 1 - clamp((progress - .73) / .27))
        if (sign && progress < .83) {
          const raise = ease(clamp((time - sign.start) / 350))
          if (raise) drawPlacard({ figure, x: leavingX, y: leavingY, raise, opacity: raise * (1 - clamp((progress - .47) / .36)) })
        }
        continue
      }

      const local = time - arrival
      if (local <= 0) continue
      if (exchange !== undefined && time >= exchange) {
        const sinceExchange = time - exchange
        if (sinceExchange < 340) {
          const out = ease(sinceExchange / 340)
          drawFigure(figure, x + (edge - x) * out, y + 3 * out, 1, 1 - out)
        } else if (sinceExchange >= 390) {
          const progress = clamp((sinceExchange - 390) / 500)
          const inward = ease(progress)
          const opposite = side < 0 ? width + 28 : -28
          drawFigure(figure, opposite + (x - opposite) * inward, y - 6 * (1 - inward), inward, clamp(progress * 2), true)
        }
        continue
      }

      const progress = clamp(local / duration)
      const travel = ease(progress)
      const sway = still ? 0 : Math.sin(time * .0055 + figure.id * .68) * (figure.depth + .25) * (1 - clamp((time - end + 900) / 900))
      const arrivedX = x + side * (11 + (1 - figure.depth) * 34) * (1 - travel) + sway * 1.65
      const arrivedY = y - 31 * (1 - travel) + Math.sin(progress * Math.PI * 5) * (1 - progress) * 3 + Math.abs(sway)
      drawFigure(figure, arrivedX, arrivedY, travel, clamp(local / 180))
      if (sign) {
        const age = time - sign.start
        const raise = ease(clamp(age / 230))
        const opacity = still ? Number(sign.index === 0) : raise * (1 - clamp((age - 1070) / 420))
        if (opacity) drawPlacard({ figure, x: arrivedX, y: arrivedY, raise, opacity })
      }
    }
    ctx.globalAlpha = 1
  }

  return (still: boolean, pulse: (cue: CrowdCue) => void) => {
    const cues: [number, CrowdCue][] = loss
      ? [[550, 'crowd-open'], [2000, 'crowd-wave'], [3050, 'crowd-settle']]
      : [[245, 'crowd-open'], [1320, 'crowd-wave'], [amount < 12 ? 2060 : amount <= 38 ? 3420 : 5450, 'crowd-settle']]
    if (delta === 1) cues.pop()
    const start = performance.now()
    let frame = 0
    let time = still ? end : 0
    let nextCue = 0
    const redraw = () => { resize(); draw(time, still) }
    const observer = new ResizeObserver(redraw)
    observer.observe(canvas)
    redraw()
    const tick = (now: number) => {
      const elapsed = Math.min(end, now - start)
      const frameInterval = elapsed < activeUntil ? 30 : 62
      if (elapsed - time >= frameInterval || elapsed === end) {
        time = elapsed
        draw(time, false)
        // Emit just the current accent if a slow frame crossed several cues.
        let cue: CrowdCue | undefined
        while (nextCue < cues.length && time >= cues[nextCue][0]) {
          cue = cues[nextCue][1]
          nextCue++
        }
        if (cue) pulse(cue)
      }
      if (time < end) frame = requestAnimationFrame(tick)
    }
    if (!still) frame = requestAnimationFrame(tick)
    return (settle = false) => {
      cancelAnimationFrame(frame)
      if (settle) {
        still = true
        time = end
        draw(time, still)
      } else {
        observer.disconnect()
      }
    }
  }
}

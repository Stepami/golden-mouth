export function flyCrystal(start: DOMRect, end: DOMRect, index: number) {
  const gem = document.createElement('span')
  gem.className = 'game-onboarding-crystal-flight'
  gem.dataset.onboardingFlight = String(index)
  gem.setAttribute('aria-hidden', 'true')
  const offset = index === 0 ? -13 : 13
  const fromX = start.left + start.width / 2 + offset
  const fromY = start.top + start.height / 2
  gem.style.left = `${fromX - 16}px`
  gem.style.top = `${fromY - 16}px`
  document.body.append(gem)
  const x = end.left + end.width / 2 - fromX
  const y = end.top + end.height / 2 - fromY
  const animation = gem.animate([
    { transform: 'translate(0, 5px) scale(.7)', opacity: 0 },
    { transform: `translate(${offset}px, -30px) rotate(${offset}deg) scale(1.15)`, opacity: 1, offset: .24 },
    { transform: `translate(${x * .5 + offset}px, ${y * .54 - 12}px) rotate(14deg) scale(.85)`, opacity: 1, offset: .65 },
    { transform: `translate(${x}px, ${y}px) rotate(35deg) scale(.24)`, opacity: .3 },
  ], { duration: 610, delay: 85 + index * 130, easing: 'cubic-bezier(.24,.58,.23,1)', fill: 'both' })
  return { gem, animation }
}

export function pulseCrystalCounter(counter: HTMLElement) {
  return counter.animate([
    { transform: 'scale(1)' },
    { transform: 'scale(1.12)', boxShadow: '0 0 0 4px #a6e5ff35', offset: .32 },
    { transform: 'scale(1)', boxShadow: '0 0 0 9px #a6e5ff00' },
  ], { duration: 260, easing: 'ease-out' })
}

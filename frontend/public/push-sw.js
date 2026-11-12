// Push only: no fetch handler, application cache or automatic page reload.
self.addEventListener('push', event => {
  event.waitUntil((async () => {
    let payload
    try { payload = event.data?.json() } catch { /* Use a neutral message for malformed payloads. */ }
    const body = typeof payload?.body === 'string' ? payload.body.slice(0, 2000) : 'Загляни в свой мир.'
    let url = new URL('/', self.location.origin)
    try {
      const requested = new URL(payload?.url ?? '/', self.location.origin)
      if (requested.origin === self.location.origin && !requested.username && !requested.password) url = requested
    } catch { /* A malformed or external URL opens the game root. */ }
    await self.registration.showNotification('Реки', {
      body, icon: '/android-chrome-192x192.png', data: { url: url.href },
    })
  })())
})

self.addEventListener('notificationclick', event => {
  event.notification.close()
  event.waitUntil((async () => {
    let url = new URL('/', self.location.origin)
    try {
      const requested = new URL(event.notification.data?.url ?? '/', self.location.origin)
      if (requested.origin === self.location.origin && !requested.username && !requested.password) url = requested
    } catch { /* Validate again; notifications can outlive a worker version. */ }
    const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true })
    for (const client of windows) {
      if (new URL(client.url).origin !== self.location.origin) continue
      try { await client.focus(); return } catch { /* Try another window or open a new one. */ }
    }
    await self.clients.openWindow(url.href)
  })())
})

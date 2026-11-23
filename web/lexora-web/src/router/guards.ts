import type { Router } from 'vue-router'

import { useAuthStore } from '@/stores/auth'
import { safeRedirect } from './redirects'

export function installGuards(router: Router): void {
  const auth = useAuthStore()

  router.beforeEach(async (to) => {
    await auth.restore()

    if (to.meta.requiresAuth && !auth.isAuthenticated) {
      return { name: 'login', query: { redirect: to.fullPath } }
    }

    if (to.meta.guestOnly && auth.isAuthenticated) {
      return safeRedirect(to.query.redirect) ?? { name: 'knowledge-bases' }
    }

    return true
  })

  auth.onExpired(() => {
    const current = router.currentRoute.value

    if (current.meta.requiresAuth) {
      void router.push({ name: 'login', query: { redirect: current.fullPath, reason: 'expired' } })
    }
  })
}

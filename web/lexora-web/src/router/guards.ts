import type { RouteLocationNormalized, RouteLocationRaw, Router } from 'vue-router'

import { useAuthStore } from '@/stores/auth'
import { safeRedirect } from './redirects'

// A page the role cannot open looks like a missing one, so the route list does not tell what exists.
function notFound(to: RouteLocationNormalized): RouteLocationRaw {
  return {
    name: 'not-found',
    params: { pathMatch: to.path.split('/').filter(Boolean) },
    query: to.query,
    hash: to.hash,
  }
}

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

    if (to.meta.roles && !auth.hasRole(...to.meta.roles)) {
      return notFound(to)
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

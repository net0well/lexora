import { watch } from 'vue'

import { useAuthStore } from './auth'

// Stores holding data of the signed-in person drop it as soon as the user changes, before anything can
// render it: signing out, an ended session or another account never sees what the previous one loaded.
export function resetWhenUserChanges(reset: () => void): void {
  const auth = useAuthStore()

  watch(
    () => auth.user?.id ?? null,
    (current, previous) => {
      if (current !== previous) {
        reset()
      }
    },
    { flush: 'sync' },
  )
}

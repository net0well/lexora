import { useRoute, useRouter, type LocationQueryRaw } from 'vue-router'

export type QueryChange = string | number | null | undefined

export function useQueryState() {
  const route = useRoute()
  const router = useRouter()

  function text(name: string): string {
    const value = route.query[name]

    return typeof value === 'string' ? value : ''
  }

  function positiveInteger(name: string, fallback = 1): number {
    const value = Number.parseInt(text(name), 10)

    return Number.isInteger(value) && value > 0 ? value : fallback
  }

  // One replace per user action: two replaces in the same tick would each start from the old query.
  function update(changes: Record<string, QueryChange>): void {
    const query: LocationQueryRaw = { ...route.query }

    for (const [name, value] of Object.entries(changes)) {
      if (value === null || value === undefined || value === '') {
        delete query[name]
      } else {
        query[name] = String(value)
      }
    }

    void router.replace({ query })
  }

  return { text, positiveInteger, update }
}

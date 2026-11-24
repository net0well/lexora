import { readonly, shallowRef, toValue, watch, type MaybeRefOrGetter } from 'vue'

import { isAbort } from '@/api/http'
import type { ApiNotification, ApiResult } from '@/api/types/commandResult'
import type { SearchHit, SearchRequest, SearchResult } from '@/api/types/search'

export interface UseKnowledgeBaseSearchOptions {
  knowledgeBaseId: MaybeRefOrGetter<string>
  request: MaybeRefOrGetter<SearchRequest | null>
  search: (
    knowledgeBaseId: string,
    request: SearchRequest,
    signal: AbortSignal,
  ) => Promise<ApiResult<SearchResult>>
  onFailure?: (notifications: ApiNotification[]) => void
}

export function useKnowledgeBaseSearch({
  knowledgeBaseId,
  request,
  search,
  onFailure,
}: UseKnowledgeBaseSearchOptions) {
  const hits = shallowRef<readonly SearchHit[]>([])
  const searched = shallowRef(false)
  const pending = shallowRef(false)
  const runs = shallowRef(0)

  // A primitive key: a recreated request object with the same values must not search again.
  const key = () => {
    const current = toValue(request)

    return current
      ? JSON.stringify([toValue(knowledgeBaseId), current.mode, current.query, runs.value])
      : null
  }

  watch(
    key,
    async (_key, _previous, onCleanup) => {
      const id = toValue(knowledgeBaseId)
      const current = toValue(request)

      if (!current) {
        hits.value = []
        searched.value = false
        pending.value = false
        return
      }

      const controller = new AbortController()
      let active = true
      onCleanup(() => {
        active = false
        controller.abort()
      })
      pending.value = true

      try {
        const result = await search(id, current, controller.signal)

        if (!active) {
          return
        }

        hits.value = result.success && result.data ? result.data.hits : []
        searched.value = true

        if (!result.success) {
          onFailure?.(result.notifications)
        }
      } catch (error) {
        if (!isAbort(error)) {
          throw error
        }
      } finally {
        if (active) {
          pending.value = false
        }
      }
    },
    { immediate: true },
  )

  function refresh(): void {
    runs.value++
  }

  return { hits: readonly(hits), searched: readonly(searched), pending: readonly(pending), refresh }
}

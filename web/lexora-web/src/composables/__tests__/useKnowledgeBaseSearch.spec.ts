import { flushPromises } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { shallowRef } from 'vue'

import type { ApiResult } from '@/api/types/commandResult'
import type { SearchHit, SearchRequest, SearchResult } from '@/api/types/search'
import { withSetup } from '@/__tests__/support'
import { ok } from '@/__tests__/tokens'
import { useKnowledgeBaseSearch } from '../useKnowledgeBaseSearch'

function hit(content: string): SearchHit {
  return {
    chunkId: content,
    documentId: 'doc-1',
    documentName: 'manual.pdf',
    content,
    pageNumber: 1,
    sheetName: null,
    sectionTitle: null,
    score: 0.03,
    matchedBy: 'vector',
  }
}

describe('useKnowledgeBaseSearch', () => {
  it('keeps only the answer of the latest search', async () => {
    const answers = new Map<string, (result: ApiResult<SearchResult>) => void>()
    const search = vi.fn(
      (_id: string, request: SearchRequest, signal: AbortSignal) =>
        new Promise<ApiResult<SearchResult>>((resolve, reject) => {
          answers.set(request.query, resolve)
          signal.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')))
        }),
    )
    const request = shallowRef<SearchRequest | null>({ query: 'first', mode: 'hybrid' })
    const { result: state } = withSetup(() =>
      useKnowledgeBaseSearch({ knowledgeBaseId: 'kb-1', request, search }),
    )

    request.value = { query: 'second', mode: 'hybrid' }
    await flushPromises()
    answers.get('second')!(ok({ hits: [hit('second')] }))
    answers.get('first')!(ok({ hits: [hit('first')] }))
    await flushPromises()

    expect(state.hits.value.map((item) => item.content)).toEqual(['second'])
    expect(state.pending.value).toBe(false)
  })

  it('stops waiting when the search is cleared while it runs', async () => {
    const search = vi.fn(
      (_id: string, _request: SearchRequest, signal: AbortSignal) =>
        new Promise<ApiResult<SearchResult>>((_resolve, reject) => {
          signal.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')))
        }),
    )
    const request = shallowRef<SearchRequest | null>({ query: 'epis', mode: 'hybrid' })
    const { result: state } = withSetup(() =>
      useKnowledgeBaseSearch({ knowledgeBaseId: 'kb-1', request, search }),
    )
    expect(state.pending.value).toBe(true)

    request.value = null
    await flushPromises()

    expect(state.pending.value).toBe(false)
    expect(state.searched.value).toBe(false)
  })

  it('does not search again for an equal request object', async () => {
    const search = vi.fn(async () => ok({ hits: [] }))
    const request = shallowRef<SearchRequest | null>({ query: 'epis', mode: 'hybrid' })
    withSetup(() => useKnowledgeBaseSearch({ knowledgeBaseId: 'kb-1', request, search }))
    await flushPromises()

    request.value = { query: 'epis', mode: 'hybrid' }
    await flushPromises()

    expect(search).toHaveBeenCalledTimes(1)
  })

  it('reports a refused search', async () => {
    const onFailure = vi.fn()
    const refused = {
      key: 'search.query.tooLong',
      message: 'Too long.',
      type: 'validation' as const,
    }
    withSetup(() =>
      useKnowledgeBaseSearch({
        knowledgeBaseId: 'kb-1',
        request: { query: 'x', mode: 'hybrid' },
        search: async () => ({ success: false, data: null, notifications: [refused], status: 400 }),
        onFailure,
      }),
    )
    await flushPromises()

    expect(onFailure).toHaveBeenCalledWith([refused])
  })
})

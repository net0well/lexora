import { defineStore } from 'pinia'
import { computed, ref, shallowRef } from 'vue'

import { knowledgeBasesApi, type KnowledgeBaseQuery } from '@/api/knowledgeBases'
import type {
  KnowledgeBase,
  KnowledgeBaseFields,
  KnowledgeBaseListItem,
} from '@/api/types/knowledgeBase'

export const useKnowledgeBasesStore = defineStore('knowledgeBases', () => {
  const items = shallowRef<KnowledgeBaseListItem[]>([])
  const totalItems = shallowRef(0)
  const pendingLists = shallowRef(0)
  const loadingList = computed(() => pendingLists.value > 0)
  const current = ref<KnowledgeBase | null>(null)
  const pendingOpens = shallowRef(0)
  const loadingCurrent = computed(() => pendingOpens.value > 0)

  async function load(query: KnowledgeBaseQuery, signal?: AbortSignal) {
    pendingLists.value++

    try {
      const result = await knowledgeBasesApi.list(query, signal)

      if (result.success && result.data) {
        items.value = result.data.items
        totalItems.value = result.data.totalItems
      }

      return result
    } finally {
      pendingLists.value--
    }
  }

  async function open(id: string, signal?: AbortSignal) {
    if (current.value?.id !== id) {
      current.value = null
    }

    pendingOpens.value++

    try {
      const result = await knowledgeBasesApi.get(id, signal)
      current.value = result.success ? result.data : null

      return result
    } finally {
      pendingOpens.value--
    }
  }

  function create(fields: KnowledgeBaseFields) {
    return knowledgeBasesApi.create(fields)
  }

  async function update(id: string, fields: KnowledgeBaseFields) {
    const result = await knowledgeBasesApi.update(id, fields)

    if (result.success && result.data && current.value?.id === id) {
      current.value = { ...current.value, ...result.data }
    }

    return result
  }

  async function archive(id: string) {
    const result = await knowledgeBasesApi.archive(id)

    if (result.success && current.value?.id === id) {
      current.value = { ...current.value, status: 'archived' }
    }

    return result
  }

  function reindex(id: string) {
    return knowledgeBasesApi.reindex(id)
  }

  function countDocuments(id: string, delta: number): void {
    if (current.value?.id === id) {
      current.value.documentCount = Math.max(current.value.documentCount + delta, 0)
    }
  }

  return {
    items,
    totalItems,
    pendingLists,
    loadingList,
    current,
    pendingOpens,
    loadingCurrent,
    load,
    open,
    create,
    update,
    archive,
    reindex,
    countDocuments,
  }
})

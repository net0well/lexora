import { defineStore } from 'pinia'
import { ref } from 'vue'

import { knowledgeBasesApi, type KnowledgeBaseQuery } from '@/api/knowledgeBases'
import type {
  KnowledgeBase,
  KnowledgeBaseFields,
  KnowledgeBaseListItem,
} from '@/api/types/knowledgeBase'

export const useKnowledgeBasesStore = defineStore('knowledgeBases', () => {
  const items = ref<KnowledgeBaseListItem[]>([])
  const totalItems = ref(0)
  const loadingList = ref(false)
  const current = ref<KnowledgeBase | null>(null)
  const loadingCurrent = ref(false)

  async function load(query: KnowledgeBaseQuery) {
    loadingList.value = true

    try {
      const result = await knowledgeBasesApi.list(query)

      if (result.success && result.data) {
        items.value = result.data.items
        totalItems.value = result.data.totalItems
      }

      return result
    } finally {
      loadingList.value = false
    }
  }

  async function open(id: string) {
    if (current.value?.id !== id) {
      current.value = null
    }

    loadingCurrent.value = true

    try {
      const result = await knowledgeBasesApi.get(id)
      current.value = result.success ? result.data : null

      return result
    } finally {
      loadingCurrent.value = false
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
    loadingList,
    current,
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

import { defineStore } from 'pinia'
import { computed, ref, shallowRef } from 'vue'

import { documentsApi, type DocumentQuery } from '@/api/documents'
import type { DocumentListItem, UploadedDocument } from '@/api/types/document'

export const useDocumentsStore = defineStore('documents', () => {
  const knowledgeBaseId = shallowRef<string | null>(null)
  const items = ref<DocumentListItem[]>([])
  const totalItems = shallowRef(0)
  const pendingLoads = shallowRef(0)
  const loading = computed(() => pendingLoads.value > 0)

  async function load(id: string, query: DocumentQuery, signal?: AbortSignal) {
    if (knowledgeBaseId.value !== id) {
      knowledgeBaseId.value = id
      items.value = []
      totalItems.value = 0
    }

    pendingLoads.value++

    try {
      const result = await documentsApi.list(id, query, signal)

      if (result.success && result.data && knowledgeBaseId.value === id) {
        items.value = result.data.items
        totalItems.value = result.data.totalItems
      }

      return result
    } finally {
      pendingLoads.value--
    }
  }

  function update(document: Partial<DocumentListItem> & { id: string }): void {
    const index = items.value.findIndex((item) => item.id === document.id)

    if (index >= 0) {
      items.value[index] = { ...items.value[index]!, ...document }
    }
  }

  function added(document: UploadedDocument, baseId: string): void {
    if (knowledgeBaseId.value !== baseId || items.value.some((item) => item.id === document.id)) {
      return
    }

    const now = new Date().toISOString()
    items.value.unshift({
      ...document,
      chunkCount: 0,
      embeddedChunkCount: 0,
      uploadedAt: now,
      updatedAt: now,
    })
    totalItems.value++
  }

  async function remove(id: string) {
    const result = await documentsApi.delete(id)

    if (result.success) {
      items.value = items.value.filter((item) => item.id !== id)
      totalItems.value = Math.max(totalItems.value - 1, 0)
    }

    return result
  }

  function download(id: string) {
    return documentsApi.download(id)
  }

  return {
    knowledgeBaseId,
    items,
    totalItems,
    pendingLoads,
    loading,
    load,
    update,
    added,
    remove,
    download,
  }
})

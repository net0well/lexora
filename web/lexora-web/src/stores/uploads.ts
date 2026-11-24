import { defineStore } from 'pinia'
import { ref } from 'vue'

import { documentsApi } from '@/api/documents'
import { isAbort } from '@/api/http'
import type { ApiNotification } from '@/api/types/commandResult'
import type { UploadedDocument } from '@/api/types/document'
import { rejectionOf, type UploadRejection } from '@/utils/uploadRules'
import { resetWhenUserChanges } from './sessionScope'

export type UploadState = 'queued' | 'uploading' | 'done' | 'failed' | 'rejected'

export interface UploadItem {
  key: number
  name: string
  size: number
  progress: number
  state: UploadState
  rejection: UploadRejection | null
  notifications: ApiNotification[]
}

type Uploaded = (document: UploadedDocument, knowledgeBaseId: string) => void

interface Waiting {
  key: number
  file: File
  knowledgeBaseId: string
  uploaded: Uploaded
}

const concurrency = 2

// The queue lives as long as the app, not the page: leaving a base does not orphan its uploads, and
// coming back shows how they went.
export const useUploadsStore = defineStore('uploads', () => {
  const queues = ref<Record<string, UploadItem[]>>({})
  const waiting: Waiting[] = []
  const controllers = new Map<number, AbortController>()
  let running = 0
  let sequence = 0

  function itemsFor(knowledgeBaseId: string): readonly UploadItem[] {
    return queues.value[knowledgeBaseId] ?? []
  }

  function find(knowledgeBaseId: string, key: number): UploadItem | undefined {
    return queues.value[knowledgeBaseId]?.find((item) => item.key === key)
  }

  function drop(knowledgeBaseId: string, key: number): void {
    queues.value[knowledgeBaseId] = itemsFor(knowledgeBaseId).filter((item) => item.key !== key)
  }

  async function send({ key, file, knowledgeBaseId, uploaded }: Waiting): Promise<void> {
    const item = find(knowledgeBaseId, key)

    if (!item) {
      return
    }

    const controller = new AbortController()
    controllers.set(key, controller)
    item.state = 'uploading'

    try {
      const result = await documentsApi.upload(knowledgeBaseId, file, {
        signal: controller.signal,
        onProgress: (fraction) => {
          item.progress = Math.round(fraction * 100)
        },
      })

      if (result.success && result.data) {
        item.state = 'done'
        item.progress = 100
        uploaded(result.data, knowledgeBaseId)
      } else {
        item.state = 'failed'
        item.notifications = result.notifications
      }
    } catch (error) {
      if (!isAbort(error)) {
        throw error
      }

      drop(knowledgeBaseId, key)
    } finally {
      controllers.delete(key)
    }
  }

  function pump(): void {
    while (running < concurrency && waiting.length > 0) {
      const next = waiting.shift()!
      running++
      void send(next).finally(() => {
        running--
        pump()
      })
    }
  }

  function add(knowledgeBaseId: string, files: Iterable<File>, uploaded: Uploaded): void {
    const queue = [...itemsFor(knowledgeBaseId)]

    for (const file of files) {
      const key = ++sequence
      const rejection = rejectionOf(file)

      queue.push({
        key,
        name: file.name,
        size: file.size,
        progress: 0,
        state: rejection ? 'rejected' : 'queued',
        rejection,
        notifications: [],
      })

      if (!rejection) {
        waiting.push({ key, file, knowledgeBaseId, uploaded })
      }
    }

    queues.value[knowledgeBaseId] = queue
    pump()
  }

  function cancel(knowledgeBaseId: string, key: number): void {
    const controller = controllers.get(key)

    if (controller) {
      controller.abort()
      return
    }

    const index = waiting.findIndex((entry) => entry.key === key)

    if (index >= 0) {
      waiting.splice(index, 1)
    }

    drop(knowledgeBaseId, key)
  }

  function clearFinished(knowledgeBaseId: string): void {
    queues.value[knowledgeBaseId] = itemsFor(knowledgeBaseId).filter(
      (item) => item.state === 'queued' || item.state === 'uploading',
    )
  }

  resetWhenUserChanges(() => {
    waiting.length = 0

    for (const controller of controllers.values()) {
      controller.abort()
    }

    queues.value = {}
  })

  return { queues, itemsFor, add, cancel, clearFinished }
})

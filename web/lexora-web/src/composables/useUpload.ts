import { readonly, ref, toValue, type MaybeRefOrGetter } from 'vue'
import { useI18n } from 'vue-i18n'

import { documentsApi } from '@/api/documents'
import { isAbort } from '@/api/http'
import { documentRules, type UploadedDocument } from '@/api/types/document'
import { useFormat } from './useFormat'
import { useNotifications } from './useNotifications'

export type UploadState = 'queued' | 'uploading' | 'done' | 'failed' | 'rejected'

export interface UploadItem {
  key: number
  name: string
  size: number
  progress: number
  state: UploadState
  error: string | null
}

export type UploadRejection = 'format' | 'size' | 'empty'

export interface UseUploadOptions {
  knowledgeBaseId: MaybeRefOrGetter<string>
  onUploaded: (document: UploadedDocument, knowledgeBaseId: string) => void
  concurrency?: number
}

export function rejectionOf(file: File): UploadRejection | null {
  const name = file.name.toLowerCase()

  if (!documentRules.extensions.some((extension) => name.endsWith(extension))) {
    return 'format'
  }

  if (file.size === 0) {
    return 'empty'
  }

  return file.size > documentRules.maxFileSizeBytes ? 'size' : null
}

export function useUpload({ knowledgeBaseId, onUploaded, concurrency = 2 }: UseUploadOptions) {
  const { t } = useI18n()
  const format = useFormat()
  const { describe } = useNotifications()

  const items = ref<UploadItem[]>([])
  const waiting: { key: number; file: File; knowledgeBaseId: string }[] = []
  const controllers = new Map<number, AbortController>()
  let running = 0
  let sequence = 0

  function find(key: number): UploadItem | undefined {
    return items.value.find((item) => item.key === key)
  }

  function rejectionText(reason: UploadRejection): string {
    return reason === 'size'
      ? t('documents.upload.tooLarge', { max: format.bytes(documentRules.maxFileSizeBytes) })
      : t(`documents.upload.${reason}`)
  }

  async function send(key: number, file: File, targetId: string): Promise<void> {
    const item = find(key)

    if (!item) {
      return
    }

    const controller = new AbortController()
    controllers.set(key, controller)
    item.state = 'uploading'

    try {
      const result = await documentsApi.upload(targetId, file, {
        signal: controller.signal,
        onProgress: (fraction) => {
          item.progress = Math.round(fraction * 100)
        },
      })

      if (result.success && result.data) {
        item.state = 'done'
        item.progress = 100
        onUploaded(result.data, targetId)
      } else {
        item.state = 'failed'
        item.error = result.notifications.map(describe).join(' ') || t('notifications.unexpected')
      }
    } catch (error) {
      if (!isAbort(error)) {
        throw error
      }

      items.value = items.value.filter((entry) => entry.key !== key)
    } finally {
      controllers.delete(key)
    }
  }

  function pump(): void {
    while (running < concurrency && waiting.length > 0) {
      const next = waiting.shift()!
      running++
      void send(next.key, next.file, next.knowledgeBaseId).finally(() => {
        running--
        pump()
      })
    }
  }

  function add(files: Iterable<File>): void {
    for (const file of files) {
      const key = ++sequence
      const rejection = rejectionOf(file)

      items.value.push({
        key,
        name: file.name,
        size: file.size,
        progress: 0,
        state: rejection ? 'rejected' : 'queued',
        error: rejection ? rejectionText(rejection) : null,
      })

      if (!rejection) {
        waiting.push({ key, file, knowledgeBaseId: toValue(knowledgeBaseId) })
      }
    }

    pump()
  }

  function cancel(key: number): void {
    const controller = controllers.get(key)

    if (controller) {
      controller.abort()
      return
    }

    const index = waiting.findIndex((entry) => entry.key === key)

    if (index >= 0) {
      waiting.splice(index, 1)
    }

    items.value = items.value.filter((item) => item.key !== key)
  }

  function clearFinished(): void {
    items.value = items.value.filter(
      (item) => item.state === 'queued' || item.state === 'uploading',
    )
  }

  return { items: readonly(items), add, cancel, clearFinished }
}

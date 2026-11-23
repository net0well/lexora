import { useDocumentsStore } from '@/stores/documents'
import { useNotifications } from './useNotifications'

export function useDocumentDownload() {
  const store = useDocumentsStore()
  const { notify } = useNotifications()

  // A plain link click, not window.open: the temporary URL answers as an attachment, so the browser
  // downloads it without leaving the page and without a pop-up blocker in the way.
  async function download(documentId: string): Promise<void> {
    const result = await store.download(documentId)

    if (!result.success || !result.data) {
      notify(result.notifications)
      return
    }

    const link = document.createElement('a')
    link.href = result.data.url
    link.rel = 'noopener'
    link.click()
  }

  return { download }
}

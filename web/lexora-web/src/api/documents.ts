import { apiUrl, http, parseResult, withSessionRetry } from './http'
import type { ApiResult, PagedResponse } from './types/commandResult'
import type {
  DocumentDetails,
  DocumentDownload,
  DocumentListItem,
  DocumentStatus,
  ReprocessedDocument,
  UploadedDocument,
} from './types/document'

export interface DocumentQuery {
  status?: DocumentStatus | null
  search?: string | null
  page?: number
  pageSize?: number
}

export interface UploadOptions {
  onProgress?: (fraction: number) => void
  signal?: AbortSignal
}

function sendFile(
  url: string,
  file: File,
  token: string | null,
  options: UploadOptions,
): Promise<ApiResult<UploadedDocument>> {
  return new Promise((resolve, reject) => {
    // A cancel that came while the session was being renewed must stop the second attempt too.
    if (options.signal?.aborted) {
      reject(new DOMException('The upload was cancelled.', 'AbortError'))
      return
    }

    const xhr = new XMLHttpRequest()
    const abort = () => xhr.abort()
    xhr.open('POST', url)
    xhr.setRequestHeader('Accept', 'application/json')

    if (token) {
      xhr.setRequestHeader('Authorization', `Bearer ${token}`)
    }

    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable && event.total > 0) {
        options.onProgress?.(event.loaded / event.total)
      }
    }
    xhr.onload = () =>
      resolve(parseResult(xhr.status, xhr.getResponseHeader('content-type'), xhr.responseText))
    xhr.onerror = () => resolve(parseResult(0, null, ''))
    xhr.onabort = () => reject(new DOMException('The upload was cancelled.', 'AbortError'))
    xhr.onloadend = () => options.signal?.removeEventListener('abort', abort)
    options.signal?.addEventListener('abort', abort, { once: true })

    const form = new FormData()
    form.append('file', file, file.name)
    options.onProgress?.(0)
    xhr.send(form)
  })
}

export const documentsApi = {
  list: (knowledgeBaseId: string, query: DocumentQuery = {}, signal?: AbortSignal) =>
    http.get<PagedResponse<DocumentListItem>>(`/knowledge-bases/${knowledgeBaseId}/documents`, {
      query: { ...query },
      signal,
    }),
  get: (id: string, signal?: AbortSignal) =>
    http.get<DocumentDetails>(`/documents/${id}`, { signal }),
  download: (id: string) => http.get<DocumentDownload>(`/documents/${id}/download`),
  reprocess: (id: string) => http.post<ReprocessedDocument>(`/documents/${id}/reprocess`),
  delete: (id: string) => http.delete<null>(`/documents/${id}`),
  upload: (knowledgeBaseId: string, file: File, options: UploadOptions = {}) =>
    withSessionRetry((token) =>
      sendFile(apiUrl(`/knowledge-bases/${knowledgeBaseId}/documents`), file, token, options),
    ),
}

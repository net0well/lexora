export type DocumentStatus = 'uploaded' | 'extracted' | 'chunked' | 'indexed' | 'failed' | 'deleted'

export type DocumentFormat = 'pdf' | 'docx' | 'xlsx' | 'txt' | 'markdown' | 'csv'

export interface DocumentListItem {
  id: string
  fileName: string
  format: DocumentFormat
  sizeBytes: number
  status: DocumentStatus
  chunkCount: number
  embeddedChunkCount: number
  uploadedAt: string
  updatedAt: string
}

export interface DocumentDetails extends DocumentListItem {
  knowledgeBaseId: string
  contentType: string
  processingVersion: number
  pageCount: number | null
  failureReason: string | null
  uploadedBy: string
  indexedAt: string | null
}

export interface UploadedDocument {
  id: string
  fileName: string
  format: DocumentFormat
  status: DocumentStatus
  sizeBytes: number
}

export interface ReprocessedDocument {
  id: string
  status: DocumentStatus
  processingVersion: number
}

export interface DocumentDownload {
  url: string
  expiresAt: string
}

export const documentRules = {
  maxFileSizeBytes: 52_428_800,
  extensions: ['.pdf', '.docx', '.xlsx', '.txt', '.md', '.markdown', '.csv'],
} as const

export const terminalStatuses: readonly DocumentStatus[] = ['indexed', 'failed', 'deleted']

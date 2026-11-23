export type SearchMode = 'hybrid' | 'vector' | 'keyword'

export type SearchMatch = 'vector' | 'keyword' | 'both'

export interface SearchHit {
  chunkId: string
  documentId: string
  documentName: string
  content: string
  pageNumber: number | null
  sheetName: string | null
  sectionTitle: string | null
  score: number
  matchedBy: SearchMatch
}

export interface SearchRequest {
  query: string
  mode: SearchMode
}

export interface SearchResult {
  hits: SearchHit[]
}

export const searchRules = {
  queryMaxLength: 1000,
  modes: ['hybrid', 'vector', 'keyword'] as const satisfies readonly SearchMode[],
} as const

export type KnowledgeBaseStatus = 'active' | 'archived'

export interface KnowledgeBaseListItem {
  id: string
  name: string
  description: string | null
  status: KnowledgeBaseStatus
  documentCount: number
  updatedAt: string
}

export interface KnowledgeBase {
  id: string
  name: string
  description: string | null
  status: KnowledgeBaseStatus
  chunkMaxTokens: number
  chunkOverlapTokens: number
  documentCount: number
  createdAt: string
  updatedAt: string
}

export interface KnowledgeBaseFields {
  name: string
  description: string | null
  chunkMaxTokens: number
  chunkOverlapTokens: number
}

export interface SavedKnowledgeBase {
  id: string
  name: string
  description: string | null
  chunkMaxTokens: number
  chunkOverlapTokens: number
}

export interface ReindexResult {
  knowledgeBaseId: string
  documentCount: number
}

export const knowledgeBaseRules = {
  nameMinLength: 3,
  nameMaxLength: 120,
  descriptionMaxLength: 1000,
  chunkMaxTokensMin: 200,
  chunkMaxTokensMax: 2000,
  defaultChunkMaxTokens: 800,
  defaultChunkOverlapTokens: 120,
} as const

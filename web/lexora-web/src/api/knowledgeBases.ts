import { http } from './http'
import type { PagedResponse } from './types/commandResult'
import type {
  KnowledgeBase,
  KnowledgeBaseFields,
  KnowledgeBaseListItem,
  KnowledgeBaseStatus,
  ReindexResult,
  SavedKnowledgeBase,
} from './types/knowledgeBase'

export interface KnowledgeBaseQuery {
  status?: KnowledgeBaseStatus | null
  page?: number
  pageSize?: number
}

export const knowledgeBasesApi = {
  list: (query: KnowledgeBaseQuery = {}) =>
    http.get<PagedResponse<KnowledgeBaseListItem>>('/knowledge-bases', { query: { ...query } }),
  get: (id: string) => http.get<KnowledgeBase>(`/knowledge-bases/${id}`),
  create: (fields: KnowledgeBaseFields) =>
    http.post<SavedKnowledgeBase>('/knowledge-bases', fields),
  update: (id: string, fields: KnowledgeBaseFields) =>
    http.put<SavedKnowledgeBase>(`/knowledge-bases/${id}`, fields),
  archive: (id: string) => http.post<null>(`/knowledge-bases/${id}/archive`),
  reindex: (id: string) => http.post<ReindexResult>(`/knowledge-bases/${id}/reindex`),
}

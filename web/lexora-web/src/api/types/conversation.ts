import type { ApiNotification } from './commandResult'

export type MessageRole = 'user' | 'assistant'

export interface Citation {
  index: number
  documentId: string
  documentName: string
  pageNumber: number | null
  sheetName: string | null
  excerpt: string
  score: number
}

export interface ConversationMessage {
  id: string
  role: MessageRole
  content: string
  citations: Citation[]
  provider: string | null
  model: string | null
  inputTokens: number
  outputTokens: number
  latencyMs: number | null
  createdAt: string
}

export interface Conversation {
  id: string
  knowledgeBaseId: string
  title: string
  messages: ConversationMessage[]
  createdAt: string
  updatedAt: string
}

export interface ConversationListItem {
  id: string
  knowledgeBaseId: string
  title: string
  messageCount: number
  createdAt: string
  updatedAt: string
}

export interface StartedConversation {
  id: string
  knowledgeBaseId: string
  title: string
  createdAt: string
}

export interface QuestionRequest {
  question: string
  profile: string | null
}

export interface AnswerCitations {
  citations: Citation[]
  grounded: boolean
}

export interface AnswerDone {
  conversationId: string
  messageId: string
  provider: string
  model: string
  inputTokens: number
  outputTokens: number
  latencyMs: number
}

export interface AnswerStreamHandlers {
  delta: (text: string) => void
  citations: (citations: AnswerCitations) => void
  done: (done: AnswerDone) => void
}

export interface AnswerStreamError {
  notifications: ApiNotification[]
}

export const conversationRules = {
  questionMaxLength: 4000,
  noContextProvider: 'lexora',
} as const

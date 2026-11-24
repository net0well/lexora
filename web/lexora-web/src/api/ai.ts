import { http } from './http'

export interface ChatProfile {
  name: string
  provider: string
  model: string
  selectable: boolean
  available: boolean
}

export interface AiProfiles {
  defaultChatProfile: string
  chatProfiles: ChatProfile[]
  embedding: { provider: string; model: string; dimensions: number }
}

export const aiApi = {
  profiles: () => http.get<AiProfiles>('/ai/profiles'),
}

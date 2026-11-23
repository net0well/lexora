import { http } from './http'
import type { UserRole } from './types/auth'
import type { PagedResponse } from './types/commandResult'
import type { NewUser, User, UserStatus } from './types/user'

export interface UserQuery {
  search?: string | null
  role?: UserRole | null
  status?: UserStatus | null
  page?: number
  pageSize?: number
}

export const usersApi = {
  list: (query: UserQuery = {}, signal?: AbortSignal) =>
    http.get<PagedResponse<User>>('/users', { query: { ...query }, signal }),
  create: (user: NewUser) => http.post<User>('/users', user),
  changeRole: (id: string, role: UserRole) => http.put<User>(`/users/${id}/role`, { role }),
  deactivate: (id: string) => http.post<User>(`/users/${id}/deactivate`),
  activate: (id: string) => http.post<User>(`/users/${id}/activate`),
  resetPassword: (id: string, newPassword: string) =>
    http.post<null>(`/users/${id}/reset-password`, { newPassword }),
}

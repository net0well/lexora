import { http } from './http'
import type { AuthenticatedUser, ChangePasswordRequest, LoginRequest, Session } from './types/auth'

export const authApi = {
  login: (request: LoginRequest) =>
    http.post<Session>('/auth/login', request, { authenticate: false }),
  refresh: () => http.post<Session>('/auth/refresh', undefined, { authenticate: false }),
  logout: () => http.post<null>('/auth/logout'),
  me: () => http.get<AuthenticatedUser>('/auth/me'),
  changePassword: (request: ChangePasswordRequest) =>
    http.post<null>('/auth/change-password', request),
}

export type UserRole = 'admin' | 'member' | 'viewer'

export interface AuthenticatedUser {
  id: string
  email: string
  name: string
  role: UserRole
}

export interface Session {
  accessToken: string
  expiresAt: string
  user: AuthenticatedUser
}

export interface LoginRequest {
  email: string
  password: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

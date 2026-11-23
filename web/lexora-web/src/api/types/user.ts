import type { UserRole } from './auth'

export type UserStatus = 'active' | 'inactive'

export interface User {
  id: string
  email: string
  name: string
  role: UserRole
  status: UserStatus
  lastLoginAt: string | null
  createdAt: string
  updatedAt: string
}

export interface NewUser {
  email: string
  name: string
  role: UserRole
  password: string
}

export const userRules = {
  nameMinLength: 2,
  nameMaxLength: 120,
  emailMaxLength: 254,
  roles: ['admin', 'member', 'viewer'] as const satisfies readonly UserRole[],
  statuses: ['active', 'inactive'] as const satisfies readonly UserStatus[],
} as const

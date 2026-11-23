export type NotificationType =
  | 'validation'
  | 'notFound'
  | 'conflict'
  | 'unauthorized'
  | 'forbidden'
  | 'businessRule'
  | 'unavailable'

export interface ApiNotification {
  key: string
  message: string
  type: NotificationType
}

export interface CommandResult<T> {
  success: boolean
  data: T | null
  notifications: ApiNotification[]
}

export interface ApiResult<T> extends CommandResult<T> {
  status: number
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

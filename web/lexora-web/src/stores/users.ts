import { defineStore } from 'pinia'
import { computed, shallowRef } from 'vue'

import { usersApi, type UserQuery } from '@/api/users'
import type { UserRole } from '@/api/types/auth'
import type { ApiResult } from '@/api/types/commandResult'
import type { NewUser, User } from '@/api/types/user'
import { resetWhenUserChanges } from './sessionScope'

export const useUsersStore = defineStore('users', () => {
  const items = shallowRef<User[]>([])
  const totalItems = shallowRef(0)
  const pendingLoads = shallowRef(0)
  const loading = computed(() => pendingLoads.value > 0)

  resetWhenUserChanges(() => {
    items.value = []
    totalItems.value = 0
  })

  async function load(query: UserQuery, signal?: AbortSignal) {
    pendingLoads.value++

    try {
      const result = await usersApi.list(query, signal)

      if (result.success && result.data) {
        items.value = result.data.items
        totalItems.value = result.data.totalItems
      }

      return result
    } finally {
      pendingLoads.value--
    }
  }

  function replace(result: ApiResult<User>): ApiResult<User> {
    const updated = result.data

    if (result.success && updated) {
      items.value = items.value.map((user) => (user.id === updated.id ? updated : user))
    }

    return result
  }

  async function create(user: NewUser) {
    const result = await usersApi.create(user)
    const created = result.data

    if (result.success && created) {
      items.value = [created, ...items.value]
      totalItems.value++
    }

    return result
  }

  async function changeRole(id: string, role: UserRole) {
    return replace(await usersApi.changeRole(id, role))
  }

  async function deactivate(id: string) {
    return replace(await usersApi.deactivate(id))
  }

  async function activate(id: string) {
    return replace(await usersApi.activate(id))
  }

  function resetPassword(id: string, newPassword: string) {
    return usersApi.resetPassword(id, newPassword)
  }

  return {
    items,
    totalItems,
    pendingLoads,
    loading,
    load,
    create,
    changeRole,
    deactivate,
    activate,
    resetPassword,
  }
})

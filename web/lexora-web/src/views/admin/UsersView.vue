<script setup lang="ts">
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable, { type DataTablePageEvent } from 'primevue/datatable'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import { useConfirm } from 'primevue/useconfirm'
import { storeToRefs } from 'pinia'
import { computed, onBeforeUnmount, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { isAbort } from '@/api/http'
import type { UserRole } from '@/api/types/auth'
import { userRules, type NewUser, type User, type UserStatus } from '@/api/types/user'
import ResetPasswordDialog from '@/components/users/ResetPasswordDialog.vue'
import UserCreateDialog from '@/components/users/UserCreateDialog.vue'
import { useFormat } from '@/composables/useFormat'
import { useNotifications } from '@/composables/useNotifications'
import { useQueryState } from '@/composables/useQueryState'
import { useAuthStore } from '@/stores/auth'
import { useUsersStore } from '@/stores/users'

const pageSize = 20
const searchDelayMs = 300

const { t } = useI18n()
const confirm = useConfirm()
const format = useFormat()
const query = useQueryState()
const auth = useAuthStore()
const store = useUsersStore()
const { items, totalItems, loading } = storeToRefs(store)
const { describe, notify, success } = useNotifications()

const role = computed<UserRole | null>(() => {
  const value = query.text('role') as UserRole
  return userRules.roles.includes(value) ? value : null
})
const status = computed<UserStatus | null>(() => {
  const value = query.text('status') as UserStatus
  return userRules.statuses.includes(value) ? value : null
})
const appliedSearch = computed(() => query.text('search'))
const page = computed(() => query.positiveInteger('page'))
const search = shallowRef(appliedSearch.value)
let searchTimer: ReturnType<typeof setTimeout> | undefined

const creating = shallowRef(false)
const saving = shallowRef(false)
const createError = shallowRef<string | null>(null)
const resetting = shallowRef<User | null>(null)
const resetSaving = shallowRef(false)
const resetError = shallowRef<string | null>(null)

const roleOptions = computed(() =>
  userRules.roles.map((value) => ({ value, label: t(`roles.${value}`) })),
)
const roleFilterOptions = computed(() => [
  { value: null, label: t('users.filters.allRoles') },
  ...roleOptions.value,
])
const statusFilterOptions = computed(() => [
  { value: null, label: t('users.filters.allStatuses') },
  ...userRules.statuses.map((value) => ({ value, label: t(`users.status.${value}`) })),
])

function isSelf(user: User): boolean {
  return user.id === auth.user?.id
}

function changeFilter(changes: Record<string, string | null>): void {
  query.update({ ...changes, page: null })
}

function changePage(event: DataTablePageEvent): void {
  query.update({ page: event.page === 0 ? null : event.page + 1 })
}

async function create(user: NewUser): Promise<void> {
  saving.value = true
  createError.value = null

  try {
    const result = await store.create(user)

    if (result.success) {
      creating.value = false
      success(t('users.created', { name: user.name }))
    } else {
      createError.value = result.notifications.map(describe).join(' ')
    }
  } finally {
    saving.value = false
  }
}

function confirmRole(user: User, next: UserRole): void {
  if (next === user.role) {
    return
  }

  confirm.require({
    header: t('users.role.title'),
    message: t('users.role.confirm', { name: user.name, role: t(`roles.${next}`) }),
    icon: 'pi pi-id-card',
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    acceptProps: { label: t('common.confirm') },
    accept: async () => {
      const result = await store.changeRole(user.id, next)

      if (result.success) {
        success(t('users.role.changed', { name: user.name }))
      } else {
        notify(result.notifications)
      }
    },
  })
}

function confirmStatus(user: User): void {
  const deactivating = user.status === 'active'

  confirm.require({
    header: t(deactivating ? 'users.deactivate.title' : 'users.activate.title'),
    message: t(deactivating ? 'users.deactivate.confirm' : 'users.activate.confirm', {
      name: user.name,
    }),
    icon: deactivating ? 'pi pi-ban' : 'pi pi-check-circle',
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    acceptProps: {
      label: t(deactivating ? 'users.deactivate.action' : 'users.activate.action'),
      severity: deactivating ? 'danger' : undefined,
    },
    accept: async () => {
      const result = deactivating ? await store.deactivate(user.id) : await store.activate(user.id)

      if (result.success) {
        success(
          t(deactivating ? 'users.deactivate.done' : 'users.activate.done', { name: user.name }),
        )
      } else {
        notify(result.notifications)
      }
    },
  })
}

function openReset(user: User): void {
  resetError.value = null
  resetting.value = user
}

async function reset(user: User, newPassword: string): Promise<void> {
  resetSaving.value = true
  resetError.value = null

  try {
    const result = await store.resetPassword(user.id, newPassword)

    if (result.success) {
      resetting.value = null
      success(t('users.reset.done', { name: user.name }))
    } else {
      resetError.value = result.notifications.map(describe).join(' ')
    }
  } finally {
    resetSaving.value = false
  }
}

watch(search, (value) => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    if (value.trim() !== appliedSearch.value) {
      changeFilter({ search: value.trim() })
    }
  }, searchDelayMs)
})
watch(appliedSearch, (value) => {
  if (value !== search.value.trim()) {
    search.value = value
  }
})
watch(
  [appliedSearch, role, status, page],
  async ([currentSearch, currentRole, currentStatus, currentPage], _previous, onCleanup) => {
    const controller = new AbortController()
    onCleanup(() => controller.abort())

    try {
      const result = await store.load(
        {
          search: currentSearch,
          role: currentRole,
          status: currentStatus,
          page: currentPage,
          pageSize,
        },
        controller.signal,
      )

      if (!result.success) {
        notify(result.notifications)
      }
    } catch (error) {
      if (!isAbort(error)) {
        throw error
      }
    }
  },
  { immediate: true },
)
onBeforeUnmount(() => clearTimeout(searchTimer))
</script>

<template>
  <section class="page">
    <header class="page__header">
      <div>
        <h1>{{ t('nav.users') }}</h1>
        <p class="page__subtitle">{{ t('users.subtitle') }}</p>
      </div>
      <Button :label="t('users.new')" icon="pi pi-user-plus" @click="creating = true" />
    </header>

    <div class="users__filters">
      <IconField>
        <InputIcon class="pi pi-search" />
        <InputText
          v-model="search"
          :placeholder="t('users.filters.search')"
          :aria-label="t('users.filters.search')"
        />
      </IconField>
      <Select
        :model-value="role"
        :options="roleFilterOptions"
        option-label="label"
        option-value="value"
        :aria-label="t('users.filters.role')"
        @update:model-value="changeFilter({ role: $event })"
      />
      <Select
        :model-value="status"
        :options="statusFilterOptions"
        option-label="label"
        option-value="value"
        :aria-label="t('users.filters.status')"
        @update:model-value="changeFilter({ status: $event })"
      />
    </div>

    <DataTable
      :value="items"
      data-key="id"
      lazy
      :loading="loading"
      :paginator="totalItems > pageSize"
      :rows="pageSize"
      :total-records="totalItems"
      :first="(page - 1) * pageSize"
      @page="changePage"
    >
      <template #empty>
        <span class="users__empty">{{ t('users.empty') }}</span>
      </template>

      <Column :header="t('users.columns.user')">
        <template #body="{ data: row }">
          <div class="users__person">
            <strong>{{ row.name }}</strong>
            <span class="users__email">{{ row.email }}</span>
          </div>
        </template>
      </Column>
      <Column :header="t('users.columns.role')" class="users__narrow">
        <template #body="{ data: row }">
          <Select
            :model-value="row.role"
            :options="roleOptions"
            option-label="label"
            option-value="value"
            size="small"
            :disabled="isSelf(row)"
            :aria-label="t('users.role.label', { name: row.name })"
            @update:model-value="confirmRole(row, $event)"
          />
        </template>
      </Column>
      <Column :header="t('users.columns.status')" class="users__narrow">
        <template #body="{ data: row }">
          <Tag
            :value="t(`users.status.${row.status}`)"
            :severity="row.status === 'active' ? 'success' : 'secondary'"
          />
        </template>
      </Column>
      <Column :header="t('users.columns.lastLogin')" class="users__narrow">
        <template #body="{ data: row }">
          {{ row.lastLoginAt ? format.dateTime(row.lastLoginAt) : t('users.neverSignedIn') }}
        </template>
      </Column>
      <Column :header="t('common.actions')" class="users__actions">
        <template #body="{ data: row }">
          <Button
            icon="pi pi-key"
            text
            rounded
            severity="secondary"
            :aria-label="t('users.reset.action', { name: row.name })"
            @click="openReset(row)"
          />
          <Button
            :icon="row.status === 'active' ? 'pi pi-ban' : 'pi pi-check-circle'"
            text
            rounded
            :severity="row.status === 'active' ? 'danger' : 'success'"
            :disabled="isSelf(row)"
            :aria-label="
              t(row.status === 'active' ? 'users.deactivate.label' : 'users.activate.label', {
                name: row.name,
              })
            "
            @click="confirmStatus(row)"
          />
        </template>
      </Column>
    </DataTable>

    <UserCreateDialog
      v-model:visible="creating"
      :saving="saving"
      :server-error="createError"
      @create="create"
    />
    <ResetPasswordDialog
      v-model:user="resetting"
      :saving="resetSaving"
      :server-error="resetError"
      @reset="reset"
    />
  </section>
</template>

<style scoped>
.users__filters {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.users__person {
  display: flex;
  flex-direction: column;
}

.users__email,
.users__empty {
  color: var(--p-text-muted-color);
}

:deep(.users__narrow) {
  white-space: nowrap;
}

:deep(.users__actions) {
  width: 1%;
  white-space: nowrap;
}
</style>

<script setup lang="ts">
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Password from 'primevue/password'
import { computed, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import type { User } from '@/api/types/user'
import PasswordRules from '@/components/account/PasswordRules.vue'
import { isStrongPassword } from '@/utils/passwordPolicy'

const user = defineModel<User | null>('user', { required: true })

const { saving = false, serverError = null } = defineProps<{
  saving?: boolean
  serverError?: string | null
}>()

const emit = defineEmits<{ reset: [user: User, newPassword: string] }>()

const { t } = useI18n()
const password = shallowRef('')
const submitted = shallowRef(false)

const visible = computed({
  get: () => user.value !== null,
  set: (open) => {
    if (!open) {
      user.value = null
    }
  },
})

const error = computed(() =>
  submitted.value && !isStrongPassword(password.value, user.value?.email ?? null)
    ? t('users.form.passwordWeak')
    : null,
)

function submit(): void {
  submitted.value = true

  if (user.value && !error.value) {
    emit('reset', user.value, password.value)
  }
}

watch(user, () => {
  password.value = ''
  submitted.value = false
})
</script>

<template>
  <Dialog
    v-model:visible="visible"
    modal
    :header="t('users.reset.title', { name: user?.name ?? '' })"
    :style="{ width: 'min(30rem, 95vw)' }"
  >
    <form class="reset" novalidate @submit.prevent="submit">
      <p class="reset__hint">{{ t('users.reset.hint') }}</p>
      <p v-if="serverError" class="field__error" role="alert">{{ serverError }}</p>

      <div class="field">
        <label for="reset-password">{{ t('users.reset.password') }}</label>
        <Password
          v-model="password"
          input-id="reset-password"
          :input-props="{ autocomplete: 'new-password' }"
          :feedback="false"
          :invalid="Boolean(error)"
          toggle-mask
          fluid
        />
        <small v-if="error" class="field__error">{{ error }}</small>
        <PasswordRules :password="password" :email="user?.email ?? null" />
      </div>

      <div class="reset__actions">
        <Button
          type="button"
          :label="t('common.cancel')"
          severity="secondary"
          text
          @click="visible = false"
        />
        <Button type="submit" :label="t('users.reset.submit')" :loading="saving" />
      </div>
    </form>
  </Dialog>
</template>

<style scoped>
.reset {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.reset__hint {
  margin: 0;
  color: var(--p-text-muted-color);
}

.reset__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>

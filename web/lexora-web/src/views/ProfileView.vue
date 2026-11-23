<script setup lang="ts">
import Button from 'primevue/button'
import Password from 'primevue/password'
import { computed, reactive, shallowRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

import { authApi } from '@/api/auth'
import PasswordRules from '@/components/account/PasswordRules.vue'
import { useNotifications } from '@/composables/useNotifications'
import { useAuthStore } from '@/stores/auth'
import { isStrongPassword } from '@/utils/passwordPolicy'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const { describe } = useNotifications()

const form = reactive({ current: '', next: '', confirmation: '' })
const submitted = shallowRef(false)
const saving = shallowRef(false)
const serverError = shallowRef<string | null>(null)

const email = computed(() => auth.user?.email ?? null)

const errors = computed(() => {
  if (!submitted.value) {
    return {}
  }

  return {
    current: form.current ? null : t('profile.password.currentRequired'),
    next: isStrongPassword(form.next, email.value) ? null : t('profile.password.weak'),
    confirmation:
      form.confirmation === form.next ? null : t('profile.password.confirmationMismatch'),
  }
})

const hasErrors = computed(() => Object.values(errors.value).some(Boolean))

async function changePassword(): Promise<void> {
  submitted.value = true
  serverError.value = null

  if (hasErrors.value) {
    return
  }

  saving.value = true

  try {
    const result = await authApi.changePassword({
      currentPassword: form.current,
      newPassword: form.next,
    })

    if (!result.success) {
      serverError.value = result.notifications.map(describe).join(' ')
      return
    }

    // The server revokes every session of the user, this one included.
    auth.forget()
    await router.replace({ name: 'login', query: { reason: 'passwordChanged' } })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <section class="page profile">
    <header class="page__header">
      <h1>{{ t('profile.title') }}</h1>
    </header>

    <section class="profile__card">
      <h2 class="profile__heading">{{ t('profile.details') }}</h2>
      <dl class="profile__details">
        <dt class="profile__label">{{ t('profile.name') }}</dt>
        <dd class="profile__value">{{ auth.user?.name }}</dd>
        <dt class="profile__label">{{ t('profile.email') }}</dt>
        <dd class="profile__value">{{ auth.user?.email }}</dd>
        <dt class="profile__label">{{ t('profile.role') }}</dt>
        <dd class="profile__value">{{ auth.user ? t(`roles.${auth.user.role}`) : '' }}</dd>
      </dl>
    </section>

    <section class="profile__card">
      <h2 class="profile__heading">{{ t('profile.password.title') }}</h2>
      <p class="profile__hint">{{ t('profile.password.hint') }}</p>

      <form class="profile__form" novalidate @submit.prevent="changePassword">
        <p v-if="serverError" class="field__error" role="alert">{{ serverError }}</p>

        <div class="field">
          <label for="current-password">{{ t('profile.password.current') }}</label>
          <Password
            v-model="form.current"
            input-id="current-password"
            :input-props="{ autocomplete: 'current-password' }"
            :feedback="false"
            :invalid="Boolean(errors.current)"
            toggle-mask
            fluid
          />
          <small v-if="errors.current" class="field__error">{{ errors.current }}</small>
        </div>

        <div class="field">
          <label for="new-password">{{ t('profile.password.next') }}</label>
          <Password
            v-model="form.next"
            input-id="new-password"
            :input-props="{ autocomplete: 'new-password' }"
            :feedback="false"
            :invalid="Boolean(errors.next)"
            toggle-mask
            fluid
          />
          <small v-if="errors.next" class="field__error">{{ errors.next }}</small>
          <PasswordRules :password="form.next" :email="email" />
        </div>

        <div class="field">
          <label for="confirm-password">{{ t('profile.password.confirmation') }}</label>
          <Password
            v-model="form.confirmation"
            input-id="confirm-password"
            :input-props="{ autocomplete: 'new-password' }"
            :feedback="false"
            :invalid="Boolean(errors.confirmation)"
            toggle-mask
            fluid
          />
          <small v-if="errors.confirmation" class="field__error">{{ errors.confirmation }}</small>
        </div>

        <div class="profile__actions">
          <Button type="submit" :label="t('profile.password.submit')" :loading="saving" />
        </div>
      </form>
    </section>
  </section>
</template>

<style scoped>
.profile {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  max-width: 40rem;
}

.profile__card {
  padding: 1.25rem;
  border: 1px solid var(--p-content-border-color);
  border-radius: 12px;
  background: var(--p-content-background);
}

.profile__heading {
  font-size: 1rem;
}

.profile__hint {
  margin: 0 0 1rem;
  color: var(--p-text-muted-color);
}

.profile__details {
  display: grid;
  grid-template-columns: max-content 1fr;
  gap: 0.5rem 1.5rem;
  margin: 0;
}

.profile__label {
  color: var(--p-text-muted-color);
}

.profile__value {
  margin: 0;
}

.profile__form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.profile__actions {
  display: flex;
  justify-content: flex-end;
}
</style>

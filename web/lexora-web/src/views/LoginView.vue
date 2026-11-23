<script setup lang="ts">
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Password from 'primevue/password'
import { computed, reactive, shallowRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'

import AppLogo from '@/components/layout/AppLogo.vue'
import LocaleSelector from '@/components/layout/LocaleSelector.vue'
import { useNotifications } from '@/composables/useNotifications'
import { safeRedirect } from '@/router/redirects'
import { useAuthStore } from '@/stores/auth'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const { describe } = useNotifications()

const form = reactive({ email: '', password: '' })
const submitted = shallowRef(false)
const submitting = shallowRef(false)
const error = shallowRef<string | null>(null)

const emailMissing = computed(() => submitted.value && !form.email.trim())
const passwordMissing = computed(() => submitted.value && !form.password)
const notice = computed(() => {
  const reason = route.query.reason

  return reason === 'expired' || reason === 'passwordChanged' ? t(`auth.login.${reason}`) : null
})

async function submit(): Promise<void> {
  submitted.value = true
  error.value = null

  if (!form.email.trim() || !form.password) {
    return
  }

  submitting.value = true

  try {
    const result = await auth.login({ email: form.email.trim(), password: form.password })

    if (result.success) {
      await router.replace(safeRedirect(route.query.redirect) ?? { name: 'knowledge-bases' })
      return
    }

    error.value = result.notifications.map(describe).join(' ') || t('notifications.unexpected')
    form.password = ''
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <main class="login">
    <section class="login__panel">
      <header class="login__header">
        <AppLogo size="2.75rem" />
        <div>
          <h1 class="login__title">{{ t('app.name') }}</h1>
          <p class="login__tagline">{{ t('app.tagline') }}</p>
        </div>
      </header>

      <Message v-if="notice && !error" severity="info">{{ notice }}</Message>
      <Message v-if="error" severity="error" role="alert">{{ error }}</Message>

      <form class="login__form" novalidate @submit.prevent="submit">
        <div class="field">
          <label for="login-email">{{ t('auth.login.email') }}</label>
          <InputText
            id="login-email"
            v-model="form.email"
            type="email"
            name="email"
            autocomplete="username"
            :invalid="emailMissing"
            fluid
          />
          <small v-if="emailMissing" class="field__error">{{
            t('auth.login.emailRequired')
          }}</small>
        </div>

        <div class="field">
          <label for="login-password">{{ t('auth.login.password') }}</label>
          <Password
            v-model="form.password"
            input-id="login-password"
            name="password"
            :input-props="{ autocomplete: 'current-password' }"
            :feedback="false"
            :invalid="passwordMissing"
            toggle-mask
            fluid
          />
          <small v-if="passwordMissing" class="field__error">{{
            t('auth.login.passwordRequired')
          }}</small>
        </div>

        <Button type="submit" :label="t('auth.login.submit')" :loading="submitting" fluid />
      </form>

      <footer class="login__footer">
        <LocaleSelector />
      </footer>
    </section>
  </main>
</template>

<style scoped>
.login {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  padding: 1.5rem;
}

.login__panel {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  width: 100%;
  max-width: 24rem;
  padding: 2rem;
  border: 1px solid var(--p-content-border-color);
  border-radius: 12px;
  background: var(--p-content-background);
  box-shadow: 0 1px 3px rgb(0 0 0 / 6%);
}

.login__header {
  display: flex;
  align-items: center;
  gap: 0.875rem;
}

.login__title {
  margin: 0;
}

.login__tagline {
  margin: 0.125rem 0 0;
  color: var(--p-text-muted-color);
}

.login__form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.login__footer {
  display: flex;
  justify-content: flex-end;
}
</style>

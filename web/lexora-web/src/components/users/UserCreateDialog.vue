<script setup lang="ts">
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Select from 'primevue/select'
import { computed, reactive, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import type { UserRole } from '@/api/types/auth'
import { userRules, type NewUser } from '@/api/types/user'
import PasswordRules from '@/components/account/PasswordRules.vue'
import { isStrongPassword } from '@/utils/passwordPolicy'

const visible = defineModel<boolean>('visible', { required: true })

const { saving = false, serverError = null } = defineProps<{
  saving?: boolean
  serverError?: string | null
}>()

const emit = defineEmits<{ create: [user: NewUser] }>()

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const { t } = useI18n()

const form = reactive({ email: '', name: '', role: 'viewer' as UserRole, password: '' })
const submitted = shallowRef(false)

const roleOptions = computed(() =>
  userRules.roles.map((role) => ({ value: role, label: t(`roles.${role}`) })),
)

const errors = computed(() => {
  if (!submitted.value) {
    return {}
  }

  const name = form.name.trim()
  const email = form.email.trim()

  return {
    email:
      emailPattern.test(email) && email.length <= userRules.emailMaxLength
        ? null
        : t('users.form.emailInvalid'),
    name:
      name.length >= userRules.nameMinLength && name.length <= userRules.nameMaxLength
        ? null
        : t('users.form.nameLength', {
            min: userRules.nameMinLength,
            max: userRules.nameMaxLength,
          }),
    password: isStrongPassword(form.password, email) ? null : t('users.form.passwordWeak'),
  }
})

const hasErrors = computed(() => Object.values(errors.value).some(Boolean))

function submit(): void {
  submitted.value = true

  if (!hasErrors.value) {
    emit('create', {
      email: form.email.trim(),
      name: form.name.trim(),
      role: form.role,
      password: form.password,
    })
  }
}

watch(visible, (open) => {
  if (open) {
    Object.assign(form, { email: '', name: '', role: 'viewer', password: '' })
    submitted.value = false
  }
})
</script>

<template>
  <Dialog
    v-model:visible="visible"
    modal
    :header="t('users.new')"
    :style="{ width: 'min(32rem, 95vw)' }"
  >
    <form class="user-form" novalidate @submit.prevent="submit">
      <p v-if="serverError" class="field__error" role="alert">{{ serverError }}</p>

      <div class="field">
        <label for="new-user-name">{{ t('users.form.name') }}</label>
        <InputText
          id="new-user-name"
          v-model="form.name"
          :maxlength="userRules.nameMaxLength"
          :invalid="Boolean(errors.name)"
          fluid
        />
        <small v-if="errors.name" class="field__error">{{ errors.name }}</small>
      </div>

      <div class="field">
        <label for="new-user-email">{{ t('users.form.email') }}</label>
        <InputText
          id="new-user-email"
          v-model="form.email"
          type="email"
          autocomplete="off"
          :maxlength="userRules.emailMaxLength"
          :invalid="Boolean(errors.email)"
          fluid
        />
        <small v-if="errors.email" class="field__error">{{ errors.email }}</small>
      </div>

      <div class="field">
        <label for="new-user-role">{{ t('users.form.role') }}</label>
        <Select
          v-model="form.role"
          input-id="new-user-role"
          :options="roleOptions"
          option-label="label"
          option-value="value"
          fluid
        />
      </div>

      <div class="field">
        <label for="new-user-password">{{ t('users.form.password') }}</label>
        <Password
          v-model="form.password"
          input-id="new-user-password"
          :input-props="{ autocomplete: 'new-password' }"
          :feedback="false"
          :invalid="Boolean(errors.password)"
          toggle-mask
          fluid
        />
        <small v-if="errors.password" class="field__error">{{ errors.password }}</small>
        <PasswordRules :password="form.password" :email="form.email" />
      </div>

      <div class="user-form__actions">
        <Button
          type="button"
          :label="t('common.cancel')"
          severity="secondary"
          text
          @click="visible = false"
        />
        <Button type="submit" :label="t('common.create')" :loading="saving" />
      </div>
    </form>
  </Dialog>
</template>

<style scoped>
.user-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.user-form__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>

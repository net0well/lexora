<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { passwordChecks, passwordMinimumLength, type PasswordRule } from '@/utils/passwordPolicy'

const { password, email = null } = defineProps<{ password: string; email?: string | null }>()

const { t } = useI18n()

const rules = computed(() =>
  (Object.entries(passwordChecks(password, email)) as [PasswordRule, boolean][]).map(
    ([rule, met]) => ({
      rule,
      met,
      text: t(`passwordRules.${rule}`, { min: passwordMinimumLength }),
    }),
  ),
)
</script>

<template>
  <ul class="password-rules" :aria-label="t('passwordRules.label')">
    <li
      v-for="item in rules"
      :key="item.rule"
      class="password-rules__item"
      :class="{ 'password-rules__item--met': item.met }"
    >
      <i
        :class="['password-rules__icon', item.met ? 'pi pi-check-circle' : 'pi pi-circle']"
        aria-hidden="true"
      />
      <span>{{ item.text }}</span>
      <span class="sr-only">{{
        item.met ? t('passwordRules.met') : t('passwordRules.missing')
      }}</span>
    </li>
  </ul>
</template>

<style scoped>
.password-rules {
  display: grid;
  gap: 0.25rem;
  margin: 0;
  padding: 0;
  list-style: none;
  color: var(--p-text-muted-color);
  font-size: 0.85rem;
}

.password-rules__item {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.password-rules__item--met {
  color: var(--p-green-700);
}
</style>

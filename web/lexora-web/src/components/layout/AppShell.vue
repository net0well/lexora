<script setup lang="ts">
import Avatar from 'primevue/avatar'
import Button from 'primevue/button'
import Menu from 'primevue/menu'
import type { MenuItem } from 'primevue/menuitem'
import { computed, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

import { useAuthStore } from '@/stores/auth'
import AppLogo from './AppLogo.vue'
import LocaleSelector from './LocaleSelector.vue'

interface NavigationLink {
  name: string
  label: string
  icon: string
  visible: boolean
}

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const menu = useTemplateRef('menu')

const initials = computed(() =>
  (auth.user?.name ?? '')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join(''),
)

const links = computed<NavigationLink[]>(() =>
  [
    {
      name: 'knowledge-bases',
      label: 'nav.knowledgeBases',
      icon: 'pi pi-book',
      visible: true,
    },
    {
      name: 'admin-users',
      label: 'nav.users',
      icon: 'pi pi-users',
      visible: auth.hasRole('admin'),
    },
  ].filter((link) => link.visible && router.hasRoute(link.name)),
)

const menuItems = computed<MenuItem[]>(() => [
  {
    label: t('nav.profile'),
    icon: 'pi pi-user',
    command: () => void router.push({ name: 'profile' }),
  },
  {
    label: t('auth.logout'),
    icon: 'pi pi-sign-out',
    command: () => void signOut(),
  },
])

function toggleMenu(event: Event): void {
  menu.value?.toggle(event)
}

async function signOut(): Promise<void> {
  await auth.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <div class="shell">
    <header class="shell__header">
      <RouterLink :to="{ name: 'knowledge-bases' }" class="shell__brand">
        <AppLogo size="1.75rem" />
        <span>{{ t('app.name') }}</span>
      </RouterLink>

      <nav class="shell__nav" :aria-label="t('shell.navigation')">
        <RouterLink
          v-for="link in links"
          :key="link.name"
          :to="{ name: link.name }"
          class="shell__link"
          active-class="shell__link--active"
        >
          <i :class="link.icon" aria-hidden="true" />
          <span class="shell__link-label">{{ t(link.label) }}</span>
        </RouterLink>
      </nav>

      <div class="shell__actions">
        <LocaleSelector />
        <Button
          type="button"
          text
          severity="secondary"
          class="shell__user"
          :aria-label="t('shell.userMenu')"
          aria-haspopup="true"
          aria-controls="shell-user-menu"
          @click="toggleMenu"
        >
          <Avatar :label="initials" shape="circle" />
          <span class="shell__user-name">{{ auth.user?.name }}</span>
          <i class="pi pi-angle-down" aria-hidden="true" />
        </Button>
        <Menu id="shell-user-menu" ref="menu" :model="menuItems" popup>
          <template #start>
            <div class="shell__menu-header">
              <strong>{{ auth.user?.name }}</strong>
              <span class="shell__email">{{ auth.user?.email }}</span>
              <span class="shell__role">{{ auth.user ? t(`roles.${auth.user.role}`) : '' }}</span>
            </div>
          </template>
        </Menu>
      </div>
    </header>

    <main class="shell__content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.shell {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.shell__header {
  position: sticky;
  top: 0;
  z-index: 10;
  display: flex;
  align-items: center;
  gap: 1.5rem;
  height: 3.5rem;
  padding: 0 1.25rem;
  border-bottom: 1px solid var(--p-content-border-color);
  background: var(--p-content-background);
}

.shell__brand {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1.05rem;
  font-weight: 600;
  color: inherit;
  text-decoration: none;
}

.shell__nav {
  display: flex;
  flex: 1;
  gap: 0.25rem;
}

.shell__link {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
  color: var(--p-text-muted-color);
  text-decoration: none;
}

.shell__link:hover {
  background: var(--p-surface-100);
  color: var(--p-text-color);
}

.shell__link--active {
  background: var(--p-primary-50);
  color: var(--p-primary-700);
}

.shell__actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.shell__user {
  gap: 0.5rem;
}

.shell__menu-header {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--p-content-border-color);
}

.shell__email,
.shell__role {
  color: var(--p-text-muted-color);
}

.shell__role {
  font-size: 0.8rem;
}

.shell__content {
  flex: 1;
}

@media (max-width: 640px) {
  .shell__header {
    gap: 0.75rem;
    padding: 0 0.75rem;
  }

  .shell__link-label,
  .shell__user-name {
    display: none;
  }
}
</style>

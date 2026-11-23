import {
  createRouter,
  createWebHistory,
  type RouteRecordRaw,
  type Router,
  type RouterHistory,
} from 'vue-router'

import type { UserRole } from '@/api/types/auth'
import { installGuards } from './guards'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    guestOnly?: boolean
    roles?: UserRole[]
  }
}

export const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { guestOnly: true },
  },
  {
    path: '/',
    component: () => import('@/components/layout/AppShell.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'knowledge-bases',
        component: () => import('@/views/KnowledgeBasesView.vue'),
      },
      {
        path: 'knowledge-bases/:id',
        name: 'knowledge-base',
        component: () => import('@/views/KnowledgeBaseView.vue'),
        props: true,
      },
      {
        path: 'knowledge-bases/:id/chat/:conversationId?',
        name: 'chat',
        component: () => import('@/views/ChatView.vue'),
        props: true,
      },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/NotFoundView.vue'),
  },
]

export function createAppRouter(history: RouterHistory = createWebHistory()): Router {
  const router = createRouter({ history, routes })
  installGuards(router)

  return router
}

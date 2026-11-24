import { useToast } from 'primevue/usetoast'
import { useI18n } from 'vue-i18n'

import type { ApiNotification, NotificationType } from '@/api/types/commandResult'

const warnings: NotificationType[] = ['validation', 'conflict', 'businessRule', 'notFound']

export function useNotifications() {
  const toast = useToast()
  const { t, te } = useI18n()

  function describe(notification: ApiNotification): string {
    const key = `notifications.${notification.key}`

    return te(key) ? t(key) : notification.message || t('notifications.unexpected')
  }

  function notify(notifications: ApiNotification[]): void {
    for (const notification of notifications) {
      const warning = warnings.includes(notification.type)

      toast.add({
        severity: warning ? 'warn' : 'error',
        summary: t(warning ? 'feedback.warning' : 'feedback.error'),
        detail: describe(notification),
        life: 6000,
      })
    }
  }

  function success(detail: string): void {
    toast.add({ severity: 'success', summary: t('feedback.success'), detail, life: 3000 })
  }

  return { describe, notify, success }
}

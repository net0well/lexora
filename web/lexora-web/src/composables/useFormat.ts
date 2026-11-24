import { useI18n } from 'vue-i18n'

import { formatBytes, formatDateTime, formatNumber } from '@/utils/format'

export function useFormat() {
  const { locale } = useI18n()

  return {
    dateTime: (value: string | null | undefined) => formatDateTime(value, locale.value),
    number: (value: number) => formatNumber(value, locale.value),
    bytes: (value: number) => formatBytes(value, locale.value),
  }
}

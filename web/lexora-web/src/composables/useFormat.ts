import { useI18n } from 'vue-i18n'

const units = ['B', 'KB', 'MB', 'GB', 'TB']

export function useFormat() {
  const { locale } = useI18n()

  function dateTime(value: string | null | undefined): string {
    return value
      ? new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }).format(
          new Date(value),
        )
      : ''
  }

  function number(value: number): string {
    return new Intl.NumberFormat(locale.value).format(value)
  }

  function bytes(value: number): string {
    let size = value
    let unit = 0

    while (size >= 1024 && unit < units.length - 1) {
      size /= 1024
      unit++
    }

    const digits = unit === 0 || size >= 100 ? 0 : 1

    return `${new Intl.NumberFormat(locale.value, { maximumFractionDigits: digits }).format(size)} ${units[unit]}`
  }

  return { dateTime, number, bytes }
}

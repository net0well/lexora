const byteUnits = ['B', 'KB', 'MB', 'GB', 'TB']

export function formatDateTime(value: string | null | undefined, locale: string): string {
  return value
    ? new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(
        new Date(value),
      )
    : ''
}

export function formatNumber(value: number, locale: string): string {
  return new Intl.NumberFormat(locale).format(value)
}

export function formatBytes(value: number, locale: string): string {
  let size = value
  let unit = 0

  while (size >= 1024 && unit < byteUnits.length - 1) {
    size /= 1024
    unit++
  }

  const digits = unit === 0 || size >= 100 ? 0 : 1

  return `${new Intl.NumberFormat(locale, { maximumFractionDigits: digits }).format(size)} ${byteUnits[unit]}`
}

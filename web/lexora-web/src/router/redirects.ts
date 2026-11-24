// Only same-site paths: an absolute or protocol-relative address would turn the login page into an open redirect.
export function safeRedirect(value: unknown): string | null {
  return typeof value === 'string' &&
    value.startsWith('/') &&
    !value.startsWith('//') &&
    !value.includes('\\')
    ? value
    : null
}

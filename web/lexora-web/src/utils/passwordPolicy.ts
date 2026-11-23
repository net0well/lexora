export type PasswordRule = 'length' | 'upper' | 'lower' | 'digit' | 'email'

export const passwordMinimumLength = 12

export function passwordChecks(
  password: string,
  email: string | null,
): Record<PasswordRule, boolean> {
  const trimmedEmail = email?.trim().toLowerCase() ?? ''

  return {
    length: password.length >= passwordMinimumLength,
    upper: /\p{Lu}/u.test(password),
    lower: /\p{Ll}/u.test(password),
    digit: /\p{Nd}/u.test(password),
    email: trimmedEmail === '' || !password.toLowerCase().includes(trimmedEmail),
  }
}

export function isStrongPassword(password: string, email: string | null): boolean {
  return Object.values(passwordChecks(password, email)).every(Boolean)
}

import { describe, expect, it } from 'vitest'

import { safeRedirect } from '../redirects'

describe('safeRedirect', () => {
  it.each(['/', '/knowledge-bases/1?tab=search', '/admin/users'])('keeps the path %s', (path) => {
    expect(safeRedirect(path)).toBe(path)
  })

  it.each([
    'https://evil.example',
    '//evil.example',
    '/\\evil.example',
    'javascript:alert(1)',
    '',
    42,
    null,
  ])('drops %s', (value) => {
    expect(safeRedirect(value)).toBeNull()
  })
})

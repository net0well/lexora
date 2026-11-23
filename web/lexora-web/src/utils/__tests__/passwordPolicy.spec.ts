import { describe, expect, it } from 'vitest'

import { isStrongPassword, passwordChecks } from '../passwordPolicy'

describe('password policy', () => {
  it.each([
    ['Lexora2026Segura', true],
    ['ÁrvoreVerde2026', true],
    ['curta1A', false],
    ['semnumeroalgumAqui', false],
    ['SEMMINUSCULAS2026', false],
    ['semmaiusculas2026', false],
  ])('judges %s as strong: %s', (password, strong) => {
    expect(isStrongPassword(password, 'ana@lexora.local')).toBe(strong)
  })

  it('refuses a password that contains the e-mail in any case', () => {
    expect(passwordChecks('XANA@LEXORA.LOCAL1a', 'ana@lexora.local').email).toBe(false)
    expect(isStrongPassword('Ana@Lexora.Local2026', 'ana@lexora.local')).toBe(false)
  })
})

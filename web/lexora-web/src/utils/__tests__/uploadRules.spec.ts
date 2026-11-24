import { describe, expect, it } from 'vitest'

import { documentRules } from '@/api/types/document'
import { rejectionOf } from '../uploadRules'

function fileOf(name: string, size: number): File {
  const file = new File(['x'], name)
  Object.defineProperty(file, 'size', { value: size })

  return file
}

describe('rejectionOf', () => {
  it.each(['manual.pdf', 'Escala.DOCX', 'planilha.xlsx', 'notas.md', 'dados.csv', 'leia.txt'])(
    'accepts %s',
    (name) => {
      expect(rejectionOf(fileOf(name, 10))).toBeNull()
    },
  )

  it('refuses a format the server does not read', () => {
    expect(rejectionOf(fileOf('foto.png', 10))).toBe('format')
  })

  it('refuses an empty file', () => {
    expect(rejectionOf(fileOf('manual.pdf', 0))).toBe('empty')
  })

  it('refuses a file over the size limit', () => {
    expect(rejectionOf(fileOf('manual.pdf', documentRules.maxFileSizeBytes))).toBeNull()
    expect(rejectionOf(fileOf('manual.pdf', documentRules.maxFileSizeBytes + 1))).toBe('size')
  })
})

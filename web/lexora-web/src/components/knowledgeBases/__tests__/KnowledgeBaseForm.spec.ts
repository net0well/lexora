import { describe, expect, it } from 'vitest'

import { mountWithApp } from '@/__tests__/support'
import KnowledgeBaseForm from '../KnowledgeBaseForm.vue'

async function form(initial?: Record<string, unknown>) {
  const { wrapper } = await mountWithApp(KnowledgeBaseForm, {
    path: '/login',
    props: { submitLabel: 'Salvar', initial },
  })

  return wrapper
}

describe('KnowledgeBaseForm', () => {
  it('applies the same limits as the server before submitting', async () => {
    const wrapper = await form({
      name: 'ab',
      description: null,
      chunkMaxTokens: 100,
      chunkOverlapTokens: 0,
    })

    await wrapper.find('form').trigger('submit')

    expect(wrapper.text()).toContain('O nome precisa ter de 3 a 120 caracteres.')
    expect(wrapper.text()).toContain('Use um valor de 200 a 2000.')
    expect(wrapper.emitted('submit')).toBeUndefined()
  })

  it('limits the overlap to half of the chunk size', async () => {
    const wrapper = await form({
      name: 'Manuais',
      description: null,
      chunkMaxTokens: 400,
      chunkOverlapTokens: 201,
    })

    await wrapper.find('form').trigger('submit')

    expect(wrapper.text()).toContain('Use um valor de 0 a 200')
    expect(wrapper.emitted('submit')).toBeUndefined()
  })

  it('submits trimmed values with the default chunking', async () => {
    const wrapper = await form()

    await wrapper.find('#knowledge-base-name').setValue('  Manuais  ')
    await wrapper.find('#knowledge-base-description').setValue('   ')
    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('submit')).toEqual([
      [{ name: 'Manuais', description: null, chunkMaxTokens: 800, chunkOverlapTokens: 120 }],
    ])
  })
})

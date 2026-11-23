import { describe, expect, it } from 'vitest'

import type { Citation } from '@/api/types/conversation'
import { mountWithApp } from '@/__tests__/support'
import MessageBubble from '../MessageBubble.vue'

const radius: Citation = {
  index: 1,
  documentId: 'doc-1',
  documentName: 'manual-seguranca.pdf',
  pageNumber: 1,
  sheetName: null,
  excerpt: 'Vazamentos químicos: isole a área num raio de 50 metros.',
  score: 0.03,
}

async function answer(content: string, citations: Citation[] = [radius]) {
  const { wrapper } = await mountWithApp(MessageBubble, {
    path: '/login',
    props: { role: 'assistant', content, citations, model: 'qwen2.5:7b', latencyMs: 1200 },
  })

  return wrapper
}

describe('MessageBubble', () => {
  it('opens a source from its marker in the answer', async () => {
    const wrapper = await answer('Isole a área [1].')

    await wrapper.find('button[aria-label="Abrir a fonte 1"]').trigger('click')

    expect(wrapper.emitted('cite')).toEqual([[1]])
  })

  it('lists the sources below the answer', async () => {
    const wrapper = await answer('Isole a área [1].')

    expect(wrapper.text()).toContain('Fontes:')
    expect(wrapper.text()).toContain('manual-seguranca.pdf')
    expect(wrapper.text()).toContain('Página 1')

    await wrapper.find('button[aria-pressed="false"]').trigger('click')

    expect(wrapper.emitted('cite')).toEqual([[1]])
  })

  it('does not run HTML that came in the answer', async () => {
    const wrapper = await answer('<img src=x onerror="alert(1)"> [1]')

    expect(wrapper.find('img').exists()).toBe(false)
    expect(wrapper.text()).toContain('<img src=x onerror="alert(1)">')
  })
})

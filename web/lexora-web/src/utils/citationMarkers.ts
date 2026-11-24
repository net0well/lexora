const markerPattern = /\[(\d+(?:\s*,\s*\d+)*)\]/g

function citedNumbers(marker: string, known: ReadonlySet<number>): number[] {
  return marker
    .split(',')
    .map((part) => Number(part.trim()))
    .filter((index) => known.has(index))
}

// Runs on HTML that was already sanitized and only adds buttons built here with a number as their
// content, so the markers stay clickable without letting the answer itself create elements.
export function linkCitations(
  html: string,
  known: ReadonlySet<number>,
  label: (index: number) => string,
): string {
  if (known.size === 0) {
    return html
  }

  const template = document.createElement('template')
  template.innerHTML = html
  const walker = document.createTreeWalker(template.content, NodeFilter.SHOW_TEXT)
  const texts: Text[] = []

  while (walker.nextNode()) {
    texts.push(walker.currentNode as Text)
  }

  for (const text of texts) {
    if (text.parentElement?.closest('code, pre, a, button')) {
      continue
    }

    const value = text.data
    const fragment = document.createDocumentFragment()
    let consumed = 0
    let linked = false

    for (const match of value.matchAll(markerPattern)) {
      const numbers = citedNumbers(match[1] ?? '', known)

      if (numbers.length === 0) {
        continue
      }

      fragment.append(value.slice(consumed, match.index))

      for (const index of numbers) {
        const button = document.createElement('button')
        button.type = 'button'
        button.className = 'citation-marker'
        button.dataset.citation = String(index)
        button.setAttribute('aria-label', label(index))
        button.textContent = String(index)
        fragment.append(button)
      }

      consumed = match.index + match[0].length
      linked = true
    }

    if (linked) {
      fragment.append(value.slice(consumed))
      text.replaceWith(fragment)
    }
  }

  return template.innerHTML
}

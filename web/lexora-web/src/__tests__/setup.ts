import { enableAutoUnmount } from '@vue/test-utils'
import { afterEach, beforeEach, vi } from 'vitest'

enableAutoUnmount(afterEach)

Object.defineProperty(window, 'matchMedia', {
  configurable: true,
  value: (query: string): MediaQueryList =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => false,
    }) as MediaQueryList,
})

class NoResizeObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

Object.defineProperty(window, 'ResizeObserver', { configurable: true, value: NoResizeObserver })

beforeEach(() => {
  document.body.innerHTML = ''
  vi.stubGlobal(
    'fetch',
    vi.fn(() => Promise.reject(new TypeError('Tests do not reach the network.'))),
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.useRealTimers()
  localStorage.clear()
})

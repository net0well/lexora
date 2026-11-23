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

// Node's BroadcastChannel would carry one test's sign-out to stores created by other tests.
export class TestBroadcastChannel {
  static open: TestBroadcastChannel[] = []

  private readonly listeners: ((event: MessageEvent) => void)[] = []

  constructor(readonly name: string) {
    TestBroadcastChannel.open.push(this)
  }

  addEventListener(_type: 'message', listener: (event: MessageEvent) => void): void {
    this.listeners.push(listener)
  }

  postMessage(data: unknown): void {
    for (const channel of TestBroadcastChannel.open) {
      if (channel !== this && channel.name === this.name) {
        queueMicrotask(() =>
          channel.listeners.forEach((listener) => listener({ data } as MessageEvent)),
        )
      }
    }
  }

  close(): void {
    TestBroadcastChannel.open = TestBroadcastChannel.open.filter((channel) => channel !== this)
  }
}

beforeEach(() => {
  document.body.innerHTML = ''
  TestBroadcastChannel.open = []
  vi.stubGlobal('BroadcastChannel', TestBroadcastChannel)
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

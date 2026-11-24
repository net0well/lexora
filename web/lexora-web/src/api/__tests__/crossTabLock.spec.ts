import { afterEach, describe, expect, it, vi } from 'vitest'

import { withCrossTabLock } from '../crossTabLock'

describe('withCrossTabLock', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('runs the task inside the named browser lock', async () => {
    const request = vi.fn(async (_name: string, task: () => Promise<string>) => task())
    vi.stubGlobal('navigator', { locks: { request } })

    await expect(withCrossTabLock('lexora-refresh', async () => 'renewed')).resolves.toBe('renewed')
    expect(request).toHaveBeenCalledWith('lexora-refresh', expect.any(Function))
  })

  it('runs the task directly when the browser has no locks', async () => {
    vi.stubGlobal('navigator', {})

    await expect(withCrossTabLock('lexora-refresh', async () => 'renewed')).resolves.toBe('renewed')
  })
})

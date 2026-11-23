import { describe, expect, it, vi } from 'vitest'

import { singleFlight } from '../singleFlight'

describe('singleFlight', () => {
  it('shares the running task between concurrent callers', async () => {
    const task = vi.fn(async () => 'renewed')
    const run = singleFlight(task)

    const results = await Promise.all([run(), run(), run()])

    expect(results).toEqual(['renewed', 'renewed', 'renewed'])
    expect(task).toHaveBeenCalledTimes(1)
  })

  it('runs the task again after the previous run settled', async () => {
    const task = vi.fn(async () => 'renewed')
    const run = singleFlight(task)

    await run()
    await run()

    expect(task).toHaveBeenCalledTimes(2)
  })

  it('runs again after a failed run', async () => {
    const task = vi
      .fn()
      .mockRejectedValueOnce(new Error('offline'))
      .mockResolvedValueOnce('renewed')
    const run = singleFlight(task)

    await expect(run()).rejects.toThrow('offline')
    await expect(run()).resolves.toBe('renewed')
  })
})

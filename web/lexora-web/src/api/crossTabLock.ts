// The refresh cookie rotates and a reused one revokes the whole session, so tabs must renew one at a
// time: the second tab then sends the cookie the first one received.
export async function withCrossTabLock<T>(name: string, task: () => Promise<T>): Promise<T> {
  const locks = globalThis.navigator?.locks

  return locks ? locks.request(name, task) : task()
}

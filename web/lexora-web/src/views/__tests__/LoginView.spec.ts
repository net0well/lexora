import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises } from '@vue/test-utils'

import App from '@/App.vue'
import { authApi } from '@/api/auth'
import { mountWithApp } from '@/__tests__/support'
import { session } from '@/__tests__/tokens'

vi.mock('@/api/auth', () => ({
  authApi: {
    login: vi.fn(),
    refresh: vi.fn(),
    logout: vi.fn(),
    me: vi.fn(),
    changePassword: vi.fn(),
  },
}))

const api = vi.mocked(authApi)
const signedOut = { success: false, data: null, notifications: [], status: 401 }

async function fillAndSubmit(
  wrapper: Awaited<ReturnType<typeof mountWithApp>>['wrapper'],
  email: string,
  password: string,
) {
  await wrapper.find('#login-email').setValue(email)
  await wrapper.find('#login-password').setValue(password)
  await wrapper.find('form').trigger('submit')
  await flushPromises()
}

describe('LoginView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    api.refresh.mockResolvedValue(signedOut)
  })

  it('asks for the e-mail and the password before calling the server', async () => {
    const { wrapper } = await mountWithApp(App, { path: '/login' })

    await wrapper.find('form').trigger('submit')

    expect(wrapper.text()).toContain('Informe o e-mail.')
    expect(wrapper.text()).toContain('Informe a senha.')
    expect(api.login).not.toHaveBeenCalled()
  })

  it('goes back to the page that asked for the login', async () => {
    api.login.mockResolvedValueOnce({
      success: true,
      data: session(),
      notifications: [],
      status: 200,
    })
    const { wrapper, router } = await mountWithApp(App, { path: '/' })
    expect(router.currentRoute.value.fullPath).toBe('/login?redirect=/')

    await fillAndSubmit(wrapper, ' ana@lexora.local ', 'secret')

    expect(api.login).toHaveBeenCalledWith({ email: 'ana@lexora.local', password: 'secret' })
    await vi.waitFor(() => expect(router.currentRoute.value.name).toBe('knowledge-bases'))
  })

  it('ignores a redirect to another site', async () => {
    api.login.mockResolvedValueOnce({
      success: true,
      data: session(),
      notifications: [],
      status: 200,
    })
    const { wrapper, router } = await mountWithApp(App, { path: '/login?redirect=//evil.example' })

    await fillAndSubmit(wrapper, 'ana@lexora.local', 'secret')

    await vi.waitFor(() => expect(router.currentRoute.value.fullPath).toBe('/'))
  })

  it('shows the translated reason of a refused login and clears the password', async () => {
    api.login.mockResolvedValueOnce({
      success: false,
      data: null,
      notifications: [
        {
          key: 'auth.invalidCredentials',
          message: 'The e-mail or password is incorrect.',
          type: 'unauthorized',
        },
      ],
      status: 401,
    })
    const { wrapper } = await mountWithApp(App, { path: '/login' })

    await fillAndSubmit(wrapper, 'ana@lexora.local', 'wrong')

    expect(wrapper.find('[role="alert"]').text()).toContain('E-mail ou senha incorretos.')
    expect((wrapper.find('#login-password').element as HTMLInputElement).value).toBe('')
  })

  it('explains that the session expired', async () => {
    const { wrapper } = await mountWithApp(App, { path: '/login?reason=expired', locale: 'en' })

    expect(wrapper.text()).toContain('Your session expired. Sign in again.')
  })
})

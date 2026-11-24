import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import App from '@/App.vue'
import { authApi } from '@/api/auth'
import { mountWithApp } from '@/__tests__/support'
import { ok, sessionAs } from '@/__tests__/tokens'

vi.mock('@/api/auth', () => ({
  authApi: {
    login: vi.fn(),
    refresh: vi.fn(),
    logout: vi.fn(),
    me: vi.fn(),
    changePassword: vi.fn(),
  },
}))

const auth = vi.mocked(authApi)

async function fill(
  wrapper: Awaited<ReturnType<typeof mountWithApp>>['wrapper'],
  current: string,
  next: string,
  confirmation = next,
) {
  await wrapper.find('#current-password').setValue(current)
  await wrapper.find('#new-password').setValue(next)
  await wrapper.find('#confirm-password').setValue(confirmation)
  await wrapper.find('#current-password').element.closest('form')!.requestSubmit()
  await flushPromises()
}

describe('ProfileView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    auth.refresh.mockResolvedValue(sessionAs('member'))
  })

  it('shows who is signed in', async () => {
    const { wrapper } = await mountWithApp(App, { path: '/profile' })

    expect(wrapper.text()).toContain('ana@lexora.local')
    expect(wrapper.text()).toContain('Membro')
  })

  it('checks the new password against the same rules as the server', async () => {
    const { wrapper } = await mountWithApp(App, { path: '/profile' })

    await fill(wrapper, 'Atual2026Senha', 'fraca')

    expect(wrapper.text()).toContain('A nova senha não atende às regras abaixo.')
    expect(auth.changePassword).not.toHaveBeenCalled()
  })

  it('asks to repeat the same new password', async () => {
    const { wrapper } = await mountWithApp(App, { path: '/profile' })

    await fill(wrapper, 'Atual2026Senha', 'NovaSenha2026', 'NovaSenha2027')

    expect(wrapper.text()).toContain('As senhas não são iguais.')
    expect(auth.changePassword).not.toHaveBeenCalled()
  })

  it('shows why the server refused the change', async () => {
    auth.changePassword.mockResolvedValueOnce({
      success: false,
      data: null,
      notifications: [
        {
          key: 'auth.password.currentInvalid',
          message: 'The current password is incorrect.',
          type: 'validation',
        },
      ],
      status: 400,
    })
    const { wrapper } = await mountWithApp(App, { path: '/profile' })

    await fill(wrapper, 'Errada2026Senha', 'NovaSenha2026')

    expect(wrapper.find('[role="alert"]').text()).toBe('A senha atual está incorreta.')
  })

  it('signs out after the change because every session ends', async () => {
    auth.changePassword.mockResolvedValueOnce(ok(null))
    const { wrapper, router } = await mountWithApp(App, { path: '/profile' })

    await fill(wrapper, 'Atual2026Senha', 'NovaSenha2026')

    expect(auth.changePassword).toHaveBeenCalledWith({
      currentPassword: 'Atual2026Senha',
      newPassword: 'NovaSenha2026',
    })
    await vi.waitFor(() => expect(router.currentRoute.value.name).toBe('login'))
    expect(wrapper.text()).toContain('Senha alterada. Entre com a nova senha.')
  })
})

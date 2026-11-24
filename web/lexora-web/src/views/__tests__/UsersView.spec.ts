import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import App from '@/App.vue'
import { authApi } from '@/api/auth'
import { usersApi } from '@/api/users'
import type { User } from '@/api/types/user'
import { buttonByText, mountWithApp } from '@/__tests__/support'
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
vi.mock('@/api/users', () => ({
  usersApi: {
    list: vi.fn(),
    create: vi.fn(),
    changeRole: vi.fn(),
    deactivate: vi.fn(),
    activate: vi.fn(),
    resetPassword: vi.fn(),
  },
}))

const auth = vi.mocked(authApi)
const users = vi.mocked(usersApi)

const me: User = {
  id: 'user-1',
  email: 'ana@lexora.local',
  name: 'Ana',
  role: 'admin',
  status: 'active',
  lastLoginAt: '2026-11-20T09:00:00Z',
  createdAt: '2026-11-01T09:00:00Z',
  updatedAt: '2026-11-20T09:00:00Z',
}

const bruno: User = {
  ...me,
  id: 'user-2',
  email: 'bruno@lexora.local',
  name: 'Bruno',
  role: 'viewer',
  lastLoginAt: null,
}

function page(items: User[]) {
  return ok({ items, page: 1, pageSize: 20, totalItems: items.length, totalPages: 1 })
}

async function openUsers(path = '/admin/users') {
  return mountWithApp(App, { path })
}

function confirmDialog(label: string): void {
  buttonByText(document.querySelector('[role="alertdialog"], [role="dialog"]')!, label)!.click()
}

describe('UsersView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    auth.refresh.mockResolvedValue(sessionAs('admin'))
    users.list.mockResolvedValue(page([me, bruno]))
  })

  it('shows the users link only to admins', async () => {
    const admin = await mountWithApp(App, { path: '/' })
    expect(admin.wrapper.find('nav').text()).toContain('Usuários')

    auth.refresh.mockResolvedValue(sessionAs('member'))
    const member = await mountWithApp(App, { path: '/' })
    expect(member.wrapper.find('nav').text()).not.toContain('Usuários')
  })

  it('lists the users with the filters of the address', async () => {
    const { wrapper } = await openUsers('/admin/users?role=viewer&status=active')

    expect(users.list).toHaveBeenCalledWith(
      { search: '', role: 'viewer', status: 'active', page: 1, pageSize: 20 },
      expect.any(AbortSignal),
    )
    expect(wrapper.text()).toContain('bruno@lexora.local')
    expect(wrapper.text()).toContain('Nunca entrou')
  })

  it('keeps the admin from locking out their own account', async () => {
    const { wrapper } = await openUsers()

    expect(wrapper.find('button[aria-label="Desativar Ana"]').attributes('disabled')).toBeDefined()
    expect(
      wrapper.find('button[aria-label="Desativar Bruno"]').attributes('disabled'),
    ).toBeUndefined()
  })

  it('creates a user only with a strong password', async () => {
    users.create.mockResolvedValueOnce(
      ok({ ...bruno, id: 'user-3', name: 'Carla', email: 'carla@lexora.local' }, 201),
    )
    const { wrapper } = await openUsers()
    buttonByText(wrapper.element, 'Novo usuário')!.click()
    await flushPromises()
    const name = document.querySelector<HTMLInputElement>('#new-user-name')!
    const email = document.querySelector<HTMLInputElement>('#new-user-email')!
    const password = document.querySelector<HTMLInputElement>('#new-user-password')!
    name.value = 'Carla'
    name.dispatchEvent(new Event('input'))
    email.value = 'carla@lexora.local'
    email.dispatchEvent(new Event('input'))
    password.value = 'fraca'
    password.dispatchEvent(new Event('input'))
    name.closest('form')!.requestSubmit()
    await flushPromises()
    expect(document.body.textContent).toContain('A senha não atende às regras abaixo.')
    expect(users.create).not.toHaveBeenCalled()

    password.value = 'SenhaForte2026'
    password.dispatchEvent(new Event('input'))
    name.closest('form')!.requestSubmit()
    await flushPromises()

    expect(users.create).toHaveBeenCalledWith({
      email: 'carla@lexora.local',
      name: 'Carla',
      role: 'viewer',
      password: 'SenhaForte2026',
    })
    expect(wrapper.text()).toContain('carla@lexora.local')
  })

  it('shows why the server refused a new user', async () => {
    users.create.mockResolvedValueOnce({
      success: false,
      data: null,
      notifications: [{ key: 'user.email.duplicate', message: 'Duplicate.', type: 'conflict' }],
      status: 409,
    })
    const { wrapper } = await openUsers()
    buttonByText(wrapper.element, 'Novo usuário')!.click()
    await flushPromises()
    for (const [id, value] of [
      ['#new-user-name', 'Bruno'],
      ['#new-user-email', 'bruno@lexora.local'],
      ['#new-user-password', 'SenhaForte2026'],
    ] as const) {
      const input = document.querySelector<HTMLInputElement>(id)!
      input.value = value
      input.dispatchEvent(new Event('input'))
    }

    document.querySelector<HTMLInputElement>('#new-user-name')!.closest('form')!.requestSubmit()
    await flushPromises()

    expect(document.querySelector('[role="alert"]')?.textContent).toBe(
      'Já existe um usuário com esse e-mail.',
    )

    buttonByText(document.querySelector('[role="dialog"]')!, 'Cancelar')!.click()
    await flushPromises()
    buttonByText(wrapper.element, 'Novo usuário')!.click()
    await flushPromises()

    expect(document.querySelector('[role="alert"]')).toBeNull()
  })

  it('keeps the role of a user when the change is cancelled', async () => {
    const { wrapper } = await openUsers()
    const role = () => wrapper.find('[aria-label="Papel de Bruno"]')

    await role().trigger('click')
    await flushPromises()
    const options = document.getElementById(role().attributes('aria-controls')!)!
    options
      .querySelector('[role="option"][aria-label="Membro"]')!
      .dispatchEvent(new MouseEvent('mousedown', { bubbles: true }))
    await flushPromises()
    confirmDialog('Cancelar')
    await flushPromises()

    expect(users.changeRole).not.toHaveBeenCalled()
    expect(role().text()).toBe('Leitor')
  })

  it('deactivates a user after confirmation', async () => {
    users.deactivate.mockResolvedValueOnce(ok({ ...bruno, status: 'inactive' }))
    const { wrapper } = await openUsers()

    await wrapper.find('button[aria-label="Desativar Bruno"]').trigger('click')
    await flushPromises()
    confirmDialog('Desativar')
    await flushPromises()

    expect(users.deactivate).toHaveBeenCalledWith('user-2')
    expect(wrapper.text()).toContain('Inativo')
  })

  it('resets the password of a user', async () => {
    users.resetPassword.mockResolvedValueOnce(ok(null))
    const { wrapper } = await openUsers()

    await wrapper.find('button[aria-label="Redefinir a senha de Bruno"]').trigger('click')
    await flushPromises()
    const input = document.querySelector<HTMLInputElement>('#reset-password')!
    input.value = 'NovaSenha2026'
    input.dispatchEvent(new Event('input'))
    input.closest('form')!.requestSubmit()
    await flushPromises()

    expect(users.resetPassword).toHaveBeenCalledWith('user-2', 'NovaSenha2026')
  })
})

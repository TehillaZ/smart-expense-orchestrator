import { API_BASE_URL } from '../config/api'
import { requestJson } from './apiClient'
import { isUserRole, type UserRole } from '../types/userRole'

const AUTH_STORAGE_KEY = 'approvalflow.auth'

export type AuthSession = {
  token: string
  role: UserRole
  username: string
  expiresAt: string
}

type LoginResponse = {
  token: string
  role: string
  username: string
  expiresAt: string
}

function readStoredSession(): AuthSession | null {
  const raw = sessionStorage.getItem(AUTH_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as AuthSession
    if (!parsed.token || !isUserRole(parsed.role)) {
      return null
    }

    if (new Date(parsed.expiresAt).getTime() <= Date.now()) {
      sessionStorage.removeItem(AUTH_STORAGE_KEY)
      return null
    }

    return parsed
  } catch {
    sessionStorage.removeItem(AUTH_STORAGE_KEY)
    return null
  }
}

export function getAuthSession(): AuthSession | null {
  return readStoredSession()
}

export function getAuthToken(): string | null {
  return readStoredSession()?.token ?? null
}

export function clearAuthSession(): void {
  sessionStorage.removeItem(AUTH_STORAGE_KEY)
}

export async function loginWithRole(role: UserRole): Promise<AuthSession> {
  const response = await requestJson<LoginResponse>(`${API_BASE_URL}/auth/login`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ role }),
  })

  if (!isUserRole(response.role)) {
    throw new Error('Login returned an unsupported role.')
  }

  const session: AuthSession = {
    token: response.token,
    role: response.role,
    username: response.username,
    expiresAt: response.expiresAt,
  }

  sessionStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
  return session
}

export function isAuthenticated(): boolean {
  return getAuthSession() !== null
}

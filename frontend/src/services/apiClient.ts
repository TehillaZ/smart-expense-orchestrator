import type { ApiErrorBody } from '../types/api'

import { getAuthToken } from './authService'

export class ApiRequestError extends Error {
  readonly status: number
  readonly details?: string[]

  constructor(message: string, status: number, details?: string[]) {
    super(message)
    this.name = 'ApiRequestError'
    this.status = status
    this.details = details
  }
}

export async function parseJsonResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    if (response.status === 204) {
      return undefined as T
    }

    return (await response.json()) as T
  }

  let details: string[] | undefined

  try {
    const body = (await response.json()) as ApiErrorBody
    if (body.errors?.length) {
      details = body.errors
    } else if (body.message) {
      details = [body.message]
    }
  } catch {
    details = undefined
  }

  throw new ApiRequestError(
    details?.[0] ?? `Request failed with status ${response.status}.`,
    response.status,
    details,
  )
}

export async function requestJson<T>(url: string, init?: RequestInit): Promise<T> {
  try {
    const authToken = getAuthToken()
    const response = await fetch(url, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...(authToken ? { Authorization: `Bearer ${authToken}` } : {}),
        ...init?.headers,
      },
    })

    return await parseJsonResponse<T>(response)
  } catch (error) {
    if (error instanceof ApiRequestError) {
      throw error
    }

    throw new ApiRequestError(
      'Unable to reach the service. Check that the backend is running.',
      0,
    )
  }
}

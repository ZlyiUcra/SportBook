const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5217/api'

export type ApiError = {
  code: string
  message: string
}

export class ApiRequestError extends Error {
  status: number
  error: ApiError

  constructor(status: number, error: ApiError) {
    super(error.message)
    this.status = status
    this.error = error
  }
}

let accessToken: string | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export async function apiFetch<TResponse>(
  path: string,
  init?: RequestInit,
): Promise<TResponse> {
  const headers = new Headers(init?.headers)
  headers.set('Content-Type', 'application/json')
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers })

  if (response.status === 204) {
    return undefined as TResponse
  }

  const body = await response.json()

  if (!response.ok) {
    throw new ApiRequestError(response.status, body.error as ApiError)
  }

  return body as TResponse
}

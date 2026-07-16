import axios from 'axios'
import { env } from '../config/env'

export const axiosInstance = axios.create({ baseURL: env.apiBaseUrl })

let authToken: string | null = null

/** Called by entities/session whenever the session changes - keeps shared free of upper-layer imports. */
export function setAuthToken(token: string | null) {
  authToken = token
}

axiosInstance.interceptors.request.use((config) => {
  if (authToken) {
    config.headers.Authorization = `Bearer ${authToken}`
  }
  return config
})

export type ApiErrorPayload = {
  code: string
  message: string
}

export class ApiRequestError extends Error {
  status: number
  code: string

  constructor(status: number, payload: ApiErrorPayload) {
    super(payload.message)
    this.status = status
    this.code = payload.code
  }
}

axiosInstance.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response) {
      const payload = (error.response.data as { error?: ApiErrorPayload } | undefined)?.error
      if (payload) {
        return Promise.reject(new ApiRequestError(error.response.status, payload))
      }
    }
    return Promise.reject(error)
  },
)

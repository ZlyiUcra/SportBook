import { apiFetch } from './client'

export type UserResponse = {
  id: string
  name: string
  email: string
  role: string
  subscriptionTier: string
  createdAt: string
}

export type AuthResponse = {
  accessToken: string
  refreshToken: string
  user: UserResponse
}

export type RegisterRequest = {
  name: string
  email: string
  password: string
}

export type LoginRequest = {
  email: string
  password: string
}

export function register(request: RegisterRequest) {
  return apiFetch<AuthResponse>('/auth/register', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export function login(request: LoginRequest) {
  return apiFetch<AuthResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export function getMe() {
  return apiFetch<UserResponse>('/users/me')
}

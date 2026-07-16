import { axiosInstance } from '@/shared/api/axiosInstance'
import type { User } from '@/entities/user/model/types'
import type { RegisterFormValues } from '../model/schema'

export type AuthResponse = {
  accessToken: string
  refreshToken: string
  user: User
}

export async function register(values: RegisterFormValues): Promise<AuthResponse> {
  const { data } = await axiosInstance.post<AuthResponse>('/auth/register', values)
  return data
}

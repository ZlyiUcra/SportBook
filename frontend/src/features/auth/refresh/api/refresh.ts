import { axiosInstance } from '@/shared/api/axiosInstance'
import type { User } from '@/entities/user/model/types'

export type RefreshResponse = {
  accessToken: string
  refreshToken: string
  user: User
}

export async function refresh(refreshToken: string): Promise<RefreshResponse> {
  const { data } = await axiosInstance.post<RefreshResponse>('/auth/refresh', { refreshToken })
  return data
}

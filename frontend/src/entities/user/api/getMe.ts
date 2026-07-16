import { axiosInstance } from '@/shared/api/axiosInstance'
import type { User } from '../model/types'

export async function getMe(): Promise<User> {
  const { data } = await axiosInstance.get<User>('/users/me')
  return data
}

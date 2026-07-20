import { axiosInstance } from '@/shared/api/axiosInstance'

export async function logout(refreshToken: string): Promise<void> {
  await axiosInstance.post('/auth/logout', { refreshToken })
}

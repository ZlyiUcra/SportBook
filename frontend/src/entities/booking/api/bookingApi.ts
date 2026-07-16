import { axiosInstance } from '@/shared/api/axiosInstance'
import type { PagedResponse } from '@/shared/api/types'
import type { Availability, Booking } from '../model/types'

export async function getAvailability(courtId: string, date: string): Promise<Availability> {
  const { data } = await axiosInstance.get<Availability>(`/courts/${courtId}/availability`, {
    params: { date },
  })
  return data
}

export async function listMyBookings(page = 1): Promise<PagedResponse<Booking>> {
  const { data } = await axiosInstance.get<PagedResponse<Booking>>('/bookings', { params: { page } })
  return data
}

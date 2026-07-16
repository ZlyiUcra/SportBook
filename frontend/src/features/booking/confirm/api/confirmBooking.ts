import { axiosInstance } from '@/shared/api/axiosInstance'
import type { Booking } from '@/entities/booking/model/types'

export async function confirmBooking(bookingId: string): Promise<Booking> {
  const { data } = await axiosInstance.put<Booking>(`/bookings/${bookingId}/confirm`)
  return data
}

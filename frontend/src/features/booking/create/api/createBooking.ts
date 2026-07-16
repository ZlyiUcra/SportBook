import { axiosInstance } from '@/shared/api/axiosInstance'
import type { Booking } from '@/entities/booking/model/types'

export type CreateBookingInput = {
  courtId: string
  startTime: string
  endTime: string
}

/** No price or user fields - both are server-derived (contracts/api.md). */
export async function createBooking(input: CreateBookingInput): Promise<Booking> {
  const { data } = await axiosInstance.post<Booking>('/bookings', input)
  return data
}

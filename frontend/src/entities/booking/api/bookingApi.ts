import { axiosInstance } from '@/shared/api/axiosInstance'
import type { PagedResponse } from '@/shared/api/types'
import type { Availability, Booking } from '../model/types'

export async function getAvailability(courtId: string, date: string): Promise<Availability> {
  const { data } = await axiosInstance.get<Availability>(`/courts/${courtId}/availability`, {
    params: { date },
  })
  return data
}

/** The status groups a customer can filter their bookings by (005) - mirrors the backend BookingStatusFilter. */
export const bookingStatusFilters = ['All', 'Upcoming', 'Completed', 'Cancelled'] as const

export type BookingStatusFilter = (typeof bookingStatusFilters)[number]

export async function listMyBookings(status: BookingStatusFilter = 'All', page = 1): Promise<PagedResponse<Booking>> {
  const { data } = await axiosInstance.get<PagedResponse<Booking>>('/bookings', {
    params: { status: status === 'All' ? undefined : status, page },
  })
  return data
}

/** Owner-only: bookings for one of the caller's own venues (backend enforces ownership). */
export async function listBookingsByVenue(venueId: string, page = 1): Promise<PagedResponse<Booking>> {
  const { data } = await axiosInstance.get<PagedResponse<Booking>>(`/venues/${venueId}/bookings`, {
    params: { page },
  })
  return data
}

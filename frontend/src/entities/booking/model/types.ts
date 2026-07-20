import type { City } from '@/entities/city/model/types'
import type { SportType } from '@/entities/venue/model/types'

export type BookingStatus = 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed'

/**
 * Mirrors the backend BookingResponse. Since 005 it carries the human-readable venue/city/sport/
 * court labels (venueName, city, sport, courtName) so a booking is legible without a second lookup
 * - the raw `courtId` remains for actions/links but is never shown to the customer. Since 006 it
 * also carries `venueId`, used to target the review entry for a completed booking's venue.
 */
export type Booking = {
  id: string
  courtId: string
  userId: string
  startTime: string
  endTime: string
  status: BookingStatus
  totalPrice: number
  createdAt: string
  venueName: string
  city: City
  sport: SportType
  courtName: string
  venueId: string
}

export type FreeSlot = {
  start: string
  end: string
}

export type Availability = {
  courtId: string
  date: string
  freeSlots: FreeSlot[]
}

import type { City } from '@/entities/city/model/types'

/** Fixed sport vocabulary mirroring the backend SportType enum (exact-match filtering). */
export const sportTypes = [
  'Tennis',
  'Football',
  'Basketball',
  'Padel',
  'Badminton',
  'Volleyball',
  'Other',
] as const

export type SportType = (typeof sportTypes)[number]

/** `latitude`/`longitude` are null when the owner has not set a pin (spec FR-009/FR-010) - never substitute the city's coordinates. */
export type VenueSummary = {
  id: string
  name: string
  city: City
  address: string
  description: string | null
  latitude: number | null
  longitude: number | null
}

export type Court = {
  id: string
  venueId: string
  name: string
  sportType: SportType
  pricePerHour: number
  openingTime: string
  closingTime: string
  isActive: boolean
}

/** `latitude`/`longitude` are null when the owner has not set a pin - no map block, no city-centre fallback (spec FR-010). */
export type VenueDetail = {
  id: string
  name: string
  city: City
  address: string
  description: string | null
  latitude: number | null
  longitude: number | null
  ownerId: string
  courts: Court[]
  averageRating: number | null
  reviewCount: number
}

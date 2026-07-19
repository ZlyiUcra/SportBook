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

/**
 * Item shape for `GET /api/venues/nearby` (003 data-model.md) - the venue summary fields plus the
 * server-computed distance from the reference point. `latitude`/`longitude` are non-null here by
 * construction: only coordinate-bearing venues are ever returned.
 */
export type NearbyVenue = {
  id: string
  name: string
  city: City
  address: string
  description: string | null
  latitude: number
  longitude: number
  distanceKm: number
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

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

export type VenueSummary = {
  id: string
  name: string
  city: string
  address: string
  description: string | null
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

export type VenueDetail = {
  id: string
  name: string
  city: string
  address: string
  description: string | null
  ownerId: string
  courts: Court[]
  averageRating: number | null
  reviewCount: number
}

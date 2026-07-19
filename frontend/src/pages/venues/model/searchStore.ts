import { create } from 'zustand'
import type { City } from '@/entities/city/model/types'
import type { SportType } from '@/entities/venue/model/types'

export type DeviceCoords = { lat: number; lng: number }

type SearchState = {
  city: City | null
  sportType: SportType | ''
  /** Rounded (2-decimal) device location captured by an explicit "near me" grant - never persisted anywhere. */
  deviceCoords: DeviceCoords | null
  setCity: (city: City | null) => void
  setSportType: (sportType: SportType | '') => void
  setDeviceCoords: (coords: DeviceCoords) => void
}

/**
 * Session-scoped memory of the venue search (004 data-model.md "Search state") - lifted out of
 * VenueSearchPage component state so returning from a venue page restores the search instead of
 * starting over (spec US1). Deliberately in-memory only, NO `persist` middleware: device
 * coordinates must never reach localStorage/sessionStorage/cookies/URL (contract MUST, spec
 * FR-006) - a page reload therefore also starts fresh, which is stricter than required and
 * privacy-safer.
 */
export const useSearchStore = create<SearchState>((set) => ({
  city: null,
  sportType: '',
  deviceCoords: null,
  setCity: (city) => set({ city }),
  setSportType: (sportType) => set({ sportType }),
  setDeviceCoords: (deviceCoords) => set({ deviceCoords }),
}))

import { create } from 'zustand'
import type { City } from '@/entities/city/model/types'
import type { SportType } from '@/entities/venue/model/types'

export type DeviceCoords = { lat: number; lng: number }

/**
 * The restorable map camera (008 spec FR-001 "zoom level and pan position"). Survives a
 * venue-detail detour so a return remount mounts the map back at this center/zoom; cleared on a
 * reference-point change (page-driven, 008 FR-002). In-memory only, like the rest of the store -
 * never persisted (004 contract MUST, 008 FR-005).
 */
export type SearchViewport = { lat: number; lng: number; zoom: number }

type SearchState = {
  city: City | null
  sportType: SportType | ''
  /** Rounded (2-decimal) device location captured by an explicit "near me" grant - never persisted anywhere. */
  deviceCoords: DeviceCoords | null
  /** Saved map camera to restore across venue-detail navigation (008); null until the first viewport report. */
  viewport: SearchViewport | null
  setCity: (city: City | null) => void
  setSportType: (sportType: SportType | '') => void
  setDeviceCoords: (coords: DeviceCoords) => void
  setViewport: (viewport: SearchViewport | null) => void
}

/**
 * Session-scoped memory of the venue search (004 data-model.md "Search state") - lifted out of
 * VenueSearchPage component state so returning from a venue page restores the search instead of
 * starting over (spec US1). Deliberately in-memory only, NO `persist` middleware: device
 * coordinates must never reach localStorage/sessionStorage/cookies/URL (contract MUST, spec
 * FR-006) - a page reload therefore also starts fresh, which is stricter than required and
 * privacy-safer. Since 008, also holds the restorable map camera (`viewport`).
 */
export const useSearchStore = create<SearchState>((set) => ({
  city: null,
  sportType: '',
  deviceCoords: null,
  viewport: null,
  setCity: (city) => set({ city }),
  setSportType: (sportType) => set({ sportType }),
  setDeviceCoords: (deviceCoords) => set({ deviceCoords }),
  setViewport: (viewport) => set({ viewport }),
}))

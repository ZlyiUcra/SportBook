import { create } from 'zustand'
import type { City } from '@/entities/city/model/types'
import type { SportType } from '@/entities/venue/model/types'

export type DeviceCoords = { lat: number; lng: number }

/**
 * The restorable map camera (008 spec FR-001 "zoom level and pan position"). Survives a
 * venue-detail detour so a return remount mounts the map back at this center/zoom; cleared on a
 * reference-point change (page-driven, 008 FR-002). Persisted (see below), unlike when this type
 * was first introduced.
 */
export type SearchViewport = { lat: number; lng: number; zoom: number }

const STORAGE_KEY = 'sportbook-venue-search'

type PersistedSearch = {
  city: City | null
  sportType: SportType | ''
  viewport: SearchViewport | null
  page: number
}

function readPersistedSearch(): PersistedSearch | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as PersistedSearch
  } catch {
    return null
  }
}

function writePersistedSearch(value: PersistedSearch) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(value))
}

type SearchState = {
  city: City | null
  sportType: SportType | ''
  /**
   * Rounded (2-decimal) device location captured by an explicit "near me" grant - the one field
   * in this store still kept strictly in-memory, never persisted. This is the specific concern
   * the store's original in-memory-only design (004 contract MUST, 008 FR-005) was protecting:
   * raw device coordinates must never reach `localStorage`/`sessionStorage`/cookies/URL. The
   * other fields below no longer carry that same sensitivity (a selected city, a map viewport
   * derived from it, a sport filter, a list page number) and are now persisted so a page reload
   * restores the search instead of showing an empty "pick a reference point" prompt.
   */
  deviceCoords: DeviceCoords | null
  viewport: SearchViewport | null
  page: number
  setCity: (city: City | null) => void
  setSportType: (sportType: SportType | '') => void
  setDeviceCoords: (coords: DeviceCoords) => void
  setViewport: (viewport: SearchViewport | null) => void
  setPage: (page: number) => void
}

const persisted = readPersistedSearch()

/**
 * Session-scoped memory of the venue search (004 data-model.md "Search state") - lifted out of
 * VenueSearchPage component state so returning from a venue page restores the search instead of
 * starting over (spec US1). City, sport filter, map viewport, and list page are persisted to
 * `localStorage`; `deviceCoords` (raw "near me" GPS) is deliberately excluded (see its own doc
 * comment above).
 */
export const useSearchStore = create<SearchState>((set, get) => {
  function persistCurrent() {
    const { city, sportType, viewport, page } = get()
    writePersistedSearch({ city, sportType, viewport, page })
  }

  return {
    city: persisted?.city ?? null,
    sportType: persisted?.sportType ?? '',
    deviceCoords: null,
    viewport: persisted?.viewport ?? null,
    page: persisted?.page ?? 1,
    setCity: (city) => {
      set({ city })
      persistCurrent()
    },
    setSportType: (sportType) => {
      set({ sportType })
      persistCurrent()
    },
    setDeviceCoords: (deviceCoords) => set({ deviceCoords }),
    setViewport: (viewport) => {
      set({ viewport })
      persistCurrent()
    },
    setPage: (page) => {
      set({ page })
      persistCurrent()
    },
  }
})

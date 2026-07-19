import type { City } from '@/entities/city/model/types'
import { useGeolocation, type GeolocationStatus } from './useGeolocation'

export type ReferencePoint = { lat: number; lng: number }

export type ReferenceSource = 'device' | 'city' | 'none'

type UseReferencePointResult = {
  referencePoint: ReferencePoint | null
  source: ReferenceSource
  geolocationStatus: GeolocationStatus
  /** Triggers the device-location lookup - wire to an explicit "near me" click (no silent prompt). */
  requestDeviceLocation: () => void
}

/**
 * Single source of truth for the 75km search's center (003 research.md "Reference-point
 * resolution and geolocation") - resolves by precedence: granted device location, then the
 * explicitly selected directory city, then none. The map and the nearby query must read this one
 * value so they never diverge (data-model.md "Reference point").
 */
export function useReferencePoint(city: City | null): UseReferencePointResult {
  const { status, coords, request } = useGeolocation()

  if (status === 'granted' && coords) {
    return { referencePoint: coords, source: 'device', geolocationStatus: status, requestDeviceLocation: request }
  }

  if (city) {
    return {
      referencePoint: { lat: city.latitude, lng: city.longitude },
      source: 'city',
      geolocationStatus: status,
      requestDeviceLocation: request,
    }
  }

  return { referencePoint: null, source: 'none', geolocationStatus: status, requestDeviceLocation: request }
}

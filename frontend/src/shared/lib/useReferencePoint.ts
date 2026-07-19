import React from 'react'
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
 * explicitly selected directory city, then none. Since 004, the persisted inputs (`city`,
 * `deviceCoords`) come from the caller's session store rather than hook-local state, so a
 * remount (returning from a venue page) restores the reference WITHOUT any Geolocation API call
 * (004 spec FR-003). They arrive as parameters - not read from the store here - because
 * shared/lib must not import a pages-layer store (FSD layering); `onDeviceCoords` is how a fresh
 * "near me" grant flows back into that store.
 */
export function useReferencePoint(
  city: City | null,
  deviceCoords: ReferencePoint | null,
  onDeviceCoords: (coords: ReferencePoint) => void,
): UseReferencePointResult {
  const { status, coords, request } = useGeolocation()

  // Ref so a new callback identity on parent re-render never re-runs the effect.
  const onDeviceCoordsRef = React.useRef(onDeviceCoords)
  onDeviceCoordsRef.current = onDeviceCoords

  React.useEffect(() => {
    if (status === 'granted' && coords) {
      onDeviceCoordsRef.current(coords)
    }
  }, [status, coords])

  if (deviceCoords) {
    return { referencePoint: deviceCoords, source: 'device', geolocationStatus: status, requestDeviceLocation: request }
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

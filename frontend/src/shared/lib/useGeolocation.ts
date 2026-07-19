import React from 'react'

export type GeolocationStatus = 'idle' | 'locating' | 'granted' | 'denied' | 'error'

export type GeolocationCoords = { lat: number; lng: number }

type UseGeolocationResult = {
  status: GeolocationStatus
  coords: GeolocationCoords | null
  /** Triggers `getCurrentPosition` - call only from an explicit user action (no silent prompt on load, contracts/api.md Frontend contract MUSTs). */
  request: () => void
}

/**
 * Owns the browser Geolocation API call, 2-decimal coordinate rounding (~1.1km, research.md
 * Geolocation privacy posture), and permission/denied/error state - extracted from the inline
 * logic in MyCityButton (003 research.md "Reference-point resolution and geolocation") so the
 * "near me" action and any future geolocation consumer share one state machine instead of each
 * re-implementing it.
 */
export function useGeolocation(): UseGeolocationResult {
  const [status, setStatus] = React.useState<GeolocationStatus>('idle')
  const [coords, setCoords] = React.useState<GeolocationCoords | null>(null)

  function request() {
    if (!('geolocation' in navigator)) {
      setStatus('error')
      return
    }

    setStatus('locating')
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setCoords({
          lat: Math.round(position.coords.latitude * 100) / 100,
          lng: Math.round(position.coords.longitude * 100) / 100,
        })
        setStatus('granted')
      },
      (error) => {
        setStatus(error.code === error.PERMISSION_DENIED ? 'denied' : 'error')
      },
    )
  }

  return { status, coords, request }
}

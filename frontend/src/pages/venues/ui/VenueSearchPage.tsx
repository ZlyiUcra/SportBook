import React from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { Button } from '@/shared/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { getNearbyVenues } from '@/entities/venue/api/venueApi'
import { sportTypes, type SportType } from '@/entities/venue/model/types'
import { CityCombobox } from '@/features/city-select/ui/CityCombobox'
import { NearMeButton } from '@/features/city-select/ui/NearMeButton'
import { cityName } from '@/entities/city/model/types'
import { useReferencePoint } from '@/shared/lib/useReferencePoint'
import { useSearchStore } from '../model/searchStore'

// Lazy so leaflet/react-leaflet/clustering never land in the initial route chunk (spec SC-006) -
// this import only fires once a reference point exists and the map section actually renders.
// The type-only import below is erased at build time and does not defeat the laziness.
const MapView = React.lazy(() => import('@/shared/ui/map/MapView'))
import type { MapBounds, MapViewport } from '@/shared/ui/map/MapView'
import type { NearbyVenue } from '@/entities/venue/model/types'

/** Client-side list page size (004 spec FR-012) - a single constant, raise here if ever needed. */
const searchPageSize = 10

/** A venue is in view when its point lies within the reported bounds (004 research.md "Visibility test"). */
function isInBounds(venue: NearbyVenue, bounds: MapBounds): boolean {
  return (
    venue.latitude >= bounds.south &&
    venue.latitude <= bounds.north &&
    venue.longitude >= bounds.west &&
    venue.longitude <= bounds.east
  )
}

/**
 * Reference-point radius view (003 spec US1-US3): the map and the list both read the same
 * in-range set from `GET /api/venues/nearby`, centered on the resolved reference point (device
 * location via "near me", else the selected city, else none - `useReferencePoint`). Supersedes
 * 002's page-based `VenueSearchMap` and `includeNearby` toggle (research.md "What this
 * supersedes on the search page"). Since 004, the search inputs live in the session-scoped
 * `useSearchStore` instead of component state, so navigating to a venue page and back restores
 * the search - reference point, sport filter, results - with no geolocation prompt (004 US1).
 */
export function VenueSearchPage() {
  const { t, i18n } = useTranslation()
  const city = useSearchStore((state) => state.city)
  const setCity = useSearchStore((state) => state.setCity)
  const sportType = useSearchStore((state) => state.sportType)
  const setSportType = useSearchStore((state) => state.setSportType)
  const deviceCoords = useSearchStore((state) => state.deviceCoords)
  const setDeviceCoords = useSearchStore((state) => state.setDeviceCoords)
  const viewport = useSearchStore((state) => state.viewport)
  const setViewport = useSearchStore((state) => state.setViewport)

  const { referencePoint, geolocationStatus, requestDeviceLocation } = useReferencePoint(city, deviceCoords, setDeviceCoords)

  const nearbyQuery = useQuery({
    queryKey: ['venues-nearby', referencePoint, sportType],
    queryFn: () => getNearbyVenues(referencePoint!.lat, referencePoint!.lng, sportType || undefined),
    enabled: referencePoint !== null,
    // Keep the previous result set while a sport-filter refetch is in flight (008), so the map
    // (mounted only while venues.length > 0) does not unmount/remount and lose the restored
    // viewport mid-transition - the same fix as MyBookingsPage's status filter.
    placeholderData: keepPreviousData,
  })

  const venues = nearbyQuery.data ?? []
  const nearestVenueId = venues[0]?.id

  // Latest completed-gesture viewport BOUNDS for the list/count filter (004 US2). Ephemeral page
  // state - repopulated on mount by the map's viewport report; NOT the restorable camera (that is
  // `viewport` in the store, which holds center+zoom, 008).
  const [viewportBounds, setViewportBounds] = React.useState<MapBounds | null>(null)
  // Reference coordinate key, stable across renders (the city-derived reference is rebuilt each
  // render, so the string key is what the effects compare against).
  const referenceKey = referencePoint ? `${referencePoint.lat},${referencePoint.lng}` : ''
  // Tell a genuine reference CHANGE from a mount / return-remount: only a real change (a new city
  // or a fresh "near me") drops the saved camera and the list bounds; a return remount must
  // preserve both (008 FR-001). On the first mount the ref is still null, so nothing is cleared.
  // A sport-filter change keeps the same reference, so the camera survives it (008 FR-002).
  const prevReferenceKeyRef = React.useRef<string | null>(null)
  React.useEffect(() => {
    if (prevReferenceKeyRef.current !== null && prevReferenceKeyRef.current !== referenceKey) {
      setViewportBounds(null)
      setViewport(null)
    }
    prevReferenceKeyRef.current = referenceKey
  }, [referenceKey, setViewport])
  // One report feeds both consumers (008 research.md): bounds -> list/count filter, center+zoom ->
  // the store's restorable camera. Stable identity (setViewport is stable) so it never re-subscribes
  // the map events.
  const handleViewportChange = React.useCallback(
    (report: MapViewport) => {
      setViewportBounds(report.bounds)
      setViewport({ lat: report.center.lat, lng: report.center.lng, zoom: report.zoom })
    },
    [setViewport],
  )

  // The list shows the viewport-visible subset (004 spec FR-007, supersedes 003 FR-013); the map
  // keeps rendering the FULL in-range set and emphasis stays the overall nearest (FR-011, FR-014).
  const visibleVenues = viewportBounds ? venues.filter((venue) => isInBounds(venue, viewportBounds)) : venues

  // List pagination (004 US3) - slices ONLY the list, never the map markers (spec FR-014). Any
  // change of the visible set (viewport, sport filter, reference) resets to page 1 (spec FR-013).
  const [page, setPage] = React.useState(1)
  React.useEffect(() => {
    setPage(1)
  }, [viewportBounds, sportType, referenceKey])
  const totalPages = Math.max(1, Math.ceil(visibleVenues.length / searchPageSize))
  const pagedVenues = visibleVenues.slice((page - 1) * searchPageSize, page * searchPageSize)

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">{t('venues.title')}</h1>

      <div className="flex flex-wrap gap-2">
        <CityCombobox
          value={city}
          onChange={setCity}
          onClear={() => setCity(null)}
          placeholder={t('venues.cityPlaceholder')}
        />
        <NearMeButton status={geolocationStatus} onActivate={requestDeviceLocation} />
        <select
          value={sportType}
          onChange={(e) => setSportType(e.target.value as SportType | '')}
          aria-label={t('venues.sportLabel')}
          className="rounded-md border border-input bg-background px-2 py-1 text-sm"
        >
          <option value="">{t('venues.allSports')}</option>
          {sportTypes.map((sport) => (
            <option key={sport} value={sport}>
              {t(`sport.${sport}`)}
            </option>
          ))}
        </select>
      </div>

      {!referencePoint && <p className="text-muted-foreground">{t('venues.pickReferencePrompt')}</p>}

      {referencePoint && nearbyQuery.isLoading && <p className="text-muted-foreground">{t('common.loading')}</p>}
      {referencePoint && nearbyQuery.isError && <p className="text-destructive">{t('common.requestFailed')}</p>}

      {referencePoint && nearbyQuery.data && venues.length === 0 && (
        <p className="text-muted-foreground">{t('venues.noResults')}</p>
      )}

      {referencePoint && venues.length > 0 && visibleVenues.length === 0 && (
        <p className="text-muted-foreground">{t('venues.noneInView')}</p>
      )}

      {referencePoint && venues.length > 0 && (
        <React.Suspense fallback={<p className="text-sm text-muted-foreground">{t('common.loading')}</p>}>
          <MapView
            className="h-80 w-full rounded-md"
            center={viewport ? { lat: viewport.lat, lng: viewport.lng } : referencePoint}
            zoom={viewport?.zoom}
            cluster
            fitBoundsKey={viewport ? undefined : referenceKey}
            onViewportChange={handleViewportChange}
            markers={venues.map((venue) => ({
              id: venue.id,
              position: { lat: venue.latitude, lng: venue.longitude },
              emphasized: venue.id === nearestVenueId,
              // JSX children only - never bindPopup/setContent with strings (research.md Map
              // content safety); venue.name is unvalidated user input.
              popup: (
                <Link to={`/venues/${venue.id}`} className="font-medium underline">
                  {venue.name}
                </Link>
              ),
            }))}
          />
        </React.Suspense>
      )}

      {referencePoint && venues.length > 0 && (
        <p className="text-sm text-muted-foreground">
          {t('venues.visibleCount', { count: visibleVenues.length })}
        </p>
      )}

      <div className="grid grid-cols-1 gap-2 min-[420px]:grid-cols-2">
        {pagedVenues.map((venue) => (
          <Link key={venue.id} to={`/venues/${venue.id}`}>
            <Card className="[--card-spacing:--spacing(2)] transition-colors hover:bg-accent/50">
              <CardHeader>
                <CardTitle>{venue.name}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">
                  {cityName(venue.city, i18n.language)}, {venue.address} - {venue.distanceKm.toFixed(1)} km
                </p>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      {visibleVenues.length > searchPageSize && (
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
            {t('common.prev')}
          </Button>
          <span className="text-sm text-muted-foreground">
            {page} / {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage(page + 1)}
          >
            {t('common.next')}
          </Button>
        </div>
      )}
    </div>
  )
}

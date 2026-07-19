import React from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
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
import type { MapBounds } from '@/shared/ui/map/MapView'
import type { NearbyVenue } from '@/entities/venue/model/types'

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

  const { referencePoint, geolocationStatus, requestDeviceLocation } = useReferencePoint(city, deviceCoords, setDeviceCoords)

  const nearbyQuery = useQuery({
    queryKey: ['venues-nearby', referencePoint, sportType],
    queryFn: () => getNearbyVenues(referencePoint!.lat, referencePoint!.lng, sportType || undefined),
    enabled: referencePoint !== null,
  })

  const venues = nearbyQuery.data ?? []
  const nearestVenueId = venues[0]?.id
  // Keyed on the reference point + the returned venue-id set (research.md fitBounds behaviour) -
  // fits once per change of either, not on every render.
  const fitBoundsKey = referencePoint
    ? `${referencePoint.lat},${referencePoint.lng}|${venues.map((v) => v.id).join(',')}`
    : undefined

  // Latest completed-gesture viewport (004 US2). Page state, deliberately NOT in the search store
  // - returning to the search must re-frame to the default full-radius view (004 spec FR-004).
  const [viewportBounds, setViewportBounds] = React.useState<MapBounds | null>(null)
  // A new reference means a new framing is coming - drop stale bounds so the list shows the full
  // set until the map reports the framed view (004 spec FR-009). Keyed on the coordinate string,
  // not the object, because the city-derived reference is rebuilt each render.
  const referenceKey = referencePoint ? `${referencePoint.lat},${referencePoint.lng}` : ''
  React.useEffect(() => {
    setViewportBounds(null)
  }, [referenceKey])

  // The list shows the viewport-visible subset (004 spec FR-007, supersedes 003 FR-013); the map
  // keeps rendering the FULL in-range set and emphasis stays the overall nearest (FR-011, FR-014).
  const visibleVenues = viewportBounds ? venues.filter((venue) => isInBounds(venue, viewportBounds)) : venues

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">{t('venues.title')}</h1>

      <div className="flex flex-wrap gap-2">
        <CityCombobox value={city} onChange={setCity} placeholder={t('venues.cityPlaceholder')} />
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
            center={referencePoint}
            cluster
            fitBoundsKey={fitBoundsKey}
            onViewportChange={setViewportBounds}
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

      <div className="flex flex-col gap-3">
        {visibleVenues.map((venue) => (
          <Link key={venue.id} to={`/venues/${venue.id}`}>
            <Card className="transition-colors hover:bg-accent/50">
              <CardHeader>
                <CardTitle>{venue.name}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">
                  {cityName(venue.city, i18n.language)}, {venue.address} - {venue.distanceKm.toFixed(1)} km
                </p>
                {venue.description && <p className="mt-1 text-sm">{venue.description}</p>}
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  )
}

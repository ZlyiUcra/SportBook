import React from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { Button } from '@/shared/ui/button'
import type { VenueSummary } from '@/entities/venue/model/types'

// Lazy so leaflet/react-leaflet never land in the initial route chunk (spec SC-006) - this
// import only fires once the customer opts to open the map.
const MapView = React.lazy(() => import('@/shared/ui/map/MapView'))

type PinnedVenue = VenueSummary & { latitude: number; longitude: number }

type VenueSearchMapProps = {
  venues: VenueSummary[]
}

/**
 * Map section for the venue search page (spec US5) - shows pins only for the current results
 * page's venues that have a precise location; venues without one simply do not appear on it.
 */
export function VenueSearchMap({ venues }: VenueSearchMapProps) {
  const { t } = useTranslation()
  const [open, setOpen] = React.useState(false)

  const pinned = venues.filter((v): v is PinnedVenue => v.latitude !== null && v.longitude !== null)

  if (pinned.length === 0) {
    return null
  }

  return (
    <div className="flex flex-col gap-2">
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="self-start"
        onClick={() => setOpen((prev) => !prev)}
      >
        {open ? t('venues.hideMap') : t('venues.showMap')}
      </Button>
      {open && (
        <React.Suspense fallback={<p className="text-sm text-muted-foreground">{t('common.loading')}</p>}>
          <MapView
            className="h-80 w-full rounded-md"
            center={{ lat: pinned[0].latitude, lng: pinned[0].longitude }}
            markers={pinned.map((venue) => ({
              id: venue.id,
              position: { lat: venue.latitude, lng: venue.longitude },
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
    </div>
  )
}

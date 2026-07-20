import React from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { Label } from '@/shared/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { ApiRequestError } from '@/shared/api/axiosInstance'
import { formatTime, tomorrowIsoDate } from '@/shared/lib/datetime'
import { getVenue } from '@/entities/venue/api/venueApi'
import { getAvailability } from '@/entities/booking/api/bookingApi'
import { createBooking } from '@/features/booking/create/api/createBooking'
import { listReviews } from '@/entities/review/api/reviewApi'
import { cityName } from '@/entities/city/model/types'

// Lazy so leaflet/react-leaflet never land in the initial route chunk (spec SC-006) - this
// import only fires when a venue actually has a precise location to show.
const MapView = React.lazy(() => import('@/shared/ui/map/MapView'))

/** T038: venue detail with court list, availability picker, and booking action. */
export function VenueDetailPage() {
  const { t, i18n } = useTranslation()
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()
  const [selectedCourtId, setSelectedCourtId] = React.useState<string | null>(null)
  const [date, setDate] = React.useState(tomorrowIsoDate())

  const venueQuery = useQuery({
    queryKey: ['venue', id],
    queryFn: () => getVenue(id!),
    enabled: !!id,
  })

  const availabilityQuery = useQuery({
    queryKey: ['availability', selectedCourtId, date],
    queryFn: () => getAvailability(selectedCourtId!, date),
    enabled: !!selectedCourtId && !!date,
  })

  const bookMutation = useMutation({
    mutationFn: createBooking,
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['availability', selectedCourtId, date] }),
  })

  const reviewsQuery = useQuery({
    queryKey: ['reviews', id],
    queryFn: () => listReviews(id!),
    enabled: !!id,
  })

  // Always-visible return to the search (004 spec FR-001) - a route link, not history
  // navigation, so it works identically for customers who landed here directly (research.md
  // "Back control"); the search itself restores from the session store (004 US1).
  const backToSearch = (
    <Link to="/" className="self-start text-sm text-muted-foreground underline">
      {t('venueDetail.backToSearch')}
    </Link>
  )

  if (venueQuery.isLoading) {
    return (
      <div className="flex flex-col gap-4 p-4">
        {backToSearch}
        <p className="text-muted-foreground">{t('common.loading')}</p>
      </div>
    )
  }

  if (venueQuery.isError || !venueQuery.data) {
    return (
      <div className="flex flex-col gap-4 p-4">
        {backToSearch}
        <p className="text-destructive">{t('common.requestFailed')}</p>
      </div>
    )
  }

  const venue = venueQuery.data
  const activeCourts = venue.courts.filter((c) => c.isActive)
  const selectedCourt = activeCourts.find((c) => c.id === selectedCourtId)

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      {backToSearch}
      <div>
        <h1 className="text-2xl font-semibold">{venue.name}</h1>
        <p className="text-sm text-muted-foreground">
          {cityName(venue.city, i18n.language)}, {venue.address}
        </p>
        {venue.averageRating !== null && (
          <p className="text-sm text-muted-foreground">
            {t('venueDetail.rating', { rating: venue.averageRating.toFixed(1), count: venue.reviewCount })}
          </p>
        )}
        {venue.description && <p className="mt-2">{venue.description}</p>}
      </div>

      {venue.latitude !== null && venue.longitude !== null && (
        <React.Suspense fallback={<p className="text-sm text-muted-foreground">{t('common.loading')}</p>}>
          <MapView
            className="h-64 w-full rounded-md"
            center={{ lat: venue.latitude, lng: venue.longitude }}
            markers={[
              {
                id: venue.id,
                position: { lat: venue.latitude, lng: venue.longitude },
                // JSX children only - never bindPopup/setContent with strings (research.md Map
                // content safety); venue.name/description are unvalidated user input.
                popup: (
                  <div>
                    <p className="font-medium">{venue.name}</p>
                    {venue.description && <p className="text-sm">{venue.description}</p>}
                  </div>
                ),
              },
            ]}
          />
        </React.Suspense>
      )}

      <h2 className="text-lg font-medium">{t('venueDetail.courts')}</h2>
      {activeCourts.length === 0 && <p className="text-muted-foreground">{t('venueDetail.noCourts')}</p>}
      <div className="flex flex-wrap gap-2">
        {activeCourts.map((court) => (
          <Button
            key={court.id}
            variant={court.id === selectedCourtId ? 'default' : 'outline'}
            onClick={() => setSelectedCourtId(court.id)}
          >
            {court.name} - {t(`sport.${court.sportType}`)} ({t('venueDetail.pricePerHour', { price: court.pricePerHour })})
          </Button>
        ))}
      </div>

      {selectedCourt && (
        <Card>
          <CardHeader>
            <CardTitle>
              {t('venueDetail.availabilityFor', { court: selectedCourt.name })}
            </CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <div className="flex items-center gap-2">
              <Label htmlFor="booking-date">{t('venueDetail.date')}</Label>
              <Input
                id="booking-date"
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                className="max-w-44"
              />
            </div>

            {availabilityQuery.isLoading && (
              <p className="text-muted-foreground">{t('common.loading')}</p>
            )}
            {availabilityQuery.data && availabilityQuery.data.freeSlots.length === 0 && (
              <p className="text-muted-foreground">{t('venueDetail.noSlots')}</p>
            )}

            <div className="flex flex-wrap gap-2">
              {availabilityQuery.data?.freeSlots.map((slot) => (
                <Button
                  key={slot.start}
                  variant="secondary"
                  size="sm"
                  disabled={bookMutation.isPending}
                  onClick={() =>
                    bookMutation.mutate({
                      courtId: selectedCourt.id,
                      startTime: slot.start,
                      endTime: slot.end,
                    })
                  }
                >
                  {formatTime(slot.start)} - {formatTime(slot.end)}
                </Button>
              ))}
            </div>

            {bookMutation.isSuccess && (
              <p className="text-sm">
                {t('venueDetail.bookingSuccess', { price: bookMutation.data.totalPrice })}{' '}
                <Link to="/bookings" className="underline">
                  {t('venueDetail.viewBookings')}
                </Link>
              </p>
            )}
            {bookMutation.isError && (
              <p role="alert" className="text-sm text-destructive">
                {bookMutation.error instanceof ApiRequestError
                  ? bookMutation.error.message
                  : t('common.requestFailed')}
              </p>
            )}
          </CardContent>
        </Card>
      )}

      <h2 className="text-lg font-medium">{t('review.title')}</h2>
      {reviewsQuery.isLoading && <p className="text-muted-foreground">{t('common.loading')}</p>}
      {reviewsQuery.data && reviewsQuery.data.items.length === 0 && (
        <p className="text-muted-foreground">{t('review.empty')}</p>
      )}
      <div className="flex flex-col gap-3">
        {reviewsQuery.data?.items.map((review) => (
          <Card key={review.id}>
            <CardContent className="py-4">
              <p className="font-medium">
                {review.userName} - {t('review.ratingValue', { rating: review.rating })}
              </p>
              {review.comment && <p className="mt-1 text-sm text-muted-foreground">{review.comment}</p>}
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}

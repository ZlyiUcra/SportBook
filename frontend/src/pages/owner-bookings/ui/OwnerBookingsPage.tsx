import React from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { Card, CardContent } from '@/shared/ui/card'
import { ApiRequestError } from '@/shared/api/axiosInstance'
import { formatDateTime, formatTime } from '@/shared/lib/datetime'
import { searchVenues } from '@/entities/venue/api/venueApi'
import { listBookingsByVenue } from '@/entities/booking/api/bookingApi'
import { confirmBooking } from '@/features/booking/confirm/api/confirmBooking'
import { cityName } from '@/entities/city/model/types'

/** T052: bookings for one of the caller's own venues, with a confirm action on Pending ones. */
export function OwnerBookingsPage() {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [selectedVenueId, setSelectedVenueId] = React.useState<string | null>(null)

  const myVenuesQuery = useQuery({
    queryKey: ['my-venues'],
    queryFn: () => searchVenues({ mine: true }),
  })

  const venueId = selectedVenueId ?? myVenuesQuery.data?.items[0]?.id ?? null

  const bookingsQuery = useQuery({
    queryKey: ['venue-bookings', venueId],
    queryFn: () => listBookingsByVenue(venueId!),
    enabled: !!venueId,
  })

  const confirmMutation = useMutation({
    mutationFn: confirmBooking,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['venue-bookings', venueId] }),
  })

  if (myVenuesQuery.isLoading) {
    return <p className="p-4 text-muted-foreground">{t('common.loading')}</p>
  }

  if (!myVenuesQuery.data || myVenuesQuery.data.items.length === 0) {
    return (
      <div className="mx-auto max-w-3xl p-4">
        <h1 className="mb-4 text-2xl font-semibold">{t('nav.venueBookings')}</h1>
        <p className="text-muted-foreground">{t('owner.noVenues')}</p>
      </div>
    )
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">{t('nav.venueBookings')}</h1>

      <select
        value={venueId ?? ''}
        onChange={(e) => setSelectedVenueId(e.target.value)}
        aria-label={t('ownerBookings.selectVenue')}
        className="max-w-sm rounded-md border border-input bg-background px-2 py-1 text-sm"
      >
        {myVenuesQuery.data.items.map((venue) => (
          <option key={venue.id} value={venue.id}>
            {venue.name} - {cityName(venue.city, i18n.language)}
          </option>
        ))}
      </select>

      {bookingsQuery.isLoading && <p className="text-muted-foreground">{t('common.loading')}</p>}
      {bookingsQuery.isError && <p className="text-destructive">{t('common.requestFailed')}</p>}
      {bookingsQuery.data && bookingsQuery.data.items.length === 0 && (
        <p className="text-muted-foreground">{t('ownerBookings.empty')}</p>
      )}

      {confirmMutation.isError && (
        <p role="alert" className="text-sm text-destructive">
          {confirmMutation.error instanceof ApiRequestError
            ? confirmMutation.error.message
            : t('common.requestFailed')}
        </p>
      )}

      <div className="flex flex-col gap-3">
        {bookingsQuery.data?.items.map((booking) => (
          <Card key={booking.id}>
            <CardContent className="flex flex-wrap items-center justify-between gap-4 py-4">
              <div className="min-w-0">
                <p className="font-medium break-words">
                  {formatDateTime(booking.startTime)} - {formatTime(booking.endTime)}
                </p>
                <p className="text-sm text-muted-foreground">
                  {t(`status.${booking.status}`)} - {t('bookings.total', { price: booking.totalPrice })}
                </p>
              </div>
              {booking.status === 'Pending' && (
                <Button
                  variant="outline"
                  size="sm"
                  disabled={confirmMutation.isPending}
                  onClick={() => confirmMutation.mutate(booking.id)}
                >
                  {t('ownerBookings.confirm')}
                </Button>
              )}
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}

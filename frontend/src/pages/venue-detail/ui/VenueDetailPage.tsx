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

/** T038: venue detail with court list, availability picker, and booking action. */
export function VenueDetailPage() {
  const { t } = useTranslation()
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

  if (venueQuery.isLoading) {
    return <p className="p-4 text-muted-foreground">{t('common.loading')}</p>
  }

  if (venueQuery.isError || !venueQuery.data) {
    return <p className="p-4 text-destructive">{t('common.requestFailed')}</p>
  }

  const venue = venueQuery.data
  const activeCourts = venue.courts.filter((c) => c.isActive)
  const selectedCourt = activeCourts.find((c) => c.id === selectedCourtId)

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <div>
        <h1 className="text-2xl font-semibold">{venue.name}</h1>
        <p className="text-sm text-muted-foreground">
          {venue.city}, {venue.address}
        </p>
        {venue.averageRating !== null && (
          <p className="text-sm text-muted-foreground">
            {t('venueDetail.rating', { rating: venue.averageRating.toFixed(1), count: venue.reviewCount })}
          </p>
        )}
        {venue.description && <p className="mt-2">{venue.description}</p>}
      </div>

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
    </div>
  )
}

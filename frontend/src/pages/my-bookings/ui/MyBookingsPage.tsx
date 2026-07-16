import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { Card, CardContent } from '@/shared/ui/card'
import { ApiRequestError } from '@/shared/api/axiosInstance'
import { formatDateTime, formatTime } from '@/shared/lib/datetime'
import { listMyBookings } from '@/entities/booking/api/bookingApi'
import type { Booking } from '@/entities/booking/model/types'
import { cancelBooking } from '@/features/booking/cancel/api/cancelBooking'

/** T039: the caller's own bookings with a cancel action (FR-005 cutoff enforced server-side). */
export function MyBookingsPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const bookingsQuery = useQuery({
    queryKey: ['my-bookings'],
    queryFn: () => listMyBookings(),
  })

  const cancelMutation = useMutation({
    mutationFn: cancelBooking,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-bookings'] }),
  })

  const isCancellable = (booking: Booking) =>
    booking.status === 'Pending' || booking.status === 'Confirmed'

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">{t('bookings.title')}</h1>

      {bookingsQuery.isLoading && <p className="text-muted-foreground">{t('common.loading')}</p>}
      {bookingsQuery.isError && <p className="text-destructive">{t('common.requestFailed')}</p>}
      {bookingsQuery.data && bookingsQuery.data.items.length === 0 && (
        <p className="text-muted-foreground">{t('bookings.empty')}</p>
      )}

      {cancelMutation.isError && (
        <p role="alert" className="text-sm text-destructive">
          {cancelMutation.error instanceof ApiRequestError
            ? cancelMutation.error.message
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
              {isCancellable(booking) && (
                <Button
                  variant="outline"
                  size="sm"
                  disabled={cancelMutation.isPending}
                  onClick={() => cancelMutation.mutate(booking.id)}
                >
                  {t('bookings.cancel')}
                </Button>
              )}
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}

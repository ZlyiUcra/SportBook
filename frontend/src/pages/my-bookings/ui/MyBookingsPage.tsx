import React from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { Card, CardContent } from '@/shared/ui/card'
import { cn } from '@/shared/lib/utils'
import { ApiRequestError } from '@/shared/api/axiosInstance'
import { bookingStatusFilters, listMyBookings, type BookingStatusFilter } from '@/entities/booking/api/bookingApi'
import type { Booking } from '@/entities/booking/model/types'
import { BookingSummary } from '@/entities/booking/ui/BookingSummary'
import { cancelBooking } from '@/features/booking/cancel/api/cancelBooking'

/**
 * The caller's own bookings (001 T039) - each row now shows venue/city/sport/court detail (005
 * US1), filterable by status (US2) and paged with Prev/Next (US3). The filter and paging are
 * server-side, so they compose across the whole history; changing the filter resets to page 1.
 */
export function MyBookingsPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [status, setStatus] = React.useState<BookingStatusFilter>('All')
  const [page, setPage] = React.useState(1)

  const bookingsQuery = useQuery({
    queryKey: ['my-bookings', status, page],
    queryFn: () => listMyBookings(status, page),
    placeholderData: keepPreviousData,
  })

  const cancelMutation = useMutation({
    mutationFn: cancelBooking,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-bookings'] }),
  })

  const isCancellable = (booking: Booking) =>
    booking.status === 'Pending' || booking.status === 'Confirmed'

  const data = bookingsQuery.data
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const isEmpty = data && data.items.length === 0

  function changeStatus(next: BookingStatusFilter) {
    setStatus(next)
    setPage(1)
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">{t('bookings.title')}</h1>

      <div className="flex flex-wrap gap-2">
        {bookingStatusFilters.map((filter) => (
          <Button
            key={filter}
            type="button"
            variant="outline"
            size="sm"
            aria-pressed={status === filter}
            className={cn(status === filter && 'bg-muted text-foreground')}
            onClick={() => changeStatus(filter)}
          >
            {t(`bookings.filter.${filter}`)}
          </Button>
        ))}
      </div>

      {bookingsQuery.isLoading && <p className="text-muted-foreground">{t('common.loading')}</p>}
      {bookingsQuery.isError && <p className="text-destructive">{t('common.requestFailed')}</p>}
      {isEmpty && (
        <p className="text-muted-foreground">
          {status === 'All' ? t('bookings.empty') : t('bookings.noneInFilter')}
        </p>
      )}

      {cancelMutation.isError && (
        <p role="alert" className="text-sm text-destructive">
          {cancelMutation.error instanceof ApiRequestError
            ? cancelMutation.error.message
            : t('common.requestFailed')}
        </p>
      )}

      <div className="flex flex-col gap-3">
        {data?.items.map((booking) => (
          <Card key={booking.id}>
            <CardContent className="flex flex-wrap items-center justify-between gap-4 py-4">
              <BookingSummary booking={booking} />
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

      {data && data.totalCount > data.pageSize && (
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
            {t('common.prev')}
          </Button>
          <span className="text-sm text-muted-foreground">
            {page} / {totalPages}
          </span>
          <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>
            {t('common.next')}
          </Button>
        </div>
      )}
    </div>
  )
}

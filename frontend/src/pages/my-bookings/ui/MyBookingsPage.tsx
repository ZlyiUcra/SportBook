import React from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { Card, CardContent } from '@/shared/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/shared/ui/dialog'
import { cn } from '@/shared/lib/utils'
import { ApiRequestError } from '@/shared/api/axiosInstance'
import { bookingStatusFilters, listMyBookings, type BookingStatusFilter } from '@/entities/booking/api/bookingApi'
import type { Booking } from '@/entities/booking/model/types'
import { BookingSummary } from '@/entities/booking/ui/BookingSummary'
import { cancelBooking } from '@/features/booking/cancel/api/cancelBooking'
import { listReviews } from '@/entities/review/api/reviewApi'
import { createReview } from '@/features/review/create/api/createReview'
import { ReviewForm } from '@/features/review/create/ui/ReviewForm'
import { useSessionStore } from '@/entities/session/model/store'

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
              <div className="flex gap-2">
                {booking.status === 'Completed' && <ReviewAction booking={booking} />}
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
              </div>
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

/** A review is editable by its author only within this many hours of its original creation (007 data-model.md). */
const REVIEW_EDIT_WINDOW_HOURS = 24

function isWithinEditWindow(createdAt: string): boolean {
  const elapsedMs = Date.now() - new Date(createdAt).getTime()
  return elapsedMs <= REVIEW_EDIT_WINDOW_HOURS * 60 * 60 * 1000
}

/**
 * The review entry for a completed booking (005/006 US2) - reached only from here, never from the
 * venue page. Reviews are per venue, not per booking, so it reuses the venue's review list to
 * pre-fill the caller's existing review; the eligibility gate (006 US1) is enforced server-side, so
 * a rejection (e.g. a legacy author who no longer qualifies) surfaces via ApiRequestError. Once a
 * review is older than the 24-hour edit window (007 US1), it is shown read-only here instead of an
 * edit form - it stays visible, only further editing is withdrawn.
 */
function ReviewAction({ booking }: { booking: Booking }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const currentUser = useSessionStore((state) => state.user)
  const [open, setOpen] = React.useState(false)

  // Not gated on `open` (007): the trigger button's label, and whether the edit window is still
  // open, must be known before the dialog is ever opened.
  const reviewsQuery = useQuery({
    queryKey: ['reviews', booking.venueId],
    queryFn: () => listReviews(booking.venueId),
  })

  const reviewMutation = useMutation({
    mutationFn: (values: Parameters<typeof createReview>[1]) => createReview(booking.venueId, values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reviews', booking.venueId] })
      setOpen(false)
    },
  })

  const mine = reviewsQuery.data?.items.find((r) => r.userId === currentUser?.id)
  const isEditWindowOpen = !mine || isWithinEditWindow(mine.createdAt)

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          {!mine ? t('review.addAction') : isEditWindowOpen ? t('review.editAction') : t('review.viewAction')}
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {!mine ? t('review.addTitle') : isEditWindowOpen ? t('review.editTitle') : t('review.readOnlyTitle')}
          </DialogTitle>
        </DialogHeader>
        {mine && !isEditWindowOpen ? (
          <div className="flex flex-col gap-2">
            <p className="text-sm text-muted-foreground">{t('review.readOnlyNotice')}</p>
            <p className="font-medium">{t('review.ratingValue', { rating: mine.rating })}</p>
            {mine.comment && <p className="text-sm text-muted-foreground">{mine.comment}</p>}
          </div>
        ) : (
          <>
            <ReviewForm
              defaultValues={mine ? { rating: mine.rating, comment: mine.comment ?? '' } : undefined}
              onSubmit={(values) => reviewMutation.mutate(values)}
              isSubmitting={reviewMutation.isPending}
            />
            {reviewMutation.isError && (
              <p role="alert" className="text-sm text-destructive">
                {reviewMutation.error instanceof ApiRequestError
                  ? reviewMutation.error.message
                  : t('common.requestFailed')}
              </p>
            )}
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}

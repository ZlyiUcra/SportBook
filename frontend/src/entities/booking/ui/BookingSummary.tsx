import { useTranslation } from 'react-i18next'
import { formatDateTime, formatTime } from '@/shared/lib/datetime'
import { cityName } from '@/entities/city/model/types'
import type { Booking } from '../model/types'

/**
 * The informational block of a booking row (005 US1) - venue, city, sport, court, time, status,
 * and price. Shared by the customer "My bookings" and owner "Venue bookings" lists so the enriched
 * detail is written once; each page wraps this with its own action (cancel / confirm). The raw
 * court id is never shown (spec FR-011).
 */
export function BookingSummary({ booking }: { booking: Booking }) {
  const { t, i18n } = useTranslation()

  return (
    <div className="min-w-0">
      <p className="font-medium break-words">{booking.venueName}</p>
      <p className="text-sm text-muted-foreground break-words">
        {cityName(booking.city, i18n.language)}, {t(`sport.${booking.sport}`)} - {booking.courtName}
      </p>
      <p className="mt-1 text-sm break-words">
        {formatDateTime(booking.startTime)} - {formatTime(booking.endTime)}
      </p>
      <p className="text-sm text-muted-foreground">
        {t(`status.${booking.status}`)} - {t('bookings.total', { price: booking.totalPrice })}
      </p>
    </div>
  )
}

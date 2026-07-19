import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import type { GeolocationStatus } from '@/shared/lib/useGeolocation'

type NearMeButtonProps = {
  status: GeolocationStatus
  onActivate: () => void
}

/**
 * "Near me" action (003 spec US1) - triggers the device-location lookup. Presentational only:
 * the status/trigger come from the page's single `useReferencePoint` instance so the map and
 * this button never read two independent geolocation states (research.md "Reference-point
 * resolution and geolocation"). Coordinate rounding and permission/denied/error handling live in
 * the shared `useGeolocation` hook this button's status is sourced from.
 */
export function NearMeButton({ status, onActivate }: NearMeButtonProps) {
  const { t } = useTranslation()
  const isError = status === 'denied' || status === 'error'

  return (
    <div className="flex items-center gap-2">
      <Button type="button" variant="outline" onClick={onActivate} disabled={status === 'locating'}>
        {status === 'locating' ? t('citySelect.locating') : t('citySelect.nearMe')}
      </Button>
      {isError && <p className="text-xs text-muted-foreground">{t('citySelect.nearMeError')}</p>}
    </div>
  )
}

import React from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { findNearestCity } from '@/entities/city/api/cityApi'
import type { City } from '@/entities/city/model/types'

type Status = 'idle' | 'locating' | 'error'

type MyCityButtonProps = {
  onDetected: (city: City) => void
}

/**
 * "My city" detection (spec US3): degrades to manual selection with no blocking error whenever
 * geolocation is unsupported, denied, or the lookup fails - manual selection via CityCombobox
 * always remains available regardless of this button's outcome. Coordinates are rounded to 2
 * decimals client-side before the request (research.md Geolocation privacy posture).
 */
export function MyCityButton({ onDetected }: MyCityButtonProps) {
  const { t } = useTranslation()
  const [status, setStatus] = React.useState<Status>('idle')

  function handleClick() {
    if (!('geolocation' in navigator)) {
      setStatus('error')
      return
    }

    setStatus('locating')
    navigator.geolocation.getCurrentPosition(
      (position) => {
        const lat = Math.round(position.coords.latitude * 100) / 100
        const lng = Math.round(position.coords.longitude * 100) / 100
        findNearestCity(lat, lng)
          .then((city) => {
            onDetected(city)
            setStatus('idle')
          })
          .catch(() => setStatus('error'))
      },
      () => setStatus('error'),
    )
  }

  return (
    <div className="flex items-center gap-2">
      <Button type="button" variant="outline" onClick={handleClick} disabled={status === 'locating'}>
        {status === 'locating' ? t('citySelect.locating') : t('citySelect.myCity')}
      </Button>
      {status === 'error' && <p className="text-xs text-muted-foreground">{t('citySelect.myCityError')}</p>}
    </div>
  )
}

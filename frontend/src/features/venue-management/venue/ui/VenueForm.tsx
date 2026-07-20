import React from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { Label } from '@/shared/ui/label'
import { CityCombobox } from '@/features/city-select/ui/CityCombobox'
import { PageLoader } from '@/shared/ui/page-loader'
import type { City } from '@/entities/city/model/types'
import { venueFormSchema, type VenueFormValues } from '../model/schema'

// Lazy so leaflet/react-leaflet never land in the initial route chunk (spec SC-006) - the pin
// picker only renders (and only then triggers this import) once the owner opts to place a pin.
const MapView = React.lazy(() => import('@/shared/ui/map/MapView'))

type VenueFormProps = {
  defaultValues?: VenueFormValues
  /** Only needed when editing - the combobox needs the full City (name/region) to display, not just its id. */
  defaultCity?: City
  onSubmit: (values: VenueFormValues) => void
  onCancel: () => void
  isSubmitting: boolean
}

/** Shared create/edit form for a venue - callers decide whether onSubmit creates or updates. */
export function VenueForm({ defaultValues, defaultCity, onSubmit, onCancel, isSubmitting }: VenueFormProps) {
  const { t } = useTranslation()
  const {
    register,
    control,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<VenueFormValues>({
    resolver: zodResolver(venueFormSchema),
    defaultValues: defaultValues ?? { name: '', cityId: 0, address: '', description: '' },
  })
  const [selectedCity, setSelectedCity] = React.useState<City | null>(defaultCity ?? null)
  const [pinPickerOpen, setPinPickerOpen] = React.useState(false)
  const latitude = useWatch({ control, name: 'latitude' })
  const longitude = useWatch({ control, name: 'longitude' })
  const hasPin = latitude !== undefined && longitude !== undefined

  function clearPin() {
    setValue('latitude', undefined)
    setValue('longitude', undefined)
    setPinPickerOpen(false)
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
      <div className="flex flex-col gap-2">
        <Label htmlFor="venue-name">{t('owner.venue.name')}</Label>
        <Input id="venue-name" {...register('name')} />
        {errors.name && <p className="text-sm text-destructive">{t('owner.venue.nameRequired')}</p>}
      </div>
      <div className="flex flex-col gap-2">
        <Label>{t('owner.venue.city')}</Label>
        <Controller
          control={control}
          name="cityId"
          render={({ field }) => (
            <CityCombobox
              value={selectedCity}
              onChange={(city) => {
                setSelectedCity(city)
                field.onChange(city.id)
              }}
            />
          )}
        />
        {errors.cityId && <p className="text-sm text-destructive">{t('owner.venue.cityRequired')}</p>}
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="venue-address">{t('owner.venue.address')}</Label>
        <Input id="venue-address" {...register('address')} />
        {errors.address && <p className="text-sm text-destructive">{t('owner.venue.addressRequired')}</p>}
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="venue-description">{t('owner.venue.description')}</Label>
        <Input id="venue-description" {...register('description')} />
      </div>

      <div className="flex flex-col gap-2">
        <Label>{t('owner.venue.pin.label')}</Label>
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={!selectedCity}
            onClick={() => setPinPickerOpen((prev) => !prev)}
          >
            {hasPin ? t('owner.venue.pin.move') : t('owner.venue.pin.place')}
          </Button>
          {hasPin && (
            <Button type="button" variant="outline" size="sm" onClick={clearPin}>
              {t('owner.venue.pin.remove')}
            </Button>
          )}
        </div>
        {errors.longitude && <p className="text-sm text-destructive">{errors.longitude.message}</p>}

        {pinPickerOpen && selectedCity && (
          <React.Suspense fallback={<PageLoader />}>
            <MapView
              className="h-64 w-full rounded-md"
              center={
                hasPin && latitude !== undefined && longitude !== undefined
                  ? { lat: latitude, lng: longitude }
                  : { lat: selectedCity.latitude, lng: selectedCity.longitude }
              }
              markers={
                hasPin && latitude !== undefined && longitude !== undefined
                  ? [{ id: 'pin', position: { lat: latitude, lng: longitude } }]
                  : []
              }
              onPick={(position) => {
                setValue('latitude', position.lat, { shouldValidate: true })
                setValue('longitude', position.lng, { shouldValidate: true })
              }}
            />
          </React.Suspense>
        )}
      </div>

      <div className="flex gap-2">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? t('common.saving') : t('common.save')}
        </Button>
        <Button type="button" variant="outline" onClick={onCancel}>
          {t('common.cancel')}
        </Button>
      </div>
    </form>
  )
}

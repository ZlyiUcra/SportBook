import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { Label } from '@/shared/ui/label'
import { venueFormSchema, type VenueFormValues } from '../model/schema'

type VenueFormProps = {
  defaultValues?: VenueFormValues
  onSubmit: (values: VenueFormValues) => void
  onCancel: () => void
  isSubmitting: boolean
}

/** Shared create/edit form for a venue - callers decide whether onSubmit creates or updates. */
export function VenueForm({ defaultValues, onSubmit, onCancel, isSubmitting }: VenueFormProps) {
  const { t } = useTranslation()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<VenueFormValues>({
    resolver: zodResolver(venueFormSchema),
    defaultValues: defaultValues ?? { name: '', city: '', address: '', description: '' },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
      <div className="flex flex-col gap-2">
        <Label htmlFor="venue-name">{t('owner.venue.name')}</Label>
        <Input id="venue-name" {...register('name')} />
        {errors.name && <p className="text-sm text-destructive">{t('owner.venue.nameRequired')}</p>}
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="venue-city">{t('owner.venue.city')}</Label>
        <Input id="venue-city" {...register('city')} />
        {errors.city && <p className="text-sm text-destructive">{t('owner.venue.cityRequired')}</p>}
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

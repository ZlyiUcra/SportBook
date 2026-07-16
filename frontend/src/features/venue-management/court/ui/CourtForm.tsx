import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { Label } from '@/shared/ui/label'
import { sportTypes } from '@/entities/venue/model/types'
import { editCourtFormSchema, type EditCourtFormValues } from '../model/schema'

type CourtFormProps = {
  mode: 'create' | 'edit'
  defaultValues?: EditCourtFormValues
  onSubmit: (values: EditCourtFormValues) => void
  onCancel: () => void
  isSubmitting: boolean
}

/**
 * One form for both create and edit - `isActive` is always tracked (default `true`) so a single
 * `EditCourtFormValues` submit shape works for both; only 'edit' mode shows the field, since
 * CreateCourtRequest never accepts it (new courts are always active).
 */
export function CourtForm({ mode, defaultValues, onSubmit, onCancel, isSubmitting }: CourtFormProps) {
  const { t } = useTranslation()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<EditCourtFormValues>({
    resolver: zodResolver(editCourtFormSchema),
    defaultValues: defaultValues ?? {
      name: '',
      sportType: 'Tennis',
      pricePerHour: 0,
      openingTime: '08:00',
      closingTime: '22:00',
      isActive: true,
    },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
      <div className="flex flex-col gap-2">
        <Label htmlFor="court-name">{t('owner.court.name')}</Label>
        <Input id="court-name" {...register('name')} />
        {errors.name && <p className="text-sm text-destructive">{t('owner.court.nameRequired')}</p>}
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="court-sport">{t('owner.court.sport')}</Label>
        <select
          id="court-sport"
          {...register('sportType')}
          className="rounded-md border border-input bg-background px-2 py-1 text-sm"
        >
          {sportTypes.map((sport) => (
            <option key={sport} value={sport}>
              {t(`sport.${sport}`)}
            </option>
          ))}
        </select>
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="court-price">{t('owner.court.pricePerHour')}</Label>
        <Input
          id="court-price"
          type="number"
          step="0.01"
          min="0.01"
          {...register('pricePerHour', { valueAsNumber: true })}
        />
        {errors.pricePerHour && (
          <p className="text-sm text-destructive">{t('owner.court.priceInvalid')}</p>
        )}
      </div>
      <div className="flex gap-3">
        <div className="flex flex-1 flex-col gap-2">
          <Label htmlFor="court-opening">{t('owner.court.opening')}</Label>
          <Input id="court-opening" type="time" {...register('openingTime')} />
        </div>
        <div className="flex flex-1 flex-col gap-2">
          <Label htmlFor="court-closing">{t('owner.court.closing')}</Label>
          <Input id="court-closing" type="time" {...register('closingTime')} />
        </div>
      </div>
      {mode === 'edit' && (
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" {...register('isActive')} className="size-4" />
          {t('owner.court.active')}
        </label>
      )}
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

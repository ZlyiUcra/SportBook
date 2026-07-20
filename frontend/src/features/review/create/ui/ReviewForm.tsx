import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { Label } from '@/shared/ui/label'
import { StarRating } from './StarRating'
import { reviewFormSchema, type ReviewFormValues } from '../model/schema'

type ReviewFormProps = {
  defaultValues?: ReviewFormValues
  onSubmit: (values: ReviewFormValues) => void
  isSubmitting: boolean
}

/** Same form serves both first-time submission and replacing the caller's existing review (one review per user per venue). */
export function ReviewForm({ defaultValues, onSubmit, isSubmitting }: ReviewFormProps) {
  const { t } = useTranslation()
  const { control, register, handleSubmit } = useForm<ReviewFormValues>({
    resolver: zodResolver(reviewFormSchema),
    defaultValues: defaultValues ?? { rating: 5, comment: '' },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
      <div className="flex flex-col gap-2">
        <Label>{t('review.rating')}</Label>
        <Controller
          control={control}
          name="rating"
          render={({ field }) => <StarRating value={field.value} onChange={field.onChange} />}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="review-comment">{t('review.comment')}</Label>
        <textarea
          id="review-comment"
          {...register('comment')}
          rows={3}
          className="rounded-md border border-input bg-background px-3 py-2 text-sm"
        />
      </div>
      <Button type="submit" disabled={isSubmitting} className="self-start">
        {isSubmitting ? t('common.saving') : t('review.submit')}
      </Button>
    </form>
  )
}

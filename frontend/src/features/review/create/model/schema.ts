import { z } from 'zod'

export const reviewFormSchema = z.object({
  rating: z.number().min(1).max(5),
  comment: z.string().optional(),
})

export type ReviewFormValues = z.infer<typeof reviewFormSchema>

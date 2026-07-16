import { z } from 'zod'

export const venueFormSchema = z.object({
  name: z.string().min(1),
  city: z.string().min(1),
  address: z.string().min(1),
  description: z.string().optional(),
})

export type VenueFormValues = z.infer<typeof venueFormSchema>

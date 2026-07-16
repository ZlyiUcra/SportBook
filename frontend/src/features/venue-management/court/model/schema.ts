import { z } from 'zod'
import { sportTypes } from '@/entities/venue/model/types'

const courtBaseSchema = z.object({
  name: z.string().min(1),
  sportType: z.enum(sportTypes),
  // Plain z.number(), not z.coerce.number() - coerce's input/output type mismatch is incompatible
  // with zodResolver's Resolver<TFieldValues> typing. The DOM string->number conversion happens
  // via register(..., { valueAsNumber: true }) in CourtForm instead.
  pricePerHour: z.number().positive(),
  openingTime: z.string().min(1),
  closingTime: z.string().min(1),
})

/** No `isActive` - matches CreateCourtRequest, which never accepts it (new courts are always active). */
export const createCourtFormSchema = courtBaseSchema
export type CreateCourtFormValues = z.infer<typeof createCourtFormSchema>

/** Adds `isActive` - matches UpdateCourtRequest. */
export const editCourtFormSchema = courtBaseSchema.extend({ isActive: z.boolean() })
export type EditCourtFormValues = z.infer<typeof editCourtFormSchema>

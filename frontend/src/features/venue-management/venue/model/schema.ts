import { z } from 'zod'

/**
 * `cityId` comes from the city combobox (features/city-select), never free text (spec US2
 * Acceptance Scenario 1). `latitude`/`longitude` are optional but both-or-neither - the pin
 * picker (MapView) always sets both together, and "remove pin" always clears both together, so
 * a partial pair here would only happen through a bug, not a legitimate user action; the backend
 * still validates as well (contracts/api.md).
 */
export const venueFormSchema = z
  .object({
    name: z.string().min(1),
    cityId: z.number().int().positive(),
    address: z.string().min(1),
    description: z.string().optional(),
    latitude: z.number().min(-90).max(90).optional(),
    longitude: z.number().min(-180).max(180).optional(),
  })
  .refine((values) => (values.latitude === undefined) === (values.longitude === undefined), {
    message: 'latitude and longitude must be set together',
    path: ['longitude'],
  })

export type VenueFormValues = z.infer<typeof venueFormSchema>

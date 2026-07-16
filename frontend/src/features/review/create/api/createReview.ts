import { axiosInstance } from '@/shared/api/axiosInstance'
import type { Review } from '@/entities/review/model/types'
import type { ReviewFormValues } from '../model/schema'

/** 201 for a new review, 200 if it replaces the caller's existing one (contracts/api.md) - callers don't need to distinguish. */
export async function createReview(venueId: string, values: ReviewFormValues): Promise<Review> {
  const { data } = await axiosInstance.post<Review>(`/venues/${venueId}/reviews`, values)
  return data
}

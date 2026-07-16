import { axiosInstance } from '@/shared/api/axiosInstance'
import type { PagedResponse } from '@/shared/api/types'
import type { Review } from '../model/types'

export async function listReviews(venueId: string, page = 1): Promise<PagedResponse<Review>> {
  const { data } = await axiosInstance.get<PagedResponse<Review>>(`/venues/${venueId}/reviews`, {
    params: { page },
  })
  return data
}

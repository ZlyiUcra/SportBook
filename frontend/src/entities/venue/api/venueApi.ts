import { axiosInstance } from '@/shared/api/axiosInstance'
import type { PagedResponse } from '@/shared/api/types'
import type { SportType, VenueDetail, VenueSummary } from '../model/types'

export type VenueSearchParams = {
  city?: string
  sportType?: SportType
  mine?: boolean
  page?: number
}

export async function searchVenues(params: VenueSearchParams): Promise<PagedResponse<VenueSummary>> {
  const { data } = await axiosInstance.get<PagedResponse<VenueSummary>>('/venues', {
    params: {
      city: params.city || undefined,
      sportType: params.sportType,
      mine: params.mine || undefined,
      page: params.page ?? 1,
      pageSize: params.mine ? 100 : undefined,
    },
  })
  return data
}

export async function getVenue(id: string): Promise<VenueDetail> {
  const { data } = await axiosInstance.get<VenueDetail>(`/venues/${id}`)
  return data
}

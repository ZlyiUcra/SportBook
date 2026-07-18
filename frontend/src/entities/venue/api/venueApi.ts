import { axiosInstance } from '@/shared/api/axiosInstance'
import type { PagedResponse } from '@/shared/api/types'
import type { SportType, VenueDetail, VenueSummary } from '../model/types'

export type VenueSearchParams = {
  cityId?: number
  /** Only has an effect together with `cityId` (spec US4) - the 150km radius is server-side, not client-configurable. */
  includeNearby?: boolean
  sportType?: SportType
  mine?: boolean
  page?: number
}

export async function searchVenues(params: VenueSearchParams): Promise<PagedResponse<VenueSummary>> {
  const { data } = await axiosInstance.get<PagedResponse<VenueSummary>>('/venues', {
    params: {
      cityId: params.cityId,
      includeNearby: params.includeNearby || undefined,
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

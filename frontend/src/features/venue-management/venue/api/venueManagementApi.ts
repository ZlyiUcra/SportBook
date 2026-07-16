import { axiosInstance } from '@/shared/api/axiosInstance'
import type { VenueDetail } from '@/entities/venue/model/types'
import type { VenueFormValues } from '../model/schema'

export async function createVenue(values: VenueFormValues): Promise<VenueDetail> {
  const { data } = await axiosInstance.post<VenueDetail>('/venues', values)
  return data
}

export async function updateVenue(venueId: string, values: VenueFormValues): Promise<VenueDetail> {
  const { data } = await axiosInstance.put<VenueDetail>(`/venues/${venueId}`, values)
  return data
}

export async function deleteVenue(venueId: string): Promise<void> {
  await axiosInstance.delete(`/venues/${venueId}`)
}

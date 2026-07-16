import { axiosInstance } from '@/shared/api/axiosInstance'
import type { Court } from '@/entities/venue/model/types'
import type { CreateCourtFormValues, EditCourtFormValues } from '../model/schema'

/** Backend TimeOnly binds "HH:mm:ss" - the <input type="time"> gives "HH:mm", so append seconds. */
function withSeconds(time: string): string {
  return time.length === 5 ? `${time}:00` : time
}

export async function createCourt(venueId: string, values: CreateCourtFormValues): Promise<Court> {
  const { data } = await axiosInstance.post<Court>(`/venues/${venueId}/courts`, {
    name: values.name,
    sportType: values.sportType,
    pricePerHour: values.pricePerHour,
    openingTime: withSeconds(values.openingTime),
    closingTime: withSeconds(values.closingTime),
  })
  return data
}

export async function updateCourt(courtId: string, values: EditCourtFormValues): Promise<Court> {
  const { data } = await axiosInstance.put<Court>(`/courts/${courtId}`, {
    name: values.name,
    sportType: values.sportType,
    pricePerHour: values.pricePerHour,
    openingTime: withSeconds(values.openingTime),
    closingTime: withSeconds(values.closingTime),
    isActive: values.isActive,
  })
  return data
}

export async function deleteCourt(courtId: string): Promise<void> {
  await axiosInstance.delete(`/courts/${courtId}`)
}

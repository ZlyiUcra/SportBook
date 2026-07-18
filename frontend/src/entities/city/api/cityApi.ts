import { axiosInstance } from '@/shared/api/axiosInstance'
import type { City } from '../model/types'

/** `query` must be at least 2 characters (contracts/api.md) - callers debounce and enforce the minimum before calling. */
export async function suggestCities(query: string): Promise<City[]> {
  const { data } = await axiosInstance.get<City[]>('/cities', { params: { query } })
  return data
}

/** `lat`/`lng` should already be rounded to 2 decimals by the caller (research.md Geolocation privacy posture). */
export async function findNearestCity(lat: number, lng: number): Promise<City> {
  const { data } = await axiosInstance.get<City>('/cities/nearest', { params: { lat, lng } })
  return data
}

/** Mirrors the backend CityResponse (contracts/api.md Cities section) - `population` is deliberately not exposed. */
export type City = {
  id: number
  nameEn: string
  nameUk: string
  namePt: string
  regionEn: string
  regionUk: string
  regionPt: string
  latitude: number
  longitude: number
}

/** Picks the localized display name for the active i18n language, falling back to English. */
export function cityName(city: City, language: string): string {
  if (language.startsWith('uk')) return city.nameUk
  if (language.startsWith('pt')) return city.namePt
  return city.nameEn
}

/** Picks the localized region name for the active i18n language, falling back to English. */
export function cityRegionName(city: City, language: string): string {
  if (language.startsWith('uk')) return city.regionUk
  if (language.startsWith('pt')) return city.regionPt
  return city.regionEn
}

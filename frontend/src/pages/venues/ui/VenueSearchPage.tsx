import React from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { Button } from '@/shared/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { searchVenues } from '@/entities/venue/api/venueApi'
import { sportTypes, type SportType } from '@/entities/venue/model/types'
import { CityCombobox } from '@/features/city-select/ui/CityCombobox'
import { MyCityButton } from '@/features/city-select/ui/MyCityButton'
import { cityName, type City } from '@/entities/city/model/types'
import { VenueSearchMap } from './VenueSearchMap'

/** T019: city/sport venue search with offset pagination - the city filter is a directory pick, never free text (spec US1). */
export function VenueSearchPage() {
  const { t, i18n } = useTranslation()
  const [city, setCity] = React.useState<City | null>(null)
  const [includeNearby, setIncludeNearby] = React.useState(false)
  const [sportType, setSportType] = React.useState<SportType | ''>('')
  const [page, setPage] = React.useState(1)

  const venuesQuery = useQuery({
    queryKey: ['venues', { cityId: city?.id, includeNearby, sportType, page }],
    queryFn: () =>
      searchVenues({ cityId: city?.id, includeNearby: !!city && includeNearby, sportType: sportType || undefined, page }),
    placeholderData: keepPreviousData,
  })

  const totalPages = venuesQuery.data
    ? Math.max(1, Math.ceil(venuesQuery.data.totalCount / venuesQuery.data.pageSize))
    : 1

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">{t('venues.title')}</h1>

      <div className="flex flex-wrap gap-2">
        <CityCombobox
          value={city}
          onChange={(selected) => {
            setCity(selected)
            setPage(1)
          }}
          placeholder={t('venues.cityPlaceholder')}
        />
        <MyCityButton
          onDetected={(detected) => {
            setCity(detected)
            setPage(1)
          }}
        />
        <select
          value={sportType}
          onChange={(e) => {
            setSportType(e.target.value as SportType | '')
            setPage(1)
          }}
          aria-label={t('venues.sportLabel')}
          className="rounded-md border border-input bg-background px-2 py-1 text-sm"
        >
          <option value="">{t('venues.allSports')}</option>
          {sportTypes.map((sport) => (
            <option key={sport} value={sport}>
              {t(`sport.${sport}`)}
            </option>
          ))}
        </select>
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={includeNearby}
            disabled={!city}
            onChange={(e) => {
              setIncludeNearby(e.target.checked)
              setPage(1)
            }}
          />
          {t('venues.includeNearby')}
        </label>
      </div>

      {venuesQuery.isLoading && <p className="text-muted-foreground">{t('common.loading')}</p>}
      {venuesQuery.isError && <p className="text-destructive">{t('common.requestFailed')}</p>}

      {venuesQuery.data && venuesQuery.data.items.length === 0 && (
        <p className="text-muted-foreground">{t('venues.noResults')}</p>
      )}

      {venuesQuery.data && <VenueSearchMap venues={venuesQuery.data.items} />}

      <div className="flex flex-col gap-3">
        {venuesQuery.data?.items.map((venue) => (
          <Link key={venue.id} to={`/venues/${venue.id}`}>
            <Card className="transition-colors hover:bg-accent/50">
              <CardHeader>
                <CardTitle>{venue.name}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">
                  {cityName(venue.city, i18n.language)}, {venue.address}
                </p>
                {venue.description && <p className="mt-1 text-sm">{venue.description}</p>}
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      {venuesQuery.data && venuesQuery.data.totalCount > venuesQuery.data.pageSize && (
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
            {t('common.prev')}
          </Button>
          <span className="text-sm text-muted-foreground">
            {page} / {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage(page + 1)}
          >
            {t('common.next')}
          </Button>
        </div>
      )}
    </div>
  )
}

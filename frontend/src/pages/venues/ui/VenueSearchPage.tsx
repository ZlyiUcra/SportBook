import React from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { searchVenues } from '@/entities/venue/api/venueApi'
import { sportTypes, type SportType } from '@/entities/venue/model/types'

/** T037: city/sport venue search with offset pagination. */
export function VenueSearchPage() {
  const { t } = useTranslation()
  const [city, setCity] = React.useState('')
  const [sportType, setSportType] = React.useState<SportType | ''>('')
  const [page, setPage] = React.useState(1)

  const venuesQuery = useQuery({
    queryKey: ['venues', { city, sportType, page }],
    queryFn: () => searchVenues({ city, sportType: sportType || undefined, page }),
    placeholderData: keepPreviousData,
  })

  const totalPages = venuesQuery.data
    ? Math.max(1, Math.ceil(venuesQuery.data.totalCount / venuesQuery.data.pageSize))
    : 1

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">{t('venues.title')}</h1>

      <div className="flex flex-wrap gap-2">
        <Input
          value={city}
          onChange={(e) => {
            setCity(e.target.value)
            setPage(1)
          }}
          placeholder={t('venues.cityPlaceholder')}
          className="max-w-48"
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
      </div>

      {venuesQuery.isLoading && <p className="text-muted-foreground">{t('common.loading')}</p>}
      {venuesQuery.isError && <p className="text-destructive">{t('common.requestFailed')}</p>}

      {venuesQuery.data && venuesQuery.data.items.length === 0 && (
        <p className="text-muted-foreground">{t('venues.noResults')}</p>
      )}

      <div className="flex flex-col gap-3">
        {venuesQuery.data?.items.map((venue) => (
          <Link key={venue.id} to={`/venues/${venue.id}`}>
            <Card className="transition-colors hover:bg-accent/50">
              <CardHeader>
                <CardTitle>{venue.name}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">
                  {venue.city}, {venue.address}
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

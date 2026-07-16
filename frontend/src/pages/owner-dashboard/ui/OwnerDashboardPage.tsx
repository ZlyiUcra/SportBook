import React from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { ApiRequestError } from '@/shared/api/axiosInstance'
import { searchVenues, getVenue } from '@/entities/venue/api/venueApi'
import type { VenueSummary } from '@/entities/venue/model/types'
import { VenueForm } from '@/features/venue-management/venue/ui/VenueForm'
import { createVenue, updateVenue, deleteVenue } from '@/features/venue-management/venue/api/venueManagementApi'
import type { VenueFormValues } from '@/features/venue-management/venue/model/schema'
import { CourtForm } from '@/features/venue-management/court/ui/CourtForm'
import { createCourt, updateCourt, deleteCourt } from '@/features/venue-management/court/api/courtManagementApi'
import type { EditCourtFormValues } from '@/features/venue-management/court/model/schema'

/** T051: create/edit/delete own venues and their courts. */
export function OwnerDashboardPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [creatingVenue, setCreatingVenue] = React.useState(false)
  const [editingVenueId, setEditingVenueId] = React.useState<string | null>(null)
  const [expandedVenueId, setExpandedVenueId] = React.useState<string | null>(null)
  const [creatingCourt, setCreatingCourt] = React.useState(false)
  const [editingCourtId, setEditingCourtId] = React.useState<string | null>(null)

  const myVenuesQuery = useQuery({
    queryKey: ['my-venues'],
    queryFn: () => searchVenues({ mine: true }),
  })

  const expandedVenueQuery = useQuery({
    queryKey: ['venue', expandedVenueId],
    queryFn: () => getVenue(expandedVenueId!),
    enabled: !!expandedVenueId,
  })

  function invalidateVenues() {
    queryClient.invalidateQueries({ queryKey: ['my-venues'] })
  }

  function invalidateExpandedVenue() {
    queryClient.invalidateQueries({ queryKey: ['venue', expandedVenueId] })
  }

  const createVenueMutation = useMutation({
    mutationFn: createVenue,
    onSuccess: () => {
      invalidateVenues()
      setCreatingVenue(false)
    },
  })

  const updateVenueMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: VenueFormValues }) => updateVenue(id, values),
    onSuccess: () => {
      invalidateVenues()
      setEditingVenueId(null)
    },
  })

  const deleteVenueMutation = useMutation({
    mutationFn: deleteVenue,
    onSuccess: invalidateVenues,
  })

  const createCourtMutation = useMutation({
    mutationFn: ({ venueId, values }: { venueId: string; values: EditCourtFormValues }) =>
      createCourt(venueId, values),
    onSuccess: () => {
      invalidateExpandedVenue()
      setCreatingCourt(false)
    },
  })

  const updateCourtMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: EditCourtFormValues }) => updateCourt(id, values),
    onSuccess: () => {
      invalidateExpandedVenue()
      setEditingCourtId(null)
    },
  })

  const deleteCourtMutation = useMutation({
    mutationFn: deleteCourt,
    onSuccess: invalidateExpandedVenue,
  })

  function handleDeleteVenue(venue: VenueSummary) {
    if (window.confirm(t('owner.venue.deleteConfirm', { name: venue.name }))) {
      deleteVenueMutation.mutate(venue.id)
    }
  }

  function handleDeleteCourt(courtId: string, courtName: string) {
    if (window.confirm(t('owner.court.deleteConfirm', { name: courtName }))) {
      deleteCourtMutation.mutate(courtId)
    }
  }

  function apiErrorMessage(error: unknown): string {
    return error instanceof ApiRequestError ? error.message : t('common.requestFailed')
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">{t('owner.title')}</h1>
        {!creatingVenue && <Button onClick={() => setCreatingVenue(true)}>{t('owner.addVenue')}</Button>}
      </div>

      {creatingVenue && (
        <Card>
          <CardHeader>
            <CardTitle>{t('owner.addVenue')}</CardTitle>
          </CardHeader>
          <CardContent>
            <VenueForm
              onSubmit={(values) => createVenueMutation.mutate(values)}
              onCancel={() => setCreatingVenue(false)}
              isSubmitting={createVenueMutation.isPending}
            />
            {createVenueMutation.isError && (
              <p role="alert" className="mt-2 text-sm text-destructive">
                {apiErrorMessage(createVenueMutation.error)}
              </p>
            )}
          </CardContent>
        </Card>
      )}

      {myVenuesQuery.isLoading && <p className="text-muted-foreground">{t('common.loading')}</p>}
      {myVenuesQuery.isError && <p className="text-destructive">{t('common.requestFailed')}</p>}
      {myVenuesQuery.data && myVenuesQuery.data.items.length === 0 && !creatingVenue && (
        <p className="text-muted-foreground">{t('owner.noVenues')}</p>
      )}

      <div className="flex flex-col gap-3">
        {myVenuesQuery.data?.items.map((venue) => (
          <Card key={venue.id}>
            <CardHeader>
              <CardTitle className="flex flex-wrap items-center justify-between gap-2">
                <span>
                  {venue.name} - {venue.city}
                </span>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setExpandedVenueId(expandedVenueId === venue.id ? null : venue.id)}
                  >
                    {expandedVenueId === venue.id ? t('owner.hideCourts') : t('owner.manageCourts')}
                  </Button>
                  <Button variant="outline" size="sm" onClick={() => setEditingVenueId(venue.id)}>
                    {t('common.edit')}
                  </Button>
                  <Button variant="outline" size="sm" onClick={() => handleDeleteVenue(venue)}>
                    {t('common.delete')}
                  </Button>
                </div>
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              <p className="text-sm text-muted-foreground">{venue.address}</p>

              {editingVenueId === venue.id && (
                <div className="rounded-md border p-3">
                  <VenueForm
                    defaultValues={{
                      name: venue.name,
                      city: venue.city,
                      address: venue.address,
                      description: venue.description ?? '',
                    }}
                    onSubmit={(values) => updateVenueMutation.mutate({ id: venue.id, values })}
                    onCancel={() => setEditingVenueId(null)}
                    isSubmitting={updateVenueMutation.isPending}
                  />
                  {updateVenueMutation.isError && (
                    <p role="alert" className="mt-2 text-sm text-destructive">
                      {apiErrorMessage(updateVenueMutation.error)}
                    </p>
                  )}
                </div>
              )}

              {expandedVenueId === venue.id && (
                <div className="flex flex-col gap-3 border-t pt-3">
                  {expandedVenueQuery.isLoading && (
                    <p className="text-muted-foreground">{t('common.loading')}</p>
                  )}
                  {expandedVenueQuery.data?.courts.length === 0 && !creatingCourt && (
                    <p className="text-muted-foreground">{t('owner.noCourts')}</p>
                  )}

                  {expandedVenueQuery.data?.courts.map((court) =>
                    editingCourtId === court.id ? (
                      <div key={court.id} className="rounded-md border p-3">
                        <CourtForm
                          mode="edit"
                          defaultValues={{
                            name: court.name,
                            sportType: court.sportType,
                            pricePerHour: court.pricePerHour,
                            openingTime: court.openingTime.slice(0, 5),
                            closingTime: court.closingTime.slice(0, 5),
                            isActive: court.isActive,
                          }}
                          onSubmit={(values) => updateCourtMutation.mutate({ id: court.id, values })}
                          onCancel={() => setEditingCourtId(null)}
                          isSubmitting={updateCourtMutation.isPending}
                        />
                        {updateCourtMutation.isError && (
                          <p role="alert" className="mt-2 text-sm text-destructive">
                            {apiErrorMessage(updateCourtMutation.error)}
                          </p>
                        )}
                      </div>
                    ) : (
                      <div
                        key={court.id}
                        className="flex flex-wrap items-center justify-between gap-2 rounded-md border p-3"
                      >
                        <span className="text-sm">
                          {court.name} - {t(`sport.${court.sportType}`)} -{' '}
                          {t('owner.court.pricePerHourValue', { price: court.pricePerHour })}
                          {!court.isActive && ` (${t('owner.court.inactive')})`}
                        </span>
                        <div className="flex gap-2">
                          <Button variant="outline" size="sm" onClick={() => setEditingCourtId(court.id)}>
                            {t('common.edit')}
                          </Button>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleDeleteCourt(court.id, court.name)}
                          >
                            {t('common.delete')}
                          </Button>
                        </div>
                      </div>
                    ),
                  )}

                  {creatingCourt ? (
                    <div className="rounded-md border p-3">
                      <CourtForm
                        mode="create"
                        onSubmit={(values) => createCourtMutation.mutate({ venueId: venue.id, values })}
                        onCancel={() => setCreatingCourt(false)}
                        isSubmitting={createCourtMutation.isPending}
                      />
                      {createCourtMutation.isError && (
                        <p role="alert" className="mt-2 text-sm text-destructive">
                          {apiErrorMessage(createCourtMutation.error)}
                        </p>
                      )}
                    </div>
                  ) : (
                    <Button variant="outline" size="sm" className="self-start" onClick={() => setCreatingCourt(true)}>
                      {t('owner.addCourt')}
                    </Button>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}

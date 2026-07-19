import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import '@/shared/i18n'
import { VenueSearchPage } from '@/pages/venues/ui/VenueSearchPage'
import { useSearchStore } from '@/pages/venues/model/searchStore'
import { getNearbyVenues } from '@/entities/venue/api/venueApi'
import type { NearbyVenue } from '@/entities/venue/model/types'
import type { City } from '@/entities/city/model/types'

// T006 (004 US1): the search restores from the session store across unmount/remount - the exact
// shape of "open a venue, come back". Map mocked per the research.md testing stance.
vi.mock('@/shared/ui/map/MapView', () => ({
  default: () => <div data-testid="mock-map" />,
}))

vi.mock('@/entities/venue/api/venueApi', () => ({
  getNearbyVenues: vi.fn(),
}))

vi.mock('@/entities/city/api/cityApi', () => ({
  suggestCities: vi.fn(),
}))

const city: City = {
  id: 703448,
  nameEn: 'Kyiv',
  nameUk: 'Київ',
  namePt: 'Kyiv',
  regionEn: 'Kyiv City',
  regionUk: 'Місто Київ',
  regionPt: 'Cidade de Kyiv',
  latitude: 50.45466,
  longitude: 30.5238,
}

function makeNearby(id: string, distanceKm: number): NearbyVenue {
  return { id, name: `Venue ${id}`, city, address: '1 St', description: null, latitude: 50.45, longitude: 30.52, distanceKm }
}

function stubGeolocation(latitude: number, longitude: number) {
  const getCurrentPosition = vi.fn((success: PositionCallback) => {
    success({ coords: { latitude, longitude } } as GeolocationPosition)
  })
  Object.defineProperty(navigator, 'geolocation', {
    configurable: true,
    value: { getCurrentPosition },
  })
  return getCurrentPosition
}

function renderPage() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <VenueSearchPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('VenueSearchPage - return-to-search state restore', () => {
  beforeEach(() => {
    vi.mocked(getNearbyVenues).mockReset()
    // The store is module-level session state - reset it so tests stay independent.
    useSearchStore.setState({ city: null, sportType: '', deviceCoords: null })
  })

  it('restores a near-me search across unmount/remount with exactly one geolocation call', async () => {
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('near-1', 0.5)])
    const getCurrentPosition = stubGeolocation(50.45466, 30.5238)

    const first = renderPage()
    fireEvent.click(screen.getByRole('button', { name: /near me/i }))
    await waitFor(() => expect(getNearbyVenues).toHaveBeenCalledWith(50.45, 30.52, undefined))
    expect(getCurrentPosition).toHaveBeenCalledTimes(1)

    // "Open a venue page": the search page unmounts entirely.
    first.unmount()
    vi.mocked(getNearbyVenues).mockClear()

    // "Back to search": a fresh mount restores from the store - same query, NO new prompt.
    renderPage()
    await waitFor(() => expect(getNearbyVenues).toHaveBeenCalledWith(50.45, 30.52, undefined))
    expect(getCurrentPosition).toHaveBeenCalledTimes(1)
    expect(await screen.findByText('Venue near-1')).toBeInTheDocument()
  })

  it('restores a selected city and sport filter across unmount/remount', async () => {
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('kyiv-1', 1)])
    useSearchStore.setState({ city, sportType: 'Tennis' })

    const first = renderPage()
    await waitFor(() => expect(getNearbyVenues).toHaveBeenCalledWith(city.latitude, city.longitude, 'Tennis'))
    first.unmount()
    vi.mocked(getNearbyVenues).mockClear()

    renderPage()
    await waitFor(() => expect(getNearbyVenues).toHaveBeenCalledWith(city.latitude, city.longitude, 'Tennis'))
    expect(screen.getByText('Kyiv')).toBeInTheDocument()
  })

  it('an empty store yields the default prompt state and no query', () => {
    renderPage()

    expect(getNearbyVenues).not.toHaveBeenCalled()
    expect(screen.getByText('Pick a city or use "near me" to see venues near you.')).toBeInTheDocument()
  })
})

import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import '@/shared/i18n'
import { VenueSearchPage } from '@/pages/venues/ui/VenueSearchPage'
import { useSearchStore } from '@/pages/venues/model/searchStore'
import { getNearbyVenues } from '@/entities/venue/api/venueApi'
import { suggestCities } from '@/entities/city/api/cityApi'
import type { MapMarker } from '@/shared/ui/map/MapView'
import type { NearbyVenue } from '@/entities/venue/model/types'
import type { City } from '@/entities/city/model/types'

// research.md testing stance: no leaflet/WebGL in jsdom - the real MapView is mocked with a thin
// stand-in that exposes markers (and which one is emphasized) as plain text.
vi.mock('@/shared/ui/map/MapView', () => ({
  default: ({ markers }: { markers: MapMarker[] }) => (
    <ul data-testid="mock-map-markers">
      {markers.map((marker) => (
        <li key={marker.id}>
          {marker.id}
          {marker.emphasized ? ' (nearest)' : ''}
        </li>
      ))}
    </ul>
  ),
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

const lvivCity: City = {
  id: 702550,
  nameEn: 'Lviv',
  nameUk: 'Львів',
  namePt: 'Lviv',
  regionEn: 'Lviv Oblast',
  regionUk: 'Львівська область',
  regionPt: 'Oblast de Lviv',
  latitude: 49.83826,
  longitude: 24.02324,
}

function makeNearby(id: string, distanceKm: number): NearbyVenue {
  return { id, name: `Venue ${id}`, city, address: '1 St', description: null, latitude: 50.45, longitude: 30.52, distanceKm }
}

function stubGeolocation(latitude: number, longitude: number) {
  Object.defineProperty(navigator, 'geolocation', {
    configurable: true,
    value: {
      getCurrentPosition: (success: PositionCallback) => {
        success({ coords: { latitude, longitude } } as GeolocationPosition)
      },
    },
  })
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

async function selectCity(matchedCity: City) {
  vi.mocked(suggestCities).mockResolvedValue([matchedCity])
  // The native sport <select> also carries an implicit "combobox" role - the CityCombobox trigger renders first.
  fireEvent.click(screen.getAllByRole('combobox')[0])
  fireEvent.change(screen.getByPlaceholderText(/type a city name/i), { target: { value: matchedCity.nameEn } })
  const option = await screen.findByRole('option', { name: new RegExp(matchedCity.nameEn, 'i') }, { timeout: 2000 })
  fireEvent.click(option)
}

describe('VenueSearchPage - reference-point radius view', () => {
  beforeEach(() => {
    vi.mocked(getNearbyVenues).mockReset()
    vi.mocked(suggestCities).mockReset()
    // Since 004 the search state is a module-level session store - reset it between tests.
    useSearchStore.setState({ city: null, sportType: '', deviceCoords: null })
  })

  it('T014: the near-me flow shows the in-range venues on the map and in the list nearest-first', async () => {
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('near-1', 0.5), makeNearby('near-2', 20)])
    stubGeolocation(50.45466, 30.5238)

    renderPage()
    fireEvent.click(screen.getByRole('button', { name: /near me/i }))

    // useGeolocation rounds to 2 decimals before the request (research.md privacy posture).
    await waitFor(() => expect(getNearbyVenues).toHaveBeenCalledWith(50.45, 30.52, undefined))

    await waitFor(() => expect(screen.getByTestId('mock-map-markers')).toBeInTheDocument())
    expect(screen.getByText('near-1 (nearest)')).toBeInTheDocument()
    expect(screen.getByText('near-2')).toBeInTheDocument()

    const listedNames = (await screen.findAllByText(/^Venue near-/)).map((el) => el.textContent)
    expect(listedNames).toEqual(['Venue near-1', 'Venue near-2'])
  })

  it('T016: selecting a city with no geolocation active drives the radius map and list', async () => {
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('lviv-1', 2)])

    renderPage()
    await selectCity(lvivCity)

    await waitFor(() => expect(getNearbyVenues).toHaveBeenCalledWith(lvivCity.latitude, lvivCity.longitude, undefined))
    await waitFor(() => expect(screen.getByTestId('mock-map-markers')).toBeInTheDocument())
    expect(screen.getByText('lviv-1 (nearest)')).toBeInTheDocument()
  })

  it('T016: device location takes precedence over a selected city when both exist', async () => {
    vi.mocked(getNearbyVenues).mockResolvedValue([])

    renderPage()
    await selectCity(lvivCity)
    await waitFor(() => expect(getNearbyVenues).toHaveBeenCalledWith(lvivCity.latitude, lvivCity.longitude, undefined))

    stubGeolocation(50.45466, 30.5238)
    fireEvent.click(screen.getByRole('button', { name: /near me/i }))

    await waitFor(() => expect(getNearbyVenues).toHaveBeenLastCalledWith(50.45, 30.52, undefined))
  })

  it('T018: no geolocation and no selected city renders no map block and shows the prompt', () => {
    renderPage()

    expect(getNearbyVenues).not.toHaveBeenCalled()
    expect(screen.queryByTestId('mock-map-markers')).not.toBeInTheDocument()
    expect(screen.getByText('Pick a city or use "near me" to see venues near you.')).toBeInTheDocument()
  })
})

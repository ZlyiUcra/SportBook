import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import '@/shared/i18n'
import { VenueSearchPage } from '@/pages/venues/ui/VenueSearchPage'
import { useSearchStore } from '@/pages/venues/model/searchStore'
import { getNearbyVenues } from '@/entities/venue/api/venueApi'
import { suggestCities } from '@/entities/city/api/cityApi'
import type { MapBounds, MapMarker, MapViewport } from '@/shared/ui/map/MapView'
import type { NearbyVenue } from '@/entities/venue/model/types'
import type { City } from '@/entities/city/model/types'

// Captures the page's onViewportChange so tests can emit MapBounds like a completed zoom/pan
// gesture would (004 US2). vi.hoisted because the mock factory below is hoisted above imports.
const viewport = vi.hoisted(() => ({
  emit: null as ((report: MapViewport) => void) | null,
}))

// research.md testing stance: no leaflet/WebGL in jsdom - the real MapView is mocked with a thin
// stand-in that exposes markers (and which one is emphasized) as plain text.
vi.mock('@/shared/ui/map/MapView', () => ({
  default: ({ markers, onViewportChange }: { markers: MapMarker[]; onViewportChange?: (report: MapViewport) => void }) => {
    viewport.emit = onViewportChange ?? null
    return (
      <ul data-testid="mock-map-markers">
        {markers.map((marker) => (
          <li key={marker.id}>
            {marker.id}
            {marker.emphasized ? ' (nearest)' : ''}
          </li>
        ))}
      </ul>
    )
  },
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

function makeNearby(id: string, distanceKm: number, latitude = 50.45, longitude = 30.52): NearbyVenue {
  return { id, name: `Venue ${id}`, city, address: '1 St', description: null, latitude, longitude, distanceKm }
}

/** Emits a viewport report the way a completed zoom/pan gesture would - see the MapView mock above.
 * The 003 tests only vary `bounds` (the list/count filter); center+zoom are filler, since the radius
 * view does not assert the restorable camera (that arrived with 008's richer report shape). */
function emitViewport(bounds: MapBounds) {
  act(() => {
    viewport.emit?.({ bounds, center: { lat: 50.45, lng: 30.52 }, zoom: 13 })
  })
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
    useSearchStore.setState({ city: null, sportType: '', deviceCoords: null, viewport: null })
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

  it('T008: the list follows the viewport - zoom in narrows it, zoom out widens it back', async () => {
    // near-1 sits at the reference; far-2 is ~17km north - both in range, spatially apart.
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('near-1', 0.5), makeNearby('far-2', 17, 50.6)])
    stubGeolocation(50.45466, 30.5238)

    renderPage()
    fireEvent.click(screen.getByRole('button', { name: /near me/i }))
    await waitFor(() => expect(screen.getByTestId('mock-map-markers')).toBeInTheDocument())

    // Before any viewport report: the full in-range set (spec FR-009).
    expect(screen.getByText('Venue near-1')).toBeInTheDocument()
    expect(screen.getByText('Venue far-2')).toBeInTheDocument()

    // "Zoom into" the northern area - only far-2 lies inside.
    emitViewport({ south: 50.55, north: 50.65, west: 30.4, east: 30.6 })
    expect(screen.queryByText('Venue near-1')).not.toBeInTheDocument()
    expect(screen.getByText('Venue far-2')).toBeInTheDocument()
    // The map still shows the FULL set and emphasis stays the overall nearest (FR-011, FR-014).
    expect(screen.getByText('near-1 (nearest)')).toBeInTheDocument()

    // "Zoom back out" - the list widens again.
    emitViewport({ south: 50.3, north: 50.7, west: 30.3, east: 30.7 })
    expect(screen.getByText('Venue near-1')).toBeInTheDocument()
    expect(screen.getByText('Venue far-2')).toBeInTheDocument()
  })

  it('T008: an empty viewport shows "no venues in view", distinct from the no-results state', async () => {
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('near-1', 0.5)])
    stubGeolocation(50.45466, 30.5238)

    renderPage()
    fireEvent.click(screen.getByRole('button', { name: /near me/i }))
    await waitFor(() => expect(screen.getByTestId('mock-map-markers')).toBeInTheDocument())

    emitViewport({ south: 51.5, north: 51.6, west: 31.5, east: 31.6 })

    expect(screen.getByText('No venues in the visible map area. Zoom out or move the map.')).toBeInTheDocument()
    expect(screen.queryByText('No venues found.')).not.toBeInTheDocument()
    expect(screen.queryByText('Venue near-1')).not.toBeInTheDocument()
  })

  it('T010: 11+ visible venues page at 10 nearest-first and viewport change resets to page 1', async () => {
    const many = Array.from({ length: 12 }, (_, i) => makeNearby(`v-${String(i + 1).padStart(2, '0')}`, i + 1))
    vi.mocked(getNearbyVenues).mockResolvedValue(many)
    stubGeolocation(50.45466, 30.5238)

    renderPage()
    fireEvent.click(screen.getByRole('button', { name: /near me/i }))
    await waitFor(() => expect(screen.getByTestId('mock-map-markers')).toBeInTheDocument())

    // Page 1: the 10 nearest; the map still shows all 12 markers (spec FR-014).
    expect(screen.getByText('Venue v-01')).toBeInTheDocument()
    expect(screen.getByText('Venue v-10')).toBeInTheDocument()
    expect(screen.queryByText('Venue v-11')).not.toBeInTheDocument()
    expect(screen.getByText('v-12')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /next/i }))
    expect(screen.getByText('Venue v-11')).toBeInTheDocument()
    expect(screen.getByText('Venue v-12')).toBeInTheDocument()
    expect(screen.queryByText('Venue v-01')).not.toBeInTheDocument()

    // A completed gesture resets to page 1 (spec FR-013).
    emitViewport({ south: 50.0, north: 51.0, west: 30.0, east: 31.0 })
    expect(screen.getByText('Venue v-01')).toBeInTheDocument()
    expect(screen.queryByText('Venue v-11')).not.toBeInTheDocument()
  })

  it('T010: changing the sport filter resets to page 1 and 10 or fewer venues show no controls', async () => {
    const many = Array.from({ length: 12 }, (_, i) => makeNearby(`v-${String(i + 1).padStart(2, '0')}`, i + 1))
    vi.mocked(getNearbyVenues).mockResolvedValue(many)
    stubGeolocation(50.45466, 30.5238)

    renderPage()
    fireEvent.click(screen.getByRole('button', { name: /near me/i }))
    await waitFor(() => expect(screen.getByTestId('mock-map-markers')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: /next/i }))
    expect(screen.getByText('Venue v-11')).toBeInTheDocument()

    // Sport change refetches (fewer results) and resets to page 1 - controls disappear at <= 10.
    vi.mocked(getNearbyVenues).mockResolvedValue(many.slice(0, 3))
    fireEvent.change(screen.getByRole('combobox', { name: /sport/i }), { target: { value: 'Tennis' } })
    await waitFor(() => expect(getNearbyVenues).toHaveBeenLastCalledWith(50.45, 30.52, 'Tennis'))

    expect(await screen.findByText('Venue v-01')).toBeInTheDocument()
    expect(screen.queryByText('Venue v-11')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /next/i })).not.toBeInTheDocument()
  })
})

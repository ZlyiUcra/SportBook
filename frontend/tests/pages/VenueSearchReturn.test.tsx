import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import '@/shared/i18n'
import { VenueSearchPage } from '@/pages/venues/ui/VenueSearchPage'
import { useSearchStore } from '@/pages/venues/model/searchStore'
import { getNearbyVenues } from '@/entities/venue/api/venueApi'
import type { NearbyVenue } from '@/entities/venue/model/types'
import type { City } from '@/entities/city/model/types'
import type { MapViewport } from '@/shared/ui/map/MapView'
import i18n from 'i18next'

// Captures the props VenueSearchPage passes to MapView so the 008 restore-path tests can assert
// center/zoom/fitBoundsKey and invoke the viewport report callback. Hoisted so the (also hoisted)
// mock factory can reference it. The 004 restore tests below ignore these props.
const { mapProps } = vi.hoisted(() => ({
  mapProps: {
    current: null as null | {
      center?: { lat: number; lng: number }
      zoom?: number
      fitBoundsKey?: string
      onViewportChange?: (report: MapViewport) => void
    },
  },
}))

vi.mock('@/shared/ui/map/MapView', () => ({
  default: (props: {
    center?: { lat: number; lng: number }
    zoom?: number
    fitBoundsKey?: string
    onViewportChange?: (report: MapViewport) => void
  }) => {
    mapProps.current = props
    return <div data-testid="mock-map" />
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
  nameEs: 'Kyiv',
  regionEn: 'Kyiv City',
  regionUk: 'Місто Київ',
  regionPt: 'Cidade de Kyiv',
  regionEs: 'Ciudad de Kyiv',
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
    useSearchStore.setState({ city: null, sportType: '', deviceCoords: null, viewport: null })
    mapProps.current = null
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

describe('VenueSearchPage - viewport preservation (008)', () => {
  beforeEach(() => {
    vi.mocked(getNearbyVenues).mockReset()
    useSearchStore.setState({ city: null, sportType: '', deviceCoords: null, viewport: null })
    mapProps.current = null
  })

  it('passes the saved viewport center/zoom to MapView and suppresses auto-framing', async () => {
    useSearchStore.setState({
      city,
      viewport: { lat: 49.9, lng: 30.1, zoom: 15 },
    })
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('v1', 1)])

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    expect(mapProps.current?.center).toEqual({ lat: 49.9, lng: 30.1 })
    expect(mapProps.current?.zoom).toBe(15)
    // No fitBoundsKey while a viewport is being restored - MapContainer mounts at the saved view
    // and FitBounds is not rendered (008 reversal of 004 FR-004).
    expect(mapProps.current?.fitBoundsKey).toBeUndefined()
  })

  it('frames the in-range set (reference fitBoundsKey) when no viewport is saved', async () => {
    useSearchStore.setState({ city, viewport: null })
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('v1', 1)])

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    // Reference-only key (venue ids no longer part of it, 008) so a sport change does not reframe.
    expect(mapProps.current?.fitBoundsKey).toBe(`${city.latitude},${city.longitude}`)
  })

  it('saves the viewport to the store when MapView reports it', async () => {
    useSearchStore.setState({ city, viewport: null })
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('v1', 1)])

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    act(() => {
      mapProps.current?.onViewportChange?.({
        bounds: { south: 50, west: 30, north: 51, east: 31 },
        center: { lat: 50.5, lng: 30.5 },
        zoom: 14,
      })
    })

    expect(useSearchStore.getState().viewport).toEqual({ lat: 50.5, lng: 30.5, zoom: 14 })
  })

  it('clears the saved viewport when the reference point changes (new search)', async () => {
    useSearchStore.setState({ city, viewport: { lat: 49.9, lng: 30.1, zoom: 15 } })
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('v1', 1)])

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    // Change the reference: drop the city, set a device location at a different point.
    useSearchStore.setState({ city: null, deviceCoords: { lat: 51.0, lng: 31.0 } })
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('v2', 2)])

    await waitFor(() => expect(useSearchStore.getState().viewport).toBeNull())
  })

  it('keeps the saved viewport when only the sport filter changes', async () => {
    useSearchStore.setState({ city, sportType: '', viewport: { lat: 49.9, lng: 30.1, zoom: 15 } })
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('v1', 1)])

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    useSearchStore.setState({ sportType: 'Tennis' })

    // Same reference -> camera survives (008 FR-002); fitBoundsKey stays suppressed.
    expect(useSearchStore.getState().viewport).toEqual({ lat: 49.9, lng: 30.1, zoom: 15 })
    expect(mapProps.current?.fitBoundsKey).toBeUndefined()
  })
})

describe('VenueSearchPage - pagination restore (013)', () => {
  beforeEach(() => {
    vi.mocked(getNearbyVenues).mockReset()
    useSearchStore.setState({ city: null, sportType: '', deviceCoords: null, viewport: null, page: 1 })
    mapProps.current = null
  })

  it('keeps a restored page across remount - the map settling to its restored viewport is not a real change', async () => {
    const many = Array.from({ length: 12 }, (_, i) => makeNearby(`v-${String(i + 1).padStart(2, '0')}`, i + 1))
    vi.mocked(getNearbyVenues).mockResolvedValue(many)
    useSearchStore.setState({ city, viewport: { lat: 49.9, lng: 30.1, zoom: 15 }, page: 2 })

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    // The map's mandatory once-on-mount report (MapView.tsx contract) settling at the restored
    // viewport - must NOT reset the restored page.
    act(() => {
      mapProps.current?.onViewportChange?.({
        bounds: { south: 49, west: 29, north: 51, east: 31 },
        center: { lat: 49.9, lng: 30.1 },
        zoom: 15,
      })
    })
    expect(useSearchStore.getState().page).toBe(2)
    expect(screen.getByText('Venue v-11')).toBeInTheDocument()

    // A genuine subsequent pan still resets to page 1 (spec FR-013 behavior, unchanged).
    act(() => {
      mapProps.current?.onViewportChange?.({
        bounds: { south: 40, west: 20, north: 41, east: 21 },
        center: { lat: 40.5, lng: 20.5 },
        zoom: 13,
      })
    })
    expect(useSearchStore.getState().page).toBe(1)
  })

  it('resets to page 1 on a fresh search with no restored viewport (unchanged pre-013 behavior)', async () => {
    const many = Array.from({ length: 12 }, (_, i) => makeNearby(`v-${String(i + 1).padStart(2, '0')}`, i + 1))
    vi.mocked(getNearbyVenues).mockResolvedValue(many)
    useSearchStore.setState({ city, viewport: null, page: 2 })

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    // No prior viewport to restore - the map's first-ever report is a genuine new observation.
    act(() => {
      mapProps.current?.onViewportChange?.({
        bounds: { south: 49, west: 29, north: 51, east: 31 },
        center: { lat: 50, lng: 30 },
        zoom: 13,
      })
    })
    expect(useSearchStore.getState().page).toBe(1)
  })
})

describe('VenueSearchPage - visible-venue count (008 US2)', () => {
  beforeEach(() => {
    vi.mocked(getNearbyVenues).mockReset()
    useSearchStore.setState({ city: null, sportType: '', deviceCoords: null, viewport: null })
    mapProps.current = null
  })

  afterEach(async () => {
    // The plural test switches language - restore the default so later files stay deterministic.
    await i18n.changeLanguage('en')
  })

  it('shows the count of venues visible in the viewport above the list', async () => {
    useSearchStore.setState({ city })
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('v1', 1), makeNearby('v2', 2)])

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    // Both venues sit at (50.45, 30.52) - bounds that contain them make both visible.
    act(() => {
      mapProps.current?.onViewportChange?.({
        bounds: { south: 50, west: 30, north: 51, east: 31 },
        center: { lat: 50.5, lng: 30.5 },
        zoom: 13,
      })
    })

    expect(screen.getByText('2 venues visible')).toBeInTheDocument()
  })

  it('updates to zero when the viewport no longer contains any venue', async () => {
    useSearchStore.setState({ city })
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('v1', 1), makeNearby('v2', 2)])

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    act(() => {
      mapProps.current?.onViewportChange?.({
        bounds: { south: 50, west: 30, north: 51, east: 31 },
        center: { lat: 50.5, lng: 30.5 },
        zoom: 13,
      })
    })
    expect(screen.getByText('2 venues visible')).toBeInTheDocument()

    // Pan to an area with no venues.
    act(() => {
      mapProps.current?.onViewportChange?.({
        bounds: { south: 40, west: 20, north: 41, east: 21 },
        center: { lat: 40.5, lng: 20.5 },
        zoom: 13,
      })
    })

    expect(screen.getByText('0 venues visible')).toBeInTheDocument()
  })

  it('uses the singular plural form for one visible venue (en)', async () => {
    useSearchStore.setState({ city })
    vi.mocked(getNearbyVenues).mockResolvedValue([makeNearby('v1', 1)])

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    act(() => {
      mapProps.current?.onViewportChange?.({
        bounds: { south: 50, west: 30, north: 51, east: 31 },
        center: { lat: 50.5, lng: 30.5 },
        zoom: 13,
      })
    })

    expect(screen.getByText('1 venue visible')).toBeInTheDocument()
  })

  it('uses the Ukrainian plural form (many) for five visible venues', async () => {
    useSearchStore.setState({ city })
    vi.mocked(getNearbyVenues).mockResolvedValue([
      makeNearby('v1', 1),
      makeNearby('v2', 2),
      makeNearby('v3', 3),
      makeNearby('v4', 4),
      makeNearby('v5', 5),
    ])

    renderPage()
    await waitFor(() => expect(screen.getByTestId('mock-map')).toBeInTheDocument())

    act(() => {
      mapProps.current?.onViewportChange?.({
        bounds: { south: 50, west: 30, north: 51, east: 31 },
        center: { lat: 50.5, lng: 30.5 },
        zoom: 13,
      })
    })

    await i18n.changeLanguage('uk')
    expect(await screen.findByText('5 видимих майданчиків')).toBeInTheDocument()
  })
})

import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import '@/shared/i18n'
import { VenueSearchMap } from '@/pages/venues/ui/VenueSearchMap'
import type { MapMarker } from '@/shared/ui/map/MapView'
import type { VenueSummary } from '@/entities/venue/model/types'

// research.md testing stance: no leaflet/WebGL in jsdom - the real MapView is mocked with a
// thin stand-in that just lists marker ids, so this stays a smoke test of VenueSearchMap's own
// filtering logic, not of Leaflet rendering.
vi.mock('@/shared/ui/map/MapView', () => ({
  default: ({ markers }: { markers: MapMarker[] }) => (
    <ul data-testid="mock-map-markers">
      {markers.map((marker) => (
        <li key={marker.id}>{marker.id}</li>
      ))}
    </ul>
  ),
}))

const city = {
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

function makeVenue(id: string, latitude: number | null, longitude: number | null): VenueSummary {
  return { id, name: `Venue ${id}`, city, address: '1 St', description: null, latitude, longitude }
}

describe('VenueSearchMap', () => {
  it('shows exactly the pinned venues of the current results page', async () => {
    const venues = [
      makeVenue('pinned-1', 50.45, 30.52),
      makeVenue('unpinned', null, null),
      makeVenue('pinned-2', 50.46, 30.53),
    ]

    render(
      <MemoryRouter>
        <VenueSearchMap venues={venues} />
      </MemoryRouter>,
    )

    fireEvent.click(screen.getByRole('button', { name: /show map/i }))

    await waitFor(() => expect(screen.getByTestId('mock-map-markers')).toBeInTheDocument())
    expect(screen.getByText('pinned-1')).toBeInTheDocument()
    expect(screen.getByText('pinned-2')).toBeInTheDocument()
    expect(screen.queryByText('unpinned')).not.toBeInTheDocument()
  })

  it('renders nothing when no venue on the page has a precise location', () => {
    const venues = [makeVenue('unpinned-1', null, null), makeVenue('unpinned-2', null, null)]

    const { container } = render(
      <MemoryRouter>
        <VenueSearchMap venues={venues} />
      </MemoryRouter>,
    )

    expect(container).toBeEmptyDOMElement()
  })
})

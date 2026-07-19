import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import '@/shared/i18n'
import { MyBookingsPage } from '@/pages/my-bookings/ui/MyBookingsPage'
import { listMyBookings } from '@/entities/booking/api/bookingApi'
import type { PagedResponse } from '@/shared/api/types'
import type { Booking } from '@/entities/booking/model/types'
import type { City } from '@/entities/city/model/types'

// Keep the real bookingStatusFilters constant (drives the tabs) and mock only the network call.
vi.mock('@/entities/booking/api/bookingApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/entities/booking/api/bookingApi')>()
  return { ...actual, listMyBookings: vi.fn() }
})

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

function makeBooking(id: string): Booking {
  return {
    id,
    courtId: `court-${id}`,
    userId: 'user-1',
    startTime: '2026-08-01T10:00:00Z',
    endTime: '2026-08-01T11:00:00Z',
    status: 'Confirmed',
    totalPrice: 300,
    venueName: 'Sport Arena',
    city,
    sport: 'Tennis',
    courtName: 'Center Court',
  }
}

function paged(items: Booking[], totalCount = items.length, pageSize = 20): PagedResponse<Booking> {
  return { items, page: 1, pageSize, totalCount }
}

function renderPage() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <MyBookingsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('MyBookingsPage', () => {
  beforeEach(() => {
    vi.mocked(listMyBookings).mockReset()
  })

  it('T007: a booking row shows venue, city, sport, and court alongside status and price', async () => {
    vi.mocked(listMyBookings).mockResolvedValue(paged([makeBooking('b1')]))

    renderPage()

    expect(await screen.findByText('Sport Arena')).toBeInTheDocument()
    // City, sport, and court on one line; no raw court id shown.
    expect(screen.getByText(/Kyiv, Tennis - Center Court/)).toBeInTheDocument()
    expect(screen.getByText(/Confirmed/)).toBeInTheDocument()
    expect(screen.getByText(/Total: 300/)).toBeInTheDocument()
    expect(screen.queryByText(/court-b1/)).not.toBeInTheDocument()
  })

  it('T014: selecting a filter requests that status and an empty result shows the filter empty state', async () => {
    vi.mocked(listMyBookings).mockResolvedValue(paged([]))

    renderPage()
    await waitFor(() => expect(listMyBookings).toHaveBeenCalledWith('All', 1))

    fireEvent.click(screen.getByRole('button', { name: 'Upcoming' }))
    await waitFor(() => expect(listMyBookings).toHaveBeenLastCalledWith('Upcoming', 1))

    // Empty under a non-All filter shows the distinct "none in filter" message.
    expect(await screen.findByText('No bookings match this filter.')).toBeInTheDocument()
    expect(screen.queryByText('You have no bookings yet.')).not.toBeInTheDocument()
  })

  it('T016: Prev/Next page and disable at the ends, and changing the filter resets to page 1', async () => {
    // 3 items over a page size of 2 -> two pages.
    vi.mocked(listMyBookings).mockResolvedValue(paged([makeBooking('b1'), makeBooking('b2')], 3, 2))

    renderPage()
    await screen.findByText('1 / 2')

    const prev = screen.getByRole('button', { name: 'Previous' })
    const next = screen.getByRole('button', { name: 'Next' })
    expect(prev).toBeDisabled()
    expect(next).toBeEnabled()

    fireEvent.click(next)
    await waitFor(() => expect(listMyBookings).toHaveBeenLastCalledWith('All', 2))

    // Changing the filter resets to page 1 (spec FR-008).
    fireEvent.click(screen.getByRole('button', { name: 'Cancelled' }))
    await waitFor(() => expect(listMyBookings).toHaveBeenLastCalledWith('Cancelled', 1))
  })
})

import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import '@/shared/i18n'
import { MyBookingsPage } from '@/pages/my-bookings/ui/MyBookingsPage'
import { listMyBookings } from '@/entities/booking/api/bookingApi'
import { listReviews } from '@/entities/review/api/reviewApi'
import { createReview } from '@/features/review/create/api/createReview'
import { useSessionStore } from '@/entities/session/model/store'
import type { PagedResponse } from '@/shared/api/types'
import type { Booking } from '@/entities/booking/model/types'
import type { City } from '@/entities/city/model/types'
import type { Review } from '@/entities/review/model/types'

// Keep the real bookingStatusFilters constant (drives the tabs) and mock only the network call.
vi.mock('@/entities/booking/api/bookingApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/entities/booking/api/bookingApi')>()
  return { ...actual, listMyBookings: vi.fn() }
})

vi.mock('@/entities/review/api/reviewApi', () => ({
  listReviews: vi.fn(),
}))

vi.mock('@/features/review/create/api/createReview', () => ({
  createReview: vi.fn(),
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

function makeBooking(id: string, status: Booking['status'] = 'Confirmed'): Booking {
  return {
    id,
    courtId: `court-${id}`,
    userId: 'user-1',
    startTime: '2026-08-01T10:00:00Z',
    endTime: '2026-08-01T11:00:00Z',
    status,
    totalPrice: 300,
    venueName: 'Sport Arena',
    city,
    sport: 'Tennis',
    courtName: 'Center Court',
    venueId: `venue-${id}`,
  }
}

function paged(items: Booking[], totalCount = items.length, pageSize = 20): PagedResponse<Booking> {
  return { items, page: 1, pageSize, totalCount }
}

function makeReview(createdAt: string): Review {
  return {
    id: 'review-1',
    venueId: 'venue-b1',
    userId: 'user-1',
    userName: 'Me',
    rating: 4,
    comment: 'A perfectly fine comment',
    createdAt,
  }
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
    useSessionStore.setState({ user: null })
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

  it('T009 (006): the review action shows only on a Completed booking', async () => {
    vi.mocked(listMyBookings).mockResolvedValue(
      paged([makeBooking('b1', 'Completed'), makeBooking('b2', 'Confirmed')]),
    )

    renderPage()
    await screen.findAllByText('Sport Arena')

    const reviewActions = screen.getAllByRole('button', { name: 'Leave a review' })
    expect(reviewActions).toHaveLength(1)
  })

  it('T009 (006): submitting the review entry posts to that booking\'s venue', async () => {
    vi.mocked(listMyBookings).mockResolvedValue(paged([makeBooking('b1', 'Completed')]))
    vi.mocked(listReviews).mockResolvedValue({ items: [], page: 1, pageSize: 20, totalCount: 0 })
    vi.mocked(createReview).mockResolvedValue({
      id: 'review-1',
      venueId: 'venue-b1',
      userId: 'user-1',
      userName: 'Me',
      rating: 5,
      comment: '',
      createdAt: '2026-08-02T00:00:00Z',
    })

    renderPage()
    await screen.findByText('Sport Arena')

    fireEvent.click(screen.getByRole('button', { name: 'Leave a review' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Submit review' }))

    await waitFor(() => expect(createReview).toHaveBeenCalledWith('venue-b1', expect.anything()))
  })

  it('T005 (007): a review created less than 24h ago still shows the edit form', async () => {
    useSessionStore.setState({ user: { id: 'user-1', name: 'Me', email: 'me@example.com', role: 'Customer', subscriptionTier: 'Free', createdAt: '2026-01-01T00:00:00Z' } })
    vi.mocked(listMyBookings).mockResolvedValue(paged([makeBooking('b1', 'Completed')]))
    const recentCreatedAt = new Date(Date.now() - 23 * 60 * 60 * 1000).toISOString()
    vi.mocked(listReviews).mockResolvedValue({ items: [makeReview(recentCreatedAt)], page: 1, pageSize: 20, totalCount: 1 })

    renderPage()
    await screen.findByText('Sport Arena')
    fireEvent.click(await screen.findByRole('button', { name: 'Edit your review' }))

    expect(await screen.findByRole('button', { name: 'Submit review' })).toBeInTheDocument()
  })

  it('T005 (007): a review created more than 24h ago shows read-only with no edit form', async () => {
    useSessionStore.setState({ user: { id: 'user-1', name: 'Me', email: 'me@example.com', role: 'Customer', subscriptionTier: 'Free', createdAt: '2026-01-01T00:00:00Z' } })
    vi.mocked(listMyBookings).mockResolvedValue(paged([makeBooking('b1', 'Completed')]))
    const oldCreatedAt = new Date(Date.now() - 25 * 60 * 60 * 1000).toISOString()
    vi.mocked(listReviews).mockResolvedValue({ items: [makeReview(oldCreatedAt)], page: 1, pageSize: 20, totalCount: 1 })

    renderPage()
    await screen.findByText('Sport Arena')
    fireEvent.click(await screen.findByRole('button', { name: 'View your review' }))

    expect(await screen.findByText('A perfectly fine comment')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Submit review' })).not.toBeInTheDocument()
  })
})

import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import '@/shared/i18n'
import { VenueDetailPage } from '@/pages/venue-detail/ui/VenueDetailPage'
import { getVenue } from '@/entities/venue/api/venueApi'
import { listReviews } from '@/entities/review/api/reviewApi'
import type { VenueDetail } from '@/entities/venue/model/types'
import type { Review } from '@/entities/review/model/types'
import type { PagedResponse } from '@/shared/api/types'
import type { City } from '@/entities/city/model/types'

// T009 (006): the venue page keeps the review LIST (social proof) but never offers a submission
// form - reviews are now reached only from a completed booking on My bookings.
vi.mock('@/shared/ui/map/MapView', () => ({
  default: () => <div data-testid="mock-map" />,
}))

vi.mock('@/entities/venue/api/venueApi', () => ({
  getVenue: vi.fn(),
}))

vi.mock('@/entities/review/api/reviewApi', () => ({
  listReviews: vi.fn(),
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

function makeVenue(): VenueDetail {
  return {
    id: 'venue-1',
    name: 'Sport Arena',
    city,
    address: '1 St',
    description: null,
    latitude: null,
    longitude: null,
    ownerId: 'owner-1',
    courts: [],
    averageRating: 4.5,
    reviewCount: 1,
  }
}

function makeReview(): Review {
  return {
    id: 'review-1',
    venueId: 'venue-1',
    userId: 'reviewer-1',
    userName: 'Alex',
    rating: 5,
    comment: 'Great court',
    createdAt: '2026-07-01T00:00:00Z',
  }
}

function paged(items: Review[]): PagedResponse<Review> {
  return { items, page: 1, pageSize: 20, totalCount: items.length }
}

function renderPage() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/venues/venue-1']}>
        <Routes>
          <Route path="/venues/:id" element={<VenueDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('VenueDetailPage - review list stays, submission form is gone', () => {
  beforeEach(() => {
    vi.mocked(getVenue).mockReset()
    vi.mocked(listReviews).mockReset()
  })

  it('renders the review list but no submission form', async () => {
    vi.mocked(getVenue).mockResolvedValue(makeVenue())
    vi.mocked(listReviews).mockResolvedValue(paged([makeReview()]))

    renderPage()

    expect(await screen.findByText(/Alex/)).toBeInTheDocument()
    expect(screen.getByText('Great court')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Submit review' })).not.toBeInTheDocument()
    expect(screen.queryByText('Leave a review')).not.toBeInTheDocument()
    expect(screen.queryByText('Update your review')).not.toBeInTheDocument()
  })

  it('shows the empty-reviews message with no submission form when a venue has none', async () => {
    vi.mocked(getVenue).mockResolvedValue(makeVenue())
    vi.mocked(listReviews).mockResolvedValue(paged([]))

    renderPage()

    expect(await screen.findByText('No reviews yet.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Submit review' })).not.toBeInTheDocument()
  })
})

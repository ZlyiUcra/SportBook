import { useSearchStore } from '@/pages/venues/model/searchStore'

// T001 (008 US1): the viewport field holds the restorable map camera and is independent of the
// other search inputs. Pure store mechanics - the page-level restore/clear behaviour is covered in
// VenueSearchReturn.test.tsx.

describe('useSearchStore - viewport (008)', () => {
  beforeEach(() => {
    useSearchStore.setState({
      city: null,
      sportType: '',
      deviceCoords: null,
      viewport: null,
    })
  })

  it('starts with a null viewport', () => {
    expect(useSearchStore.getState().viewport).toBeNull()
  })

  it('setViewport stores and clears the viewport', () => {
    useSearchStore.getState().setViewport({ lat: 50, lng: 30, zoom: 14 })

    expect(useSearchStore.getState().viewport).toEqual({ lat: 50, lng: 30, zoom: 14 })

    useSearchStore.getState().setViewport(null)

    expect(useSearchStore.getState().viewport).toBeNull()
  })

  it('setViewport leaves the other search fields untouched', () => {
    useSearchStore.setState({ sportType: 'Tennis', deviceCoords: { lat: 1, lng: 2 } })

    useSearchStore.getState().setViewport({ lat: 50, lng: 30, zoom: 14 })

    const state = useSearchStore.getState()
    expect(state.sportType).toBe('Tennis')
    expect(state.deviceCoords).toEqual({ lat: 1, lng: 2 })
  })
})

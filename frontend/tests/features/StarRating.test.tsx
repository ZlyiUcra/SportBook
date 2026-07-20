import { fireEvent, render, screen } from '@testing-library/react'
import '@/shared/i18n'
import { StarRating } from '@/features/review/create/ui/StarRating'

// T012 (006 US3): hover previews a value, clicking sets 1-5 by star position, and the control is
// keyboard-operable across the full range.
describe('StarRating', () => {
  it('previews three stars on hovering the third star', () => {
    render(<StarRating value={1} onChange={() => {}} />)

    const stars = screen.getAllByRole('radio')
    fireEvent.mouseEnter(stars[2])

    expect(stars[0]).toHaveAccessibleName('Rate 1 out of 5')
    const filled = stars.filter((s) => s.querySelector('svg')?.classList.contains('fill-yellow-400'))
    expect(filled).toHaveLength(3)
  })

  it('clicking the fourth star yields 4, and near the right edge yields 5', () => {
    const onChange = vi.fn()
    render(<StarRating value={1} onChange={onChange} />)

    const stars = screen.getAllByRole('radio')
    fireEvent.click(stars[3])
    expect(onChange).toHaveBeenLastCalledWith(4)

    fireEvent.click(stars[4])
    expect(onChange).toHaveBeenLastCalledWith(5)
  })

  it('is settable by keyboard across the full 1-5 range', () => {
    const onChange = vi.fn()
    const { rerender } = render(<StarRating value={3} onChange={onChange} />)

    const group = screen.getByRole('radiogroup')
    fireEvent.keyDown(group, { key: 'ArrowRight' })
    expect(onChange).toHaveBeenLastCalledWith(4)

    rerender(<StarRating value={1} onChange={onChange} />)
    fireEvent.keyDown(group, { key: 'ArrowLeft' })
    expect(onChange).toHaveBeenLastCalledWith(1)

    fireEvent.keyDown(group, { key: '5' })
    expect(onChange).toHaveBeenLastCalledWith(5)
  })
})

import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import '@/shared/i18n'
import { ReviewForm } from '@/features/review/create/ui/ReviewForm'

// T010 (007): the min-10-character, non-empty comment rule applies only when isEdit is true; a
// first-time submission (isEdit false/omitted) keeps the comment optional.
describe('ReviewForm', () => {
  it('blocks an empty comment in edit mode', async () => {
    const onSubmit = vi.fn()
    render(
      <ReviewForm
        isEdit
        defaultValues={{ rating: 4, comment: '' }}
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Submit review' }))

    expect(await screen.findByText('Editing a review requires a comment of at least 10 characters.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('blocks a 9-character comment in edit mode', async () => {
    const onSubmit = vi.fn()
    render(
      <ReviewForm
        isEdit
        defaultValues={{ rating: 4, comment: 'Too short' }}
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Submit review' }))

    expect(await screen.findByText('Editing a review requires a comment of at least 10 characters.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('accepts a 10+ character comment in edit mode', async () => {
    const onSubmit = vi.fn()
    render(
      <ReviewForm
        isEdit
        defaultValues={{ rating: 4, comment: 'A sufficiently long comment' }}
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Submit review' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalled())
  })

  it('accepts no comment at all for a first-time submission (isEdit false)', async () => {
    const onSubmit = vi.fn()
    render(<ReviewForm defaultValues={{ rating: 4, comment: '' }} onSubmit={onSubmit} isSubmitting={false} />)

    fireEvent.click(screen.getByRole('button', { name: 'Submit review' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalled())
  })
})

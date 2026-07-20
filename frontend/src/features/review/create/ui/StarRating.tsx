import React from 'react'
import { Star } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/shared/lib/utils'

const VALUES = [1, 2, 3, 4, 5] as const

type StarRatingProps = {
  value: number
  onChange: (value: number) => void
}

/**
 * Five-star rating control (006 US3) - the row is five equal segments (research.md): each star is
 * its own focusable radio button, so hovering/clicking/tapping a star previews or picks that star's
 * value directly, and native radio keyboard navigation (arrow keys move focus, the focused star is
 * the only tab stop) covers the full 1-5 range without custom position math.
 */
export function StarRating({ value, onChange }: StarRatingProps) {
  const { t } = useTranslation()
  const [hover, setHover] = React.useState<number | null>(null)
  const displayed = hover ?? value

  function handleKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'ArrowRight' || event.key === 'ArrowUp') {
      event.preventDefault()
      onChange(Math.min(5, value + 1))
    } else if (event.key === 'ArrowLeft' || event.key === 'ArrowDown') {
      event.preventDefault()
      onChange(Math.max(1, value - 1))
    } else if (VALUES.includes(Number(event.key) as (typeof VALUES)[number])) {
      onChange(Number(event.key))
    }
  }

  return (
    <div
      role="radiogroup"
      aria-label={t('review.starRating')}
      className="flex gap-1"
      onKeyDown={handleKeyDown}
      onMouseLeave={() => setHover(null)}
    >
      {VALUES.map((n) => (
        <button
          key={n}
          type="button"
          role="radio"
          aria-checked={value === n}
          aria-label={t('review.starLabel', { count: n })}
          tabIndex={n === value ? 0 : -1}
          className="rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          onMouseEnter={() => setHover(n)}
          onFocus={() => setHover(n)}
          onBlur={() => setHover(null)}
          onClick={() => onChange(n)}
        >
          <Star
            className={cn('size-6', n <= displayed ? 'fill-yellow-400 text-yellow-400' : 'text-muted-foreground')}
          />
        </button>
      ))}
    </div>
  )
}

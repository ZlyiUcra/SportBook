import React from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Check, ChevronsUpDown, X } from 'lucide-react'
import { Button } from '@/shared/ui/button'
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/ui/popover'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/shared/ui/command'
import { cn } from '@/shared/lib/utils'
import { suggestCities } from '@/entities/city/api/cityApi'
import { cityName, cityRegionName, type City } from '@/entities/city/model/types'

const MIN_QUERY_LENGTH = 2
const DEBOUNCE_MS = 250

type CityComboboxProps = {
  value: City | null
  onChange: (city: City) => void
  placeholder?: string
  /**
   * When provided and a city is selected, a clear (X) control appears that resets the selection.
   * Omitted where a city is mandatory (the owner venue form) - clearing only makes sense on the
   * search page, where no reference point is a valid state (004 spec US3).
   */
  onClear?: () => void
}

/**
 * Directory-backed city picker (shadcn Command/Popover + `cmdk`) - there is no way to submit
 * free text, only a suggested `City` (spec US1 Acceptance Scenario 3). Suggestions are debounced
 * server-side lookups; the full city list is never shipped to the browser (research.md City
 * selection UI).
 */
export function CityCombobox({ value, onChange, placeholder, onClear }: CityComboboxProps) {
  const { t, i18n } = useTranslation()
  const [open, setOpen] = React.useState(false)
  const [query, setQuery] = React.useState('')
  const [debouncedQuery, setDebouncedQuery] = React.useState('')

  React.useEffect(() => {
    const handle = setTimeout(() => setDebouncedQuery(query), DEBOUNCE_MS)
    return () => clearTimeout(handle)
  }, [query])

  const suggestionsQuery = useQuery({
    queryKey: ['cities', 'suggest', debouncedQuery],
    queryFn: () => suggestCities(debouncedQuery),
    enabled: open && debouncedQuery.length >= MIN_QUERY_LENGTH,
  })

  const suggestions = suggestionsQuery.data ?? []
  const showClear = value !== null && onClear !== undefined

  // Every close resets the in-dropdown search, so reopening always starts fresh rather than
  // showing the previous query and its stale cmdk highlight - which otherwise swallowed the next
  // click (the item cmdk still considered "active" did not re-fire onSelect on real pointer
  // input). An effect (not the onOpenChange handler) because selecting a city closes via a direct
  // setOpen(false) that bypasses onOpenChange - this catches every close path.
  React.useEffect(() => {
    if (!open) {
      setQuery('')
      setDebouncedQuery('')
    }
  }, [open])

  return (
    // Relative wrapper so the clear control can overlay the trigger's right edge - it must be a
    // sibling of the trigger, not a child, since a <button> cannot nest inside the trigger button.
    <div className="relative w-56">
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className={cn('w-full justify-between font-normal', showClear && 'pr-8')}
          >
            <span className="truncate">
              {value ? cityName(value, i18n.language) : (placeholder ?? t('citySelect.placeholder'))}
            </span>
            {!showClear && <ChevronsUpDown className="ml-2 size-4 shrink-0 opacity-50" />}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-56 p-0">
        <Command shouldFilter={false}>
          <CommandInput
            value={query}
            onValueChange={setQuery}
            placeholder={t('citySelect.searchPlaceholder')}
          />
          <CommandList>
            {query.length < MIN_QUERY_LENGTH && (
              <CommandEmpty>{t('citySelect.typeMore')}</CommandEmpty>
            )}
            {query.length >= MIN_QUERY_LENGTH && suggestions.length === 0 && (
              <CommandEmpty>{t('citySelect.noResults')}</CommandEmpty>
            )}
            <CommandGroup>
              {suggestions.map((city) => (
                <CommandItem
                  key={city.id}
                  value={String(city.id)}
                  onSelect={() => {
                    onChange(city)
                    setOpen(false)
                  }}
                >
                  <Check className={cn('mr-2 size-4', value?.id === city.id ? 'opacity-100' : 'opacity-0')} />
                  <span>{cityName(city, i18n.language)}</span>
                  <span className="ml-2 text-xs text-muted-foreground">{cityRegionName(city, i18n.language)}</span>
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
      </Popover>
      {showClear && (
        <button
          type="button"
          onClick={onClear}
          aria-label={t('citySelect.clear')}
          className="absolute right-2 top-1/2 -translate-y-1/2 rounded-sm opacity-60 hover:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          <X className="size-4" />
        </button>
      )}
    </div>
  )
}

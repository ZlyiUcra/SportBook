import React from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Check, ChevronsUpDown } from 'lucide-react'
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
}

/**
 * Directory-backed city picker (shadcn Command/Popover + `cmdk`) - there is no way to submit
 * free text, only a suggested `City` (spec US1 Acceptance Scenario 3). Suggestions are debounced
 * server-side lookups; the full city list is never shipped to the browser (research.md City
 * selection UI).
 */
export function CityCombobox({ value, onChange, placeholder }: CityComboboxProps) {
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

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-56 justify-between font-normal"
        >
          <span className="truncate">
            {value ? cityName(value, i18n.language) : (placeholder ?? t('citySelect.placeholder'))}
          </span>
          <ChevronsUpDown className="ml-2 size-4 shrink-0 opacity-50" />
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
  )
}

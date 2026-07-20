import { Settings } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/ui/dropdown-menu'
import { useThemeStore, type Theme } from '@/shared/theme/model/store'

const THEMES: Theme[] = ['light', 'dark', 'blue']
const LANGUAGES = [
  { code: 'en', label: 'English' },
  { code: 'uk', label: 'Українська' },
  { code: 'pt', label: 'Português' },
  { code: 'es', label: 'Español' },
]

/** Single settings entry point (gear icon) for language + theme - visible on every page, anonymous or authenticated. */
export function SettingsMenu() {
  const { t, i18n } = useTranslation()
  const theme = useThemeStore((state) => state.theme)
  const setTheme = useThemeStore((state) => state.setTheme)

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" size="icon" aria-label={t('settings.label')}>
          <Settings className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuLabel>{t('language.label')}</DropdownMenuLabel>
        <DropdownMenuRadioGroup
          value={i18n.language}
          onValueChange={(value) => void i18n.changeLanguage(value)}
        >
          {LANGUAGES.map((lang) => (
            <DropdownMenuRadioItem key={lang.code} value={lang.code}>
              {lang.label}
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>

        <DropdownMenuSeparator />

        <DropdownMenuLabel>{t('theme.label')}</DropdownMenuLabel>
        <DropdownMenuRadioGroup value={theme} onValueChange={(value) => setTheme(value as Theme)}>
          {THEMES.map((option) => (
            <DropdownMenuRadioItem key={option} value={option}>
              {t(`theme.${option}`)}
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

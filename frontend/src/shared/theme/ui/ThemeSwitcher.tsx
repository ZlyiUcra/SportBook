import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { useThemeStore, type Theme } from '../model/store'

const THEMES: Theme[] = ['light', 'dark', 'blue']

export function ThemeSwitcher() {
  const { t } = useTranslation()
  const theme = useThemeStore((state) => state.theme)
  const setTheme = useThemeStore((state) => state.setTheme)

  return (
    <div className="flex gap-1" role="group" aria-label={t('theme.label')}>
      {THEMES.map((option) => (
        <Button
          key={option}
          type="button"
          size="sm"
          variant={theme === option ? 'default' : 'outline'}
          onClick={() => setTheme(option)}
        >
          {t(`theme.${option}`)}
        </Button>
      ))}
    </div>
  )
}

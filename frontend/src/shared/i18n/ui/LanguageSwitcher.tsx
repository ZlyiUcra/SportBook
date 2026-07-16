import { useTranslation } from 'react-i18next'

const LANGUAGES = [
  { code: 'en', label: 'English' },
  { code: 'uk', label: 'Українська' },
  { code: 'pt', label: 'Português' },
]

export function LanguageSwitcher() {
  const { i18n, t } = useTranslation()

  return (
    <select
      value={i18n.language}
      onChange={(event) => void i18n.changeLanguage(event.target.value)}
      aria-label={t('language.label')}
      className="rounded-md border border-input bg-background px-2 py-1 text-sm"
    >
      {LANGUAGES.map((lang) => (
        <option key={lang.code} value={lang.code}>
          {lang.label}
        </option>
      ))}
    </select>
  )
}

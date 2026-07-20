import { Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'

/**
 * Full-page loading overlay - same blurred/dimmed backdrop treatment as the modal dialogs
 * (`shared/ui/dialog.tsx`'s `DialogOverlay`), covering the whole viewport above the page's own
 * content while data is loading, with a centered spinner.
 */
export function PageLoader() {
  const { t } = useTranslation()
  return (
    <div className="fixed inset-0 isolate z-50 flex flex-col items-center justify-center gap-2 bg-black/10 supports-backdrop-filter:backdrop-blur-xs">
      <Loader2 className="size-8 animate-spin text-foreground" />
      <p className="text-sm text-muted-foreground">{t('common.loading')}</p>
    </div>
  )
}

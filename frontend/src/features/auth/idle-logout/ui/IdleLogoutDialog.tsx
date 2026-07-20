import { useTranslation } from 'react-i18next'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/shared/ui/dialog'
import { Button } from '@/shared/ui/button'
import { useIdleLogout } from '../model/useIdleLogout'

/**
 * Mounted once inside the authenticated layout - invisible until 3 minutes of inactivity, then
 * blocks the page with a 30-second countdown warning before auto sign-out. Not dismissible via
 * Escape or an outside click - only an explicit button ("stay" or "log out now") counts as the
 * user acknowledging it.
 */
export function IdleLogoutDialog() {
  const { t } = useTranslation()
  const { secondsLeft, stay, logoutNow } = useIdleLogout()

  return (
    <Dialog open={secondsLeft !== null}>
      <DialogContent
        showCloseButton={false}
        onEscapeKeyDown={(event) => event.preventDefault()}
        onInteractOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>{t('auth.idleLogout.title')}</DialogTitle>
          <DialogDescription>{t('auth.idleLogout.message')}</DialogDescription>
        </DialogHeader>
        <p className="text-center font-heading text-5xl font-semibold tabular-nums" aria-live="polite">
          {secondsLeft ?? 0}
        </p>
        <DialogFooter>
          <Button variant="outline" onClick={logoutNow}>
            {t('auth.idleLogout.logoutButton')}
          </Button>
          <Button onClick={stay}>{t('auth.idleLogout.stayButton')}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

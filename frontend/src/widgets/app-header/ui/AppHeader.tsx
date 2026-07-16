import React from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Menu } from 'lucide-react'
import { cn } from '@/shared/lib/utils'
import { Button } from '@/shared/ui/button'
import {
  Sheet,
  SheetClose,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@/shared/ui/sheet'
import { SettingsMenu } from '@/widgets/settings-menu/ui/SettingsMenu'
import { useSessionStore } from '@/entities/session/model/store'
import { useLogout } from '@/features/auth/logout/model/useLogout'

/**
 * Explicit route-group matching instead of NavLink's default prefix matching: "Venues" must stay
 * marked active on `/venues/:id` detail pages too, and a bare `to="/"` NavLink either matches
 * every route (without `end`) or only the exact root (with `end`, missing `/venues/:id`) - neither
 * is right here.
 */
function isVenuesRoute(pathname: string) {
  return pathname === '/' || pathname.startsWith('/venues')
}

function isBookingsRoute(pathname: string) {
  return pathname === '/bookings'
}

function isOwnerVenuesRoute(pathname: string) {
  return pathname.startsWith('/owner/venues')
}

/** Underline-style active indicator for the inline desktop nav. */
function DesktopNavLink({
  to,
  isActive,
  children,
}: {
  to: string
  isActive: boolean
  children: React.ReactNode
}) {
  return (
    <Link
      to={to}
      className={cn(
        'border-b-2 border-transparent pb-0.5 text-sm text-muted-foreground hover:text-foreground',
        isActive && 'border-primary font-medium text-foreground',
      )}
    >
      {children}
    </Link>
  )
}

/**
 * Filled-pill active indicator for the mobile Sheet nav. Uses `bg-primary` rather than
 * `bg-accent` - the accent tint is too close to the sheet's popover background in every theme to
 * read as "selected" at a glance; primary/primary-foreground are built for that exact contrast.
 */
function MobileNavLink({
  to,
  isActive,
  children,
}: {
  to: string
  isActive: boolean
  children: React.ReactNode
}) {
  return (
    <SheetClose asChild>
      <Link
        to={to}
        className={cn(
          'rounded-md px-3 py-2 text-sm text-foreground hover:bg-accent',
          isActive && 'bg-primary font-medium text-primary-foreground hover:bg-primary',
        )}
      >
        {children}
      </Link>
    </SheetClose>
  )
}

/**
 * Below the `md` breakpoint, the inline nav/actions collapse into a hamburger-triggered Sheet
 * carrying the same links and actions - avoids the header wrapping into a cramped multi-row
 * mess on narrow viewports. Both variants mark the current route (underline on desktop, filled
 * pill on mobile) so it's clear which nav option is active.
 */
export function AppHeader() {
  const { t } = useTranslation()
  const location = useLocation()
  const user = useSessionStore((state) => state.user)
  const logout = useLogout()
  const [mobileNavOpen, setMobileNavOpen] = React.useState(false)

  const venuesActive = isVenuesRoute(location.pathname)
  const bookingsActive = isBookingsRoute(location.pathname)
  const ownerVenuesActive = isOwnerVenuesRoute(location.pathname)

  return (
    <header className="flex items-center justify-between gap-2 border-b px-4 py-2">
      <div className="flex items-center gap-4">
        <Link to="/" className="font-semibold">
          {t('app.title')}
        </Link>
        <nav className="hidden items-center gap-4 md:flex">
          <DesktopNavLink to="/" isActive={venuesActive}>
            {t('nav.venues')}
          </DesktopNavLink>
          <DesktopNavLink to="/bookings" isActive={bookingsActive}>
            {t('nav.myBookings')}
          </DesktopNavLink>
          <DesktopNavLink to="/owner/venues" isActive={ownerVenuesActive}>
            {t('nav.myVenues')}
          </DesktopNavLink>
        </nav>
      </div>

      <div className="hidden items-center gap-3 md:flex">
        <span className="text-sm text-muted-foreground">{user?.name}</span>
        <SettingsMenu />
        <Button variant="outline" size="sm" onClick={logout}>
          {t('nav.signOut')}
        </Button>
      </div>

      <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
        <SheetTrigger asChild className="md:hidden">
          <Button variant="outline" size="icon" aria-label={t('nav.openMenu')}>
            <Menu className="size-4" />
          </Button>
        </SheetTrigger>
        <SheetContent side="left" className="flex flex-col gap-6 p-4">
          <SheetHeader className="p-0">
            <SheetTitle>{t('app.title')}</SheetTitle>
          </SheetHeader>

          <nav className="flex flex-col gap-1">
            <MobileNavLink to="/" isActive={venuesActive}>
              {t('nav.venues')}
            </MobileNavLink>
            <MobileNavLink to="/bookings" isActive={bookingsActive}>
              {t('nav.myBookings')}
            </MobileNavLink>
            <MobileNavLink to="/owner/venues" isActive={ownerVenuesActive}>
              {t('nav.myVenues')}
            </MobileNavLink>
          </nav>

          <div className="mt-auto flex flex-col gap-3 border-t pt-4">
            <span className="text-sm text-muted-foreground">{user?.name}</span>
            <SettingsMenu />
            <Button variant="outline" size="sm" onClick={logout}>
              {t('nav.signOut')}
            </Button>
          </div>
        </SheetContent>
      </Sheet>
    </header>
  )
}

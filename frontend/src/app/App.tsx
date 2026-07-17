import { Routes, Route, Outlet, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { SettingsMenu } from '@/widgets/settings-menu/ui/SettingsMenu'
import { AppHeader } from '@/widgets/app-header/ui/AppHeader'
import { RequireAuth } from './providers/RequireAuth'
import { LoginPage } from '@/pages/login/ui/LoginPage'
import { RegisterPage } from '@/pages/register/ui/RegisterPage'
import { AboutPage } from '@/pages/about/ui/AboutPage'
import { VenueSearchPage } from '@/pages/venues/ui/VenueSearchPage'
import { VenueDetailPage } from '@/pages/venue-detail/ui/VenueDetailPage'
import { MyBookingsPage } from '@/pages/my-bookings/ui/MyBookingsPage'
import { OwnerDashboardPage } from '@/pages/owner-dashboard/ui/OwnerDashboardPage'
import { OwnerBookingsPage } from '@/pages/owner-bookings/ui/OwnerBookingsPage'

/** Authenticated area: header navigation, with the settings menu (language + theme) on the right. */
function AuthenticatedLayout() {
  return (
    <RequireAuth>
      <AppHeader />
      <Outlet />
    </RequireAuth>
  )
}

/** Anonymous area: settings menu and an About link, top corners, same slot as the authenticated header. */
function AnonymousLayout() {
  const { t } = useTranslation()
  return (
    <>
      <div className="flex items-center justify-between p-2">
        <Link to="/about" className="text-sm text-muted-foreground hover:text-foreground">
          {t('nav.about')}
        </Link>
        <SettingsMenu />
      </div>
      <Outlet />
    </>
  )
}

/**
 * Public area: same header as AuthenticatedLayout, but no RequireAuth gate - unlike every other
 * route, `/about` is deliberately reachable without an account (a documented exception to spec
 * FR-014, see spec.md). AppHeader itself adapts its right-hand slot when there's no signed-in user.
 */
function PublicLayout() {
  return (
    <>
      <AppHeader />
      <Outlet />
    </>
  )
}

export function App() {
  return (
    <Routes>
      <Route element={<AnonymousLayout />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>
      <Route element={<PublicLayout />}>
        <Route path="/about" element={<AboutPage />} />
      </Route>
      <Route element={<AuthenticatedLayout />}>
        <Route path="/" element={<VenueSearchPage />} />
        <Route path="/venues/:id" element={<VenueDetailPage />} />
        <Route path="/bookings" element={<MyBookingsPage />} />
        <Route path="/owner/venues" element={<OwnerDashboardPage />} />
        <Route path="/owner/bookings" element={<OwnerBookingsPage />} />
      </Route>
    </Routes>
  )
}

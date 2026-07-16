import { Routes, Route, Outlet } from 'react-router-dom'
import { SettingsMenu } from '@/widgets/settings-menu/ui/SettingsMenu'
import { AppHeader } from '@/widgets/app-header/ui/AppHeader'
import { RequireAuth } from './providers/RequireAuth'
import { LoginPage } from '@/pages/login/ui/LoginPage'
import { RegisterPage } from '@/pages/register/ui/RegisterPage'
import { VenueSearchPage } from '@/pages/venues/ui/VenueSearchPage'
import { VenueDetailPage } from '@/pages/venue-detail/ui/VenueDetailPage'
import { MyBookingsPage } from '@/pages/my-bookings/ui/MyBookingsPage'
import { OwnerDashboardPage } from '@/pages/owner-dashboard/ui/OwnerDashboardPage'

/** Authenticated area: header navigation, with the settings menu (language + theme) on the right. */
function AuthenticatedLayout() {
  return (
    <RequireAuth>
      <AppHeader />
      <Outlet />
    </RequireAuth>
  )
}

/** Anonymous area: just the settings menu, top-right, same as the authenticated header slot. */
function AnonymousLayout() {
  return (
    <>
      <div className="flex justify-end p-2">
        <SettingsMenu />
      </div>
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
      <Route element={<AuthenticatedLayout />}>
        <Route path="/" element={<VenueSearchPage />} />
        <Route path="/venues/:id" element={<VenueDetailPage />} />
        <Route path="/bookings" element={<MyBookingsPage />} />
        <Route path="/owner/venues" element={<OwnerDashboardPage />} />
      </Route>
    </Routes>
  )
}

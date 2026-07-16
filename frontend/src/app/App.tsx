import { Routes, Route } from 'react-router-dom'
import { ThemeSwitcher } from '@/shared/theme/ui/ThemeSwitcher'
import { RequireAuth } from './providers/RequireAuth'
import { LoginPage } from '@/pages/login/ui/LoginPage'
import { RegisterPage } from '@/pages/register/ui/RegisterPage'
import { HomePage } from '@/pages/home/ui/HomePage'

export function App() {
  return (
    <>
      <div className="flex justify-end p-2">
        <ThemeSwitcher />
      </div>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route
          path="/"
          element={
            <RequireAuth>
              <HomePage />
            </RequireAuth>
          }
        />
      </Routes>
    </>
  )
}

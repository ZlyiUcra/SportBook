import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from './context/AuthContext'
import { Login } from './pages/Login'
import { Register } from './pages/Register'
import { Home } from './pages/Home'

function RequireAuth({ children }: React.PropsWithChildren) {
  const { user } = useAuth()
  return user ? children : <Navigate to="/login" replace />
}

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <Home />
          </RequireAuth>
        }
      />
    </Routes>
  )
}

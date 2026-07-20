import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useSessionStore } from '@/entities/session/model/store'

/** Redirects to `/login`, remembering the current location so a successful sign-in returns here
 * instead of always landing on the default route (LoginForm/RegisterForm read `location.state.from`). */
export function RequireAuth({ children }: React.PropsWithChildren) {
  const user = useSessionStore((state) => state.user)
  const location = useLocation()
  return user ? children : <Navigate to="/login" replace state={{ from: location }} />
}

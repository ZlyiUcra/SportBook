import React from 'react'
import { Navigate } from 'react-router-dom'
import { useSessionStore } from '@/entities/session/model/store'

export function RequireAuth({ children }: React.PropsWithChildren) {
  const user = useSessionStore((state) => state.user)
  return user ? children : <Navigate to="/login" replace />
}

import React from 'react'
import { setAccessToken } from '../api/client'
import type { UserResponse } from '../api/auth'

type AuthContextValue = {
  user: UserResponse | null
  signIn: (accessToken: string, user: UserResponse) => void
  signOut: () => void
}

const AuthContext = React.createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: React.PropsWithChildren) {
  const [user, setUser] = React.useState<UserResponse | null>(null)

  const signIn = React.useCallback((accessToken: string, nextUser: UserResponse) => {
    setAccessToken(accessToken)
    setUser(nextUser)
  }, [])

  const signOut = React.useCallback(() => {
    setAccessToken(null)
    setUser(null)
  }, [])

  const value = React.useMemo(() => ({ user, signIn, signOut }), [user, signIn, signOut])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = React.useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

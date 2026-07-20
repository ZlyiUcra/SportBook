import { useNavigate } from 'react-router-dom'
import { useSessionStore } from '@/entities/session/model/store'
import { logout } from '../api/logout'

/**
 * Clears the local session unconditionally, but also asks the server to revoke the refresh token
 * first (now that it's persisted in `localStorage`, leaving it valid server-side after a sign-out
 * would be a real credential-leftover, not just a client-side inconvenience). A failed revoke
 * call (offline, token already expired) must not block signing out locally.
 */
export function useLogout() {
  const refreshToken = useSessionStore((state) => state.refreshToken)
  const signOut = useSessionStore((state) => state.signOut)
  const navigate = useNavigate()

  return () => {
    if (refreshToken) {
      logout(refreshToken).catch(() => {})
    }
    signOut()
    navigate('/login')
  }
}

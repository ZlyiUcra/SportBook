import React from 'react'
import { refresh } from '@/features/auth/refresh/api/refresh'
import { updateStoredTokens, useSessionStore } from './store'

/**
 * Runs once on app mount: if a session was restored from `localStorage`, its access token may
 * already be expired (15-minute lifetime), so this exchanges the stored refresh token for a fresh
 * pair right away rather than waiting for the first API call to fail. On failure (refresh token
 * revoked/expired) the stale session is cleared, same as an explicit sign-out.
 */
export function useSessionBootstrap() {
  const refreshToken = useSessionStore((state) => state.refreshToken)
  const signOut = useSessionStore((state) => state.signOut)

  React.useEffect(() => {
    if (!refreshToken) return
    let cancelled = false

    refresh(refreshToken)
      .then((data) => {
        if (!cancelled) {
          updateStoredTokens(data.accessToken, data.refreshToken)
        }
      })
      .catch(() => {
        if (!cancelled) {
          signOut()
        }
      })

    return () => {
      cancelled = true
    }
    // Runs once on mount only - a refresh triggered by this effect must not re-trigger itself.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])
}

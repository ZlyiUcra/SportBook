import { create } from 'zustand'
import { setAuthToken } from '@/shared/api/axiosInstance'
import type { User } from '@/entities/user/model/types'

const STORAGE_KEY = 'sportbook-session'

type StoredSession = {
  accessToken: string
  refreshToken: string
  user: User
}

function readStoredSession(): StoredSession | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as StoredSession
  } catch {
    return null
  }
}

function writeStoredSession(session: StoredSession | null) {
  if (session) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  } else {
    localStorage.removeItem(STORAGE_KEY)
  }
}

type SessionState = {
  user: User | null
  refreshToken: string | null
  signIn: (accessToken: string, refreshToken: string, user: User) => void
  signOut: () => void
}

const initialSession = readStoredSession()
if (initialSession) {
  // Best-effort: may already be expired (15-minute lifetime) - the app bootstrap
  // (useSessionBootstrap) calls /auth/refresh right away to get a valid one.
  setAuthToken(initialSession.accessToken)
}

/**
 * Auth session state, persisted to `localStorage` (access/refresh token + user) so a page reload
 * no longer signs the user out. The stored access token is trusted optimistically on load - it may
 * already be expired, which is why `useSessionBootstrap` immediately exchanges the refresh token
 * for a fresh pair rather than trusting storage as the source of truth.
 */
export const useSessionStore = create<SessionState>((set) => ({
  user: initialSession?.user ?? null,
  refreshToken: initialSession?.refreshToken ?? null,
  signIn: (accessToken, refreshToken, user) => {
    setAuthToken(accessToken)
    writeStoredSession({ accessToken, refreshToken, user })
    set({ user, refreshToken })
  },
  signOut: () => {
    setAuthToken(null)
    writeStoredSession(null)
    set({ user: null, refreshToken: null })
  },
}))

/**
 * Called after a successful `/auth/refresh` (bootstrap on load, or a future 401 retry) - the
 * refresh token rotates server-side on every use, so both tokens must be re-persisted, not just
 * the access token.
 */
export function updateStoredTokens(accessToken: string, refreshToken: string) {
  setAuthToken(accessToken)
  const stored = readStoredSession()
  if (stored) {
    writeStoredSession({ ...stored, accessToken, refreshToken })
  }
  useSessionStore.setState({ refreshToken })
}

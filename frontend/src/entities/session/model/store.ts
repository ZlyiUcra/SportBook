import { create } from 'zustand'
import { setAuthToken } from '@/shared/api/axiosInstance'
import type { User } from '@/entities/user/model/types'

type SessionState = {
  user: User | null
  signIn: (accessToken: string, user: User) => void
  signOut: () => void
}

/** Auth session state. In-memory only (no persistence) - a page refresh signs the user out, matching the prior AuthContext behavior. */
export const useSessionStore = create<SessionState>((set) => ({
  user: null,
  signIn: (accessToken, user) => {
    setAuthToken(accessToken)
    set({ user })
  },
  signOut: () => {
    setAuthToken(null)
    set({ user: null })
  },
}))

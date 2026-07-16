import { create } from 'zustand'

export type Theme = 'light' | 'dark' | 'blue'

const STORAGE_KEY = 'sportbook-theme'

function applyThemeClass(theme: Theme) {
  const root = document.documentElement
  root.classList.remove('dark', 'blue')
  if (theme !== 'light') {
    root.classList.add(theme)
  }
}

function getInitialTheme(): Theme {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored === 'light' || stored === 'dark' || stored === 'blue') {
    return stored
  }
  return 'dark'
}

type ThemeState = {
  theme: Theme
  setTheme: (theme: Theme) => void
}

const initialTheme = getInitialTheme()
applyThemeClass(initialTheme)

/** Default theme is dark (no stored preference yet); persisted to localStorage across sessions. */
export const useThemeStore = create<ThemeState>((set) => ({
  theme: initialTheme,
  setTheme: (theme) => {
    localStorage.setItem(STORAGE_KEY, theme)
    applyThemeClass(theme)
    set({ theme })
  },
}))

import React from 'react'
import { useSessionStore } from '@/entities/session/model/store'
import { logout } from '@/features/auth/logout/api/logout'

const IDLE_MS = 3 * 60 * 1000
const WARNING_SECONDS = 30
const ACTIVITY_THROTTLE_MS = 1000
const ACTIVITY_EVENTS = ['mousemove', 'keydown', 'click', 'touchstart', 'scroll'] as const

/**
 * After 3 minutes with no user activity, starts a 30-second countdown (`secondsLeft`); reaching
 * zero signs the user out. Activity is ignored while the countdown is showing - only `stay()`
 * (the dialog's button) dismisses it, per the requested UX (a passive mouse twitch elsewhere on
 * the page must not silently swallow the warning).
 */
export function useIdleLogout() {
  const [secondsLeft, setSecondsLeft] = React.useState<number | null>(null)
  // Mirrors `secondsLeft` for the activity listener below, which is attached once on mount and
  // would otherwise only ever see the state value from that first render (stale closure).
  const secondsLeftRef = React.useRef<number | null>(null)
  const idleTimerRef = React.useRef<ReturnType<typeof setTimeout>>(undefined)
  const countdownIntervalRef = React.useRef<ReturnType<typeof setInterval>>(undefined)

  const setCountdown = React.useCallback((value: number | null) => {
    secondsLeftRef.current = value
    setSecondsLeft(value)
  }, [])

  const clearTimers = React.useCallback(() => {
    clearTimeout(idleTimerRef.current)
    clearInterval(countdownIntervalRef.current)
  }, [])

  const logoutNow = React.useCallback(() => {
    clearTimers()
    const { refreshToken, signOut } = useSessionStore.getState()
    if (refreshToken) {
      logout(refreshToken).catch(() => {})
    }
    signOut()
  }, [clearTimers])

  const startIdleTimer = React.useCallback(() => {
    clearTimers()
    setCountdown(null)
    idleTimerRef.current = setTimeout(() => {
      let remaining = WARNING_SECONDS
      setCountdown(remaining)
      countdownIntervalRef.current = setInterval(() => {
        remaining -= 1
        if (remaining <= 0) {
          logoutNow()
        } else {
          setCountdown(remaining)
        }
      }, 1000)
    }, IDLE_MS)
  }, [clearTimers, setCountdown, logoutNow])

  React.useEffect(() => {
    startIdleTimer()
    // mousemove/scroll can fire dozens of times a second - only reset the idle timer at most
    // once a second, rather than on every single event.
    let lastReset = Date.now()
    const handleActivity = () => {
      if (secondsLeftRef.current !== null) return
      const now = Date.now()
      if (now - lastReset < ACTIVITY_THROTTLE_MS) return
      lastReset = now
      startIdleTimer()
    }
    ACTIVITY_EVENTS.forEach((event) => window.addEventListener(event, handleActivity))
    return () => {
      clearTimers()
      ACTIVITY_EVENTS.forEach((event) => window.removeEventListener(event, handleActivity))
    }
  }, [startIdleTimer, clearTimers])

  return { secondsLeft, stay: startIdleTimer, logoutNow }
}

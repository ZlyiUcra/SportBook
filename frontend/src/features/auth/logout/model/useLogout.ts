import { useNavigate } from 'react-router-dom'
import { useSessionStore } from '@/entities/session/model/store'

export function useLogout() {
  const signOut = useSessionStore((state) => state.signOut)
  const navigate = useNavigate()

  return () => {
    signOut()
    navigate('/login')
  }
}

import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { getMe } from '../api/auth'
import { useAuth } from '../context/AuthContext'

export function Home() {
  const { user, signOut } = useAuth()
  const navigate = useNavigate()
  const meQuery = useQuery({ queryKey: ['me'], queryFn: getMe })

  function handleSignOut() {
    signOut()
    navigate('/login')
  }

  return (
    <div>
      <h1>SportBook</h1>
      <p>Registered as: {user?.name} ({user?.email})</p>
      <p>
        Confirmed via authenticated <code>GET /api/users/me</code>:{' '}
        {meQuery.isLoading && 'loading...'}
        {meQuery.isError && 'request failed'}
        {meQuery.data && `${meQuery.data.name} - role ${meQuery.data.role}`}
      </p>
      <button onClick={handleSignOut}>Sign out</button>
    </div>
  )
}

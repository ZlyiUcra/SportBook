import React from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { login } from '../api/auth'
import { ApiRequestError } from '../api/client'
import { useAuth } from '../context/AuthContext'

export function Login() {
  const navigate = useNavigate()
  const { signIn } = useAuth()
  const [email, setEmail] = React.useState('')
  const [password, setPassword] = React.useState('')

  const mutation = useMutation({
    mutationFn: login,
    onSuccess: (data) => {
      signIn(data.accessToken, data.user)
      navigate('/')
    },
  })

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    mutation.mutate({ email, password })
  }

  return (
    <form onSubmit={handleSubmit}>
      <h1>Log in</h1>
      <label>
        Email
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
      </label>
      <label>
        Password
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
      </label>
      <button type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? 'Logging in...' : 'Log in'}
      </button>
      {mutation.isError && (
        <p role="alert">
          {mutation.error instanceof ApiRequestError
            ? mutation.error.error.message
            : 'Login failed.'}
        </p>
      )}
      <p>
        No account yet? <Link to="/register">Register</Link>
      </p>
    </form>
  )
}

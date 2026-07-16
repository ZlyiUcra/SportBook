import React from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { register } from '../api/auth'
import { ApiRequestError } from '../api/client'
import { useAuth } from '../context/AuthContext'

export function Register() {
  const navigate = useNavigate()
  const { signIn } = useAuth()
  const [name, setName] = React.useState('')
  const [email, setEmail] = React.useState('')
  const [password, setPassword] = React.useState('')

  const mutation = useMutation({
    mutationFn: register,
    onSuccess: (data) => {
      signIn(data.accessToken, data.user)
      navigate('/')
    },
  })

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    mutation.mutate({ name, email, password })
  }

  return (
    <form onSubmit={handleSubmit}>
      <h1>Register</h1>
      <label>
        Name
        <input value={name} onChange={(e) => setName(e.target.value)} required />
      </label>
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
          minLength={8}
        />
      </label>
      <button type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? 'Creating account...' : 'Register'}
      </button>
      {mutation.isError && (
        <p role="alert">
          {mutation.error instanceof ApiRequestError
            ? mutation.error.error.message
            : 'Registration failed.'}
        </p>
      )}
      <p>
        Already have an account? <Link to="/login">Log in</Link>
      </p>
    </form>
  )
}

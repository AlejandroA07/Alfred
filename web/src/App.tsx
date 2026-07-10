import { useEffect, useState, type FormEvent } from 'react'
import './App.css'

type Session = { email: string } | null

async function fetchSession(): Promise<Session> {
  const res = await fetch('/api/auth/manage/info', { credentials: 'include' })
  if (!res.ok) return null
  const info = (await res.json()) as { email: string }
  return { email: info.email }
}

export default function App() {
  const [session, setSession] = useState<Session>(null)
  const [checking, setChecking] = useState(true)
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    fetchSession()
      .then(setSession)
      .finally(() => setChecking(false))
  }, [])

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      if (mode === 'register') {
        const res = await fetch('/api/auth/register', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email, password }),
        })
        if (!res.ok) {
          const problem = await res.json().catch(() => null)
          const details = problem?.errors
            ? Object.values(problem.errors as Record<string, string[]>).flat().join(' ')
            : 'Registration failed.'
          setError(details)
          return
        }
      }
      const login = await fetch('/api/auth/login?useCookies=true&useSessionCookies=true', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email, password }),
      })
      if (!login.ok) {
        setError('Wrong email or password.')
        return
      }
      setSession(await fetchSession())
    } finally {
      setBusy(false)
    }
  }

  if (checking) return <main className="shell" />

  if (!session) {
    return (
      <main className="shell">
        <h1>Alfred</h1>
        <p className="tagline">Your personal butler for life admin.</p>
        <form onSubmit={submit} className="card">
          <label>
            Email
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />
          </label>
          <label>
            Password
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={10}
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            />
          </label>
          {error && <p className="error">{error}</p>}
          <button type="submit" disabled={busy}>
            {mode === 'login' ? 'Log in' : 'Create account'}
          </button>
          <button type="button" className="link" onClick={() => setMode(mode === 'login' ? 'register' : 'login')}>
            {mode === 'login' ? 'No account yet? Register' : 'Already registered? Log in'}
          </button>
        </form>
      </main>
    )
  }

  return (
    <main className="shell">
      <h1>Alfred</h1>
      <p className="tagline">Good to see you, {session.email}.</p>
      <div className="card">
        <p>The skeleton is standing. Finance (M1) is next.</p>
      </div>
    </main>
  )
}

import { FormEvent, useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { ApiRequestError } from '../services/apiClient'
import { USER_ROLES, getDefaultRoute, getRoleDescription, type UserRole } from '../types/userRole'

export function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [selectedRole, setSelectedRole] = useState<UserRole>('Submitter')
  const [username, setUsername] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const redirectPath =
    (location.state as { from?: string } | null)?.from ?? getDefaultRoute(selectedRole)

  if (isAuthenticated) {
    return <Navigate to={redirectPath} replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setErrorMessage(null)

    try {
      await login(selectedRole, username)
      navigate(getDefaultRoute(selectedRole), { replace: true })
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? error.message
          : 'Unable to sign in. Check that the gateway is running.'
      setErrorMessage(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-8">
      <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
        <div className="mb-6 text-center">
          <p className="text-sm font-semibold uppercase tracking-wide text-slate-500">
            ApprovalFlow
          </p>
          <h1 className="mt-2 text-2xl font-semibold text-slate-900">Sign in</h1>
          <p className="mt-2 text-sm text-slate-500">
            Choose a role and enter your name to receive a JWT from the gateway.
          </p>
        </div>

        <form className="space-y-5" onSubmit={handleSubmit}>
          <div>
            <label htmlFor="username" className="block text-sm font-medium text-slate-700">
              Your name
            </label>
            <input
              id="username"
              name="username"
              type="text"
              required
              autoComplete="name"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              placeholder="e.g. Tehila Cohen"
              className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-slate-900 focus:outline-none focus:ring-2 focus:ring-slate-200"
            />
          </div>

          <fieldset className="space-y-3">
            <legend className="text-sm font-medium text-slate-700">Role</legend>
            {USER_ROLES.map((role) => {
              const isSelected = selectedRole === role

              return (
                <label
                  key={role}
                  className={[
                    'flex cursor-pointer items-start gap-3 rounded-xl border p-4 transition-colors',
                    isSelected
                      ? 'border-slate-900 bg-slate-50'
                      : 'border-slate-200 hover:border-slate-300',
                  ].join(' ')}
                >
                  <input
                    type="radio"
                    name="role"
                    value={role}
                    checked={isSelected}
                    onChange={() => setSelectedRole(role)}
                    className="mt-1"
                  />
                  <span>
                    <span className="block text-sm font-semibold text-slate-900">{role}</span>
                    <span className="mt-1 block text-sm text-slate-500">
                      {getRoleDescription(role)}
                    </span>
                  </span>
                </label>
              )
            })}
          </fieldset>

          {errorMessage ? (
            <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700" role="alert">
              {errorMessage}
            </p>
          ) : null}

          <button
            type="submit"
            disabled={isSubmitting || !username.trim()}
            className="w-full rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? 'Signing in…' : 'Continue'}
          </button>
        </form>
      </div>
    </div>
  )
}

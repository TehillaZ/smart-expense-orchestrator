import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import { canAccessPath, getDefaultRoute } from '../../types/userRole'

export function ProtectedRoute() {
  const { isAuthenticated, session } = useAuth()
  const location = useLocation()

  if (!isAuthenticated || !session) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  if (!canAccessPath(session.role, location.pathname)) {
    return <Navigate to={getDefaultRoute(session.role)} replace />
  }

  return <Outlet context={{ role: session.role }} />
}

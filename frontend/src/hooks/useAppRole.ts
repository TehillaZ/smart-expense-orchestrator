import { useOutletContext } from 'react-router-dom'
import type { UserRole } from '../types/userRole'

export type AppLayoutContext = {
  role: UserRole
}

export function useAppRole(): UserRole {
  const context = useOutletContext<AppLayoutContext>()
  return context.role
}

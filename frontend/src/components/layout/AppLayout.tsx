import { useEffect, useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import {
  canAccessPath,
  getDefaultRoute,
  type UserRole,
} from '../../types/userRole'
import { Sidebar } from './Sidebar'
import { TopNavbar } from './TopNavbar'

export function AppLayout() {
  const [role, setRole] = useState<UserRole>('Submitter')
  const [isSidebarOpen, setIsSidebarOpen] = useState(true)
  const location = useLocation()
  const navigate = useNavigate()

  useEffect(() => {
    if (!canAccessPath(role, location.pathname)) {
      navigate(getDefaultRoute(role), { replace: true })
    }
  }, [role, location.pathname, navigate])

  useEffect(() => {
    if (window.matchMedia('(max-width: 1023px)').matches) {
      setIsSidebarOpen(false)
    }
  }, [])

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans text-slate-900">
      <Sidebar
        role={role}
        isOpen={isSidebarOpen}
        onClose={() => setIsSidebarOpen(false)}
      />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopNavbar
          role={role}
          onRoleChange={setRole}
          isSidebarOpen={isSidebarOpen}
          onOpenSidebar={() => setIsSidebarOpen(true)}
        />
        <main className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8">
          <div className="mx-auto max-w-7xl">
            <Outlet context={{ role }} />
          </div>
        </main>
      </div>
    </div>
  )
}

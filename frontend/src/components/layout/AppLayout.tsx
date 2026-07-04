import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import type { UserRole } from '../../types/userRole'
import { Sidebar } from './Sidebar'
import { TopNavbar } from './TopNavbar'

export function AppLayout() {
  const [role, setRole] = useState<UserRole>('Submitter')

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans text-slate-900">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopNavbar role={role} onRoleChange={setRole} />
        <main className="flex-1 overflow-y-auto p-8">
          <div className="mx-auto max-w-7xl">
            <Outlet context={{ role }} />
          </div>
        </main>
      </div>
    </div>
  )
}

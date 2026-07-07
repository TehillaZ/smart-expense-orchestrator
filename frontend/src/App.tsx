import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { ApprovalsQueuePage } from './pages/ApprovalsQueuePage'
import { AuditTrailPage } from './pages/AuditTrailPage'
import { DashboardPage } from './pages/DashboardPage'
import { EscalationDetailPage } from './pages/EscalationDetailPage'
import { MySubmissionsPage } from './pages/MySubmissionsPage'
import { PolicySettingsPage } from './pages/PolicySettingsPage'

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Navigate to="/submissions" replace />} />
        <Route path="submissions" element={<MySubmissionsPage mode="submit" />} />
        <Route path="track" element={<MySubmissionsPage mode="track" />} />
        <Route path="approvals" element={<ApprovalsQueuePage />} />
        <Route path="approvals/:trackingId" element={<EscalationDetailPage />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="audit" element={<AuditTrailPage />} />
        <Route path="policy" element={<PolicySettingsPage />} />
      </Route>
    </Routes>
  )
}

export default App

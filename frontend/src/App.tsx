import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './components/auth/ProtectedRoute'
import { AppLayout } from './components/layout/AppLayout'
import { AuthProvider } from './contexts/AuthContext'
import { ApprovalsQueuePage } from './pages/ApprovalsQueuePage'
import { AuditTrailPage } from './pages/AuditTrailPage'
import { DashboardPage } from './pages/DashboardPage'
import { EscalationDetailPage } from './pages/EscalationDetailPage'
import { LoginPage } from './pages/LoginPage'
import { MySubmissionsPage } from './pages/MySubmissionsPage'
import { PolicySettingsPage } from './pages/PolicySettingsPage'

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
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
        </Route>
      </Routes>
    </AuthProvider>
  )
}

export default App

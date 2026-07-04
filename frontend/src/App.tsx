import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { ApprovalsQueuePage } from './pages/ApprovalsQueuePage'
import { MySubmissionsPage } from './pages/MySubmissionsPage'
import { PolicySettingsPage } from './pages/PolicySettingsPage'

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Navigate to="/submissions" replace />} />
        <Route path="submissions" element={<MySubmissionsPage />} />
        <Route path="approvals" element={<ApprovalsQueuePage />} />
        <Route path="policy" element={<PolicySettingsPage />} />
      </Route>
    </Routes>
  )
}

export default App

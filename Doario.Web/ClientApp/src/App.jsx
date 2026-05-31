import { Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Home from './Pages/Home';
import UploadTest from './UploadTest';
import MailPortal from './Pages/MailPortal';
import SettingsLayout from './Pages/Settings/SettingsLayout';
import OrganisationSettings from './Pages/Settings/OrganisationSettings';
import StaffSettings from './Pages/Settings/StaffSettings';
import IntegrationsSettings from './Pages/Settings/IntegrationsSettings';
import SubscriptionSettings from './Pages/Settings/SubscriptionSettings';
import BatchScanPage from './Pages/BatchScanPage';
import ExtractionFieldsSettings from './Pages/Settings/ExtractionFieldsSettings';
import AiAssignmentSettings from './Pages/Settings/AiAssignmentSettings';
import InboxSettings from './Pages/Settings/InboxSettings';
import BillingDashboard from './Pages/Settings/BillingDashboard';
import OperatorPortal from './Pages/OperatorPortal';
import VerifyExtractionPage from './Pages/VerifyExtractionPage';
import DoarioHomePage from './Pages/DoarioHomePage';
import LoginPage from './Pages/LoginPage';
import { useState, useEffect } from 'react';

// ── Auth guard ────────────────────────────────────────────────────────────────
function RequireAuth({ children }) {
    const [checked, setChecked] = useState(false);
    const [authed, setAuthed] = useState(false);

    useEffect(() => {
        fetch('/api/demo-auth/check')
            .then(r => r.json())
            .then(d => {
                setAuthed(d.authenticated);
                setChecked(true);
            })
            .catch(() => setChecked(true));
    }, []);

    if (!checked) return null;
    if (!authed) return <Navigate to="/login" replace />;
    return children;
}

function App() {
    return (
        <Routes>
            {/* Marketing homepage */}
            <Route path="/" element={<DoarioHomePage />} />

            {/* Login */}
            <Route path="/login" element={<LoginPage />} />

            {/* Public token-based routes — no login, no layout */}
            <Route path="/verify-extraction/:documentId/:token" element={<VerifyExtractionPage />} />

            {/* Main app — all original routes unchanged, just wrapped in auth */}
            <Route element={<RequireAuth><Layout /></RequireAuth>}>
                <Route path="upload-test" element={<UploadTest />} />
                <Route path="scan" element={<BatchScanPage />} />
                <Route path="admin/queue" element={<MailPortal />} />
                <Route path="operator" element={<OperatorPortal />} />
                <Route path="settings" element={<SettingsLayout />}>
                    <Route index element={<Navigate to="/settings/organisation" replace />} />
                    <Route path="organisation" element={<OrganisationSettings />} />
                    <Route path="staff" element={<StaffSettings />} />
                    <Route path="integrations" element={<IntegrationsSettings />} />
                    <Route path="subscription" element={<SubscriptionSettings />} />
                    <Route path="billing" element={<BillingDashboard />} />
                    <Route path="extraction-fields" element={<ExtractionFieldsSettings />} />
                    <Route path="ai-assignment" element={<AiAssignmentSettings />} />
                    <Route path="inbox" element={<InboxSettings />} />
                </Route>
            </Route>
        </Routes>
    );
}

export default App;
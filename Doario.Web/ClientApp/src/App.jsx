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

function App() {
    return (
        <Routes>
            <Route path="/" element={<Layout />}>
                <Route index element={<Home />} />
                <Route path="upload-test" element={<UploadTest />} />
                <Route path="scan" element={<BatchScanPage />} />
                <Route path="admin/queue" element={<MailPortal />} />
                <Route path="settings" element={<SettingsLayout />}>
                    <Route index element={<Navigate to="/settings/organisation" replace />} />
                    <Route path="organisation" element={<OrganisationSettings />} />
                    <Route path="staff" element={<StaffSettings />} />
                    <Route path="integrations" element={<IntegrationsSettings />} />
                    <Route path="subscription" element={<SubscriptionSettings />} />
                    <Route path="extraction-fields" element={<ExtractionFieldsSettings />} />
                </Route>
            </Route>
        </Routes>
    );
}

export default App;
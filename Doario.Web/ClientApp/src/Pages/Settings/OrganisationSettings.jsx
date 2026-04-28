// OrganisationSettings.jsx

import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { S, Shared } from './SettingsShared';

export default function OrganisationSettings() {
    const [org, setOrg] = useState({
        name: '', domain: '', mailboxAddress: '',
        sharePointSiteUrl: '', isHipaaEnabled: false, scanInboxAddress: ''
    });
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [msg, setMsg] = useState(null);
    const [error, setError] = useState(null);

    useEffect(() => { load(); }, []);

    const load = async () => {
        setLoading(true);
        try {
            const res = await axios.get('/api/settings/organisation');
            setOrg(res.data);
        } catch {
            setError('Failed to load organisation details.');
        } finally {
            setLoading(false);
        }
    };

    const handleSave = async () => {
        setSaving(true);
        setMsg(null);
        setError(null);
        try {
            await axios.put('/api/settings/organisation', {
                name: org.name,
                mailboxAddress: org.mailboxAddress,
                sharePointSiteUrl: org.sharePointSiteUrl,
            });
            setMsg('Organisation updated successfully.');
            setTimeout(() => setMsg(null), 4000);
        } catch {
            setError('Failed to save changes.');
        } finally {
            setSaving(false);
        }
    };

    if (loading) return <Shared.Loading />;

    return (
        <div>
            <div style={S.sectionTitle}>Organisation</div>
            <div style={S.sectionSub}>Your organisation details used across Doario</div>

            <div style={S.card}>

                <label style={S.label}>Organisation Name</label>
                <input
                    style={S.input}
                    value={org.name ?? ''}
                    onChange={e => setOrg({ ...org, name: e.target.value })}
                />

                <label style={S.label}>Domain</label>
                <input
                    style={{ ...S.input, ...S.inputReadOnly }}
                    value={org.domain ?? ''}
                    readOnly
                />

                <label style={S.label}>Mailbox Address</label>
                <input
                    style={S.input}
                    value={org.mailboxAddress ?? ''}
                    onChange={e => setOrg({ ...org, mailboxAddress: e.target.value })}
                    placeholder="mailroom@yourdomain.com"
                />

                <label style={S.label}>SharePoint Site URL</label>
                <input
                    style={S.input}
                    value={org.sharePointSiteUrl ?? ''}
                    onChange={e => setOrg({ ...org, sharePointSiteUrl: e.target.value })}
                    placeholder="https://yourtenant.sharepoint.com"
                />

                {org.scanInboxAddress && (
                    <>
                        <label style={S.label}>Scan Inbox Address</label>
                        <input
                            style={{ ...S.input, ...S.inputReadOnly }}
                            value={org.scanInboxAddress}
                            readOnly
                        />
                    </>
                )}

                {org.isHipaaEnabled && (
                    <div style={{ marginBottom: 16 }}>
                        <span style={S.tagGreen}>✅ HIPAA Enabled</span>
                    </div>
                )}

                <button
                    style={{ ...S.btnPrimary, opacity: saving ? 0.6 : 1 }}
                    onClick={handleSave}
                    disabled={saving}
                >
                    {saving ? 'Saving...' : 'Save Changes'}
                </button>

                {msg && <div style={{ ...S.msg, ...S.msgSuccess }}>✅ {msg}</div>}
                {error && <div style={{ ...S.msg, ...S.msgError }}>❌ {error}</div>}

            </div>
        </div>
    );
}
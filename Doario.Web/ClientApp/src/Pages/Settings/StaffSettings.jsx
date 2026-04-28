// StaffSettings.jsx

import React, { useState, useRef } from 'react';
import axios from 'axios';
import { S, Shared } from './SettingsShared';

export default function StaffSettings() {

    // ── M365 Sync ────────────────────────────────────────────────
    const [syncing, setSyncing] = useState(false);
    const [syncMsg, setSyncMsg] = useState(null);
    const [syncError, setSyncError] = useState(null);

    // ── CSV Import ───────────────────────────────────────────────
    const fileInputRef = useRef(null);
    const [csvFile, setCsvFile] = useState(null);
    const [csvLoading, setCsvLoading] = useState(false);
    const [csvResult, setCsvResult] = useState(null);
    const [csvError, setCsvError] = useState(null);

    // ── Sync handler ─────────────────────────────────────────────
    const handleSync = async () => {
        setSyncing(true);
        setSyncMsg(null);
        setSyncError(null);
        try {
            const res = await axios.post('/api/settings/sync-staff');
            setSyncMsg(res.data.message);
        } catch (err) {
            setSyncError(err.response?.data?.error || 'Sync failed.');
        } finally {
            setSyncing(false);
        }
    };

    // ── CSV handlers ─────────────────────────────────────────────
    const handleCsvChange = e => {
        setCsvFile(e.target.files[0] ?? null);
        setCsvResult(null);
        setCsvError(null);
    };

    const handleCsvImport = async () => {
        if (!csvFile) return;
        setCsvLoading(true);
        setCsvResult(null);
        setCsvError(null);
        const formData = new FormData();
        formData.append('file', csvFile);
        try {
            const res = await axios.post('/api/settings/import-staff-csv', formData, {
                headers: { 'Content-Type': 'multipart/form-data' },
            });
            setCsvResult(res.data);
        } catch (err) {
            setCsvError(err.response?.data?.message || 'Import failed.');
        } finally {
            setCsvLoading(false);
            setCsvFile(null);
            if (fileInputRef.current) fileInputRef.current.value = '';
        }
    };

    return (
        <div>
            <div style={S.sectionTitle}>Staff Management</div>
            <div style={S.sectionSub}>Sync staff from Microsoft 365 or import from a CSV file</div>

            {/* ── M365 Sync ── */}
            <div style={S.card}>
                <div style={cardTitle}>Microsoft 365 Directory Sync</div>
                <div style={cardSub}>
                    Pulls all active staff from your Microsoft 365 directory.
                    Existing staff are updated — no staff are ever deleted.
                </div>

                <button
                    style={{ ...S.btnSecondary, opacity: syncing ? 0.6 : 1 }}
                    onClick={handleSync}
                    disabled={syncing}
                >
                    {syncing ? '⟳ Syncing...' : '⟳ Sync M365 Staff'}
                </button>

                {syncMsg && <div style={{ ...S.msg, ...S.msgSuccess }}>✅ {syncMsg}</div>}
                {syncError && <div style={{ ...S.msg, ...S.msgError }}>❌ {syncError}</div>}
            </div>

            {/* ── CSV Import ── */}
            <div style={S.card}>
                <div style={cardTitle}>Import from CSV</div>
                <div style={cardSub}>
                    Upload a spreadsheet to bulk-add or update staff.
                    Required columns: FirstName, LastName, Email.
                    Optional: JobTitle, Department.
                </div>

                {/* CSV template download hint */}
                <div style={templateHint}>
                    <span style={{ fontWeight: 600 }}>Template format: </span>
                    FirstName, LastName, Email, JobTitle, Department
                </div>

                <div style={{ ...S.row, marginBottom: 12 }}>
                    <input
                        ref={fileInputRef}
                        type="file"
                        accept=".csv"
                        onChange={handleCsvChange}
                        style={{ fontSize: 12, color: '#4a6478', flex: 1 }}
                    />
                    <button
                        style={{
                            ...S.btnSecondary,
                            opacity: (!csvFile || csvLoading) ? 0.4 : 1,
                            cursor: (!csvFile || csvLoading) ? 'default' : 'pointer',
                        }}
                        onClick={handleCsvImport}
                        disabled={!csvFile || csvLoading}
                    >
                        {csvLoading ? 'Importing...' : '📂 Import CSV'}
                    </button>
                </div>

                {/* Results */}
                {csvResult && (
                    <div>
                        <div style={{ ...S.msg, ...S.msgSuccess }}>
                            ✅ {csvResult.message}
                        </div>

                        {/* Stats row */}
                        <div style={statsRow}>
                            <div style={statBox}>
                                <div style={statNum}>{csvResult.added}</div>
                                <div style={statLabel}>Added</div>
                            </div>
                            <div style={statBox}>
                                <div style={statNum}>{csvResult.updated}</div>
                                <div style={statLabel}>Updated</div>
                            </div>
                            <div style={{ ...statBox, opacity: csvResult.skipped > 0 ? 1 : 0.4 }}>
                                <div style={{ ...statNum, color: csvResult.skipped > 0 ? '#e53e3e' : '#7a9ab0' }}>
                                    {csvResult.skipped}
                                </div>
                                <div style={statLabel}>Skipped</div>
                            </div>
                        </div>

                        {/* Row errors */}
                        {csvResult.errors?.length > 0 && (
                            <div style={{ marginTop: 10 }}>
                                <div style={{ fontSize: 11, fontWeight: 600, color: '#e53e3e', marginBottom: 4 }}>
                                    Row errors:
                                </div>
                                {csvResult.errors.map(e => (
                                    <div key={e.row} style={S.csvErrRow}>
                                        Row {e.row}: {e.reason}
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                )}

                {csvError && (
                    <div style={{ ...S.msg, ...S.msgError }}>❌ {csvError}</div>
                )}
            </div>
        </div>
    );
}

// ── Local styles ─────────────────────────────────────────────────
const cardTitle = {
    fontSize: 14, fontWeight: 700, color: '#0f2d4a', marginBottom: 4,
};
const cardSub = {
    fontSize: 12, color: '#7a9ab0', marginBottom: 16, lineHeight: 1.6,
};
const templateHint = {
    fontSize: 11, color: '#4a6478',
    background: '#f7f9fb', border: '1px solid #e2eaef',
    borderRadius: 6, padding: '7px 10px',
    marginBottom: 14, fontFamily: 'monospace',
};
const statsRow = {
    display: 'flex', gap: 12, marginTop: 12,
};
const statBox = {
    flex: 1, background: '#f7f9fb',
    border: '1px solid #e2eaef', borderRadius: 8,
    padding: '10px 0', textAlign: 'center',
};
const statNum = {
    fontSize: 22, fontWeight: 800, color: '#0d9488', marginBottom: 2,
};
const statLabel = {
    fontSize: 10, color: '#7a9ab0',
    textTransform: 'uppercase', letterSpacing: '1px',
};
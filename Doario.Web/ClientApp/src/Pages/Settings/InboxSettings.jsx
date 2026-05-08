// InboxSettings.jsx

import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import { S, Shared } from './SettingsShared';

const formatDate = (dateStr) => {
    if (!dateStr) return 'Never';
    const d = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
    if (d.getFullYear() < 2020) return 'Never';
    return d.toLocaleDateString([], {
        month: 'short', day: 'numeric', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
    });
};

const formatInterval = (seconds) => {
    if (seconds < 60) return `${seconds}s`;
    if (seconds < 3600) return `${Math.round(seconds / 60)}m`;
    return `${Math.round(seconds / 3600)}h`;
};

const STATE_ICON = {
    Waiting: '⬜',
    Processing: '⏳',
    Done: '✅',
    Failed: '❌',
};

const EMPTY_FORM = {
    emailAddress: '',
    description: '',
    isFaxInbox: false,
    pollingIntervalSeconds: 60,
};

export default function InboxSettings() {
    const [inboxes, setInboxes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [msg, setMsg] = useState(null);
    const [error, setError] = useState(null);
    const [showForm, setShowForm] = useState(false);
    const [counts, setCounts] = useState({});
    const [editingId, setEditingId] = useState(null);
    const [form, setForm] = useState(EMPTY_FORM);

    // Background job state
    const [jobRunning, setJobRunning] = useState(false);
    const [jobInboxes, setJobInboxes] = useState([]);
    const [jobFinished, setJobFinished] = useState(false);

    const pollRef = useRef(null);
    const jobRunningRef = useRef(false);

    useEffect(() => {
        load();
        loadStats();
        checkJobStatus();

        const interval = setInterval(() => {
            if (!jobRunningRef.current) { load(); loadStats(); }
        }, 30000);

        return () => {
            clearInterval(interval);
            stopPolling();
        };
    }, []);

    const load = async () => {
        setLoading(true);
        try {
            const r = await axios.get('/api/settings/monitored-inboxes');
            setInboxes(r.data);
        } catch {
            setError('Failed to load inboxes.');
        } finally {
            setLoading(false);
        }
    };

    const loadStats = async () => {
        try {
            const r = await axios.get('/api/settings/monitored-inboxes/stats');
            const map = {};
            r.data.forEach(s => {
                if (s.lastFetchCount !== null && s.lastFetchCount !== undefined)
                    map[s.tenantMonitoredInboxId] = s.lastFetchCount;
            });
            setCounts(map);
        } catch { }
    };

    // ── Job polling ───────────────────────────────────────────────────────────

    const startPolling = () => {
        if (pollRef.current) return;
        pollRef.current = setInterval(checkJobStatus, 2000);
    };

    const stopPolling = () => {
        if (pollRef.current) {
            clearInterval(pollRef.current);
            pollRef.current = null;
        }
    };

    const checkJobStatus = async () => {
        try {
            const r = await axios.get('/api/settings/process-inbox-status');
            const status = r.data;

            setJobInboxes(status.inboxes ?? []);

            if (status.isRunning) {
                setJobRunning(true);
                jobRunningRef.current = true;
                startPolling();
            } else if (jobRunningRef.current && !status.isRunning) {
                setJobRunning(false);
                jobRunningRef.current = false;
                setJobFinished(true);
                stopPolling();
                await load();
                await loadStats();
                setTimeout(() => setJobFinished(false), 5000);
            }
        } catch { }
    };

    // ── Start job ─────────────────────────────────────────────────────────────

    const startJob = async (endpoint) => {
        setError(null);
        try {
            const r = await axios.post(endpoint);
            if (r.data.alreadyRunning) {
                startPolling();
                return;
            }
            if (r.data.total === 0) {
                setMsg('No active inboxes to process.');
                setTimeout(() => setMsg(null), 3000);
                return;
            }
            setJobRunning(true);
            jobRunningRef.current = true;
            setJobFinished(false);
            startPolling();
        } catch {
            setError('Failed to start processing.');
        }
    };

    const handleProcessOne = (id) => {
        if (jobRunning) return;
        startJob(`/api/settings/monitored-inboxes/${id}/process-now`);
    };

    const handleProcessNow = () => {
        if (jobRunning) return;
        startJob('/api/settings/process-inbox');
    };

    const handleAdd = () => { setEditingId(null); setForm(EMPTY_FORM); setShowForm(true); };

    const handleEdit = (inbox) => {
        setEditingId(inbox.tenantMonitoredInboxId);
        setForm({
            emailAddress: inbox.emailAddress,
            description: inbox.description || '',
            isFaxInbox: inbox.isFaxInbox,
            pollingIntervalSeconds: inbox.pollingIntervalSeconds,
        });
        setShowForm(true);
    };

    const handleSave = async () => {
        if (!form.emailAddress.trim()) { setError('Email address is required.'); return; }
        setSaving(true);
        setMsg(null);
        setError(null);
        try {
            if (editingId) {
                await axios.put(`/api/settings/monitored-inboxes/${editingId}`, form);
            } else {
                await axios.post('/api/settings/monitored-inboxes', form);
            }
            setShowForm(false);
            setEditingId(null);
            setForm(EMPTY_FORM);
            setMsg(editingId ? 'Inbox updated.' : 'Inbox added.');
            setTimeout(() => setMsg(null), 3000);
            load();
        } catch {
            setError('Failed to save inbox.');
        } finally {
            setSaving(false);
        }
    };

    const handleDeactivate = async (id) => {
        try { await axios.delete(`/api/settings/monitored-inboxes/${id}`); load(); }
        catch { setError('Failed to deactivate inbox.'); }
    };

    const handleRestore = async (id) => {
        try { await axios.post(`/api/settings/monitored-inboxes/${id}/restore`); load(); }
        catch { setError('Failed to restore inbox.'); }
    };

    const activeInboxes = inboxes.filter(i => i.isActive);
    const pastInboxes = inboxes.filter(i => !i.isActive);

    if (loading) return <Shared.Loading />;

    return (
        <div>
            <div style={S.sectionTitle}>Inbox & Fax</div>
            <div style={S.sectionSub}>
                Monitor email mailboxes for incoming mail and faxes. Each inbox is polled independently.
            </div>

            {msg && <div style={{ ...S.msg, ...S.msgSuccess }}>✅ {msg}</div>}
            {error && <div style={{ ...S.msg, ...S.msgError }}>❌ {error}</div>}

            <div style={S.card}>
                <div style={styles.cardHeader}>
                    <div>
                        <div style={S.sectionTitle}>Monitored Inboxes</div>
                        <div style={S.sectionSub}>
                            Add email addresses for Doario to monitor. Each can have its own polling interval.
                        </div>
                    </div>
                    <button style={S.btnPrimary} onClick={handleAdd}>+ Add Inbox</button>
                </div>

                {activeInboxes.length === 0 && !showForm && (
                    <div style={styles.emptyState}>
                        <div style={styles.emptyIcon}>📥</div>
                        <div style={styles.emptyTitle}>No inboxes configured</div>
                        <div style={styles.emptySub}>Add an inbox to start monitoring emails and faxes</div>
                    </div>
                )}

                {activeInboxes.map(inbox => {
                    const id = inbox.tenantMonitoredInboxId;
                    const isThisInJob = jobRunning && jobInboxes.some(j => j.inboxId === id);
                    const jobStatus = jobInboxes.find(j => j.inboxId === id);

                    return (
                        <div key={id} style={styles.inboxRow}>
                            <div style={styles.inboxLeft}>
                                <div style={styles.inboxEmail}>
                                    {inbox.isFaxInbox && <span style={styles.faxBadge}>📠 Fax</span>}
                                    {inbox.emailAddress}
                                    {jobStatus && (
                                        <span style={styles.inboxJobBadge}>
                                            {STATE_ICON[jobStatus.state]}
                                            {jobStatus.state === 'Processing' && ` ${jobStatus.documentsProcessed} docs...`}
                                            {jobStatus.state === 'Done' && ` ${jobStatus.documentsProcessed} done`}
                                            {jobStatus.state === 'Failed' && ' Failed'}
                                        </span>
                                    )}
                                </div>
                                {inbox.description && <div style={styles.inboxDesc}>{inbox.description}</div>}
                                <div style={styles.inboxMeta}>
                                    Every {formatInterval(inbox.pollingIntervalSeconds)}
                                    {' · '}
                                    <span style={{ color: inbox.lastProcessedAt && new Date(inbox.lastProcessedAt + (inbox.lastProcessedAt.endsWith('Z') ? '' : 'Z')).getFullYear() >= 2020 ? '#059669' : '#dc2626' }}>
                                        Last checked: {formatDate(inbox.lastProcessedAt)}
                                    </span>
                                    {counts[id] !== undefined && (
                                        <span style={{ color: '#7a9ab0' }}>
                                            {' · '}{counts[id]} document{counts[id] !== 1 ? 's' : ''} last fetch
                                        </span>
                                    )}
                                </div>
                            </div>
                            <div style={styles.inboxActions}>
                                <button
                                    style={{ ...styles.processBtn, opacity: jobRunning ? 0.4 : 1 }}
                                    disabled={jobRunning}
                                    title={jobRunning ? 'Processing in progress...' : 'Fetch now'}
                                    onClick={() => handleProcessOne(id)}
                                >
                                    {isThisInJob && jobStatus?.state === 'Processing' ? '⏳' : '▶'}
                                </button>
                                <button style={styles.editBtn} onClick={() => handleEdit(inbox)}>Edit</button>
                                <button style={styles.removeBtn} onClick={() => handleDeactivate(id)}>Remove</button>
                            </div>
                        </div>
                    );
                })}

                {showForm && (
                    <div style={styles.form}>
                        <div style={styles.formTitle}>{editingId ? 'Edit Inbox' : 'Add Inbox'}</div>
                        <label style={S.label}>Email Address</label>
                        <input
                            style={S.input}
                            placeholder="e.g. mailroom@company.com"
                            value={form.emailAddress}
                            onChange={e => setForm({ ...form, emailAddress: e.target.value })}
                            disabled={!!editingId}
                        />
                        <label style={S.label}>
                            Description <span style={{ color: '#7a9ab0', fontWeight: 400 }}>(optional)</span>
                        </label>
                        <input
                            style={S.input}
                            placeholder="e.g. Main fax line, Reception inbox"
                            value={form.description}
                            onChange={e => setForm({ ...form, description: e.target.value })}
                        />
                        <label style={S.label}>Check inbox every</label>
                        <div style={styles.intervalRow}>
                            <input
                                style={{ ...S.input, ...styles.intervalInput }}
                                type="number" min={10} max={3600}
                                value={form.pollingIntervalSeconds}
                                onChange={e => setForm({ ...form, pollingIntervalSeconds: parseInt(e.target.value) || 60 })}
                            />
                            <span style={styles.intervalUnit}>seconds</span>
                            <span style={styles.intervalHint}>(min 10s — currently every {formatInterval(form.pollingIntervalSeconds)})</span>
                        </div>
                        <div style={styles.toggleRow}>
                            <div style={styles.toggleInfo}>
                                <div style={styles.toggleLabel}>This is a fax inbox</div>
                                <div style={styles.toggleDesc}>All documents from this inbox will be treated as fax</div>
                            </div>
                            <button
                                style={{ ...styles.toggle, background: form.isFaxInbox ? '#0d9488' : '#d0dce6' }}
                                onClick={() => setForm({ ...form, isFaxInbox: !form.isFaxInbox })}
                            >
                                <span style={{ ...styles.toggleKnob, transform: form.isFaxInbox ? 'translateX(20px)' : 'translateX(2px)' }} />
                            </button>
                        </div>
                        <div style={styles.formActions}>
                            <button style={{ ...S.btnPrimary, opacity: saving ? 0.6 : 1 }} onClick={handleSave} disabled={saving}>
                                {saving ? 'Saving...' : editingId ? 'Update' : 'Add Inbox'}
                            </button>
                            <button style={S.btnSecondary} onClick={() => { setShowForm(false); setEditingId(null); setForm(EMPTY_FORM); }}>
                                Cancel
                            </button>
                        </div>
                    </div>
                )}
            </div>

            {pastInboxes.length > 0 && (
                <div style={S.card}>
                    <div style={S.sectionTitle}>Removed Inboxes</div>
                    <div style={S.sectionSub}>These inboxes are no longer monitored. You can restore them.</div>
                    <Shared.Divider />
                    {pastInboxes.map(inbox => (
                        <div key={inbox.tenantMonitoredInboxId} style={{ ...styles.inboxRow, opacity: 0.6 }}>
                            <div style={styles.inboxLeft}>
                                <div style={styles.inboxEmail}>{inbox.emailAddress}</div>
                                {inbox.description && <div style={styles.inboxDesc}>{inbox.description}</div>}
                            </div>
                            <button style={styles.editBtn} onClick={() => handleRestore(inbox.tenantMonitoredInboxId)}>Restore</button>
                        </div>
                    ))}
                </div>
            )}

            <div style={S.card}>
                <div style={S.sectionTitle}>Manual Processing</div>
                <div style={S.sectionSub}>
                    Trigger inbox processing right now. Processing runs on the server — safe to close or reload this page.
                </div>

                <button
                    style={{ ...S.btnSecondary, opacity: jobRunning ? 0.6 : 1 }}
                    onClick={handleProcessNow}
                    disabled={jobRunning}
                >
                    {jobRunning ? '⏳ Processing...' : '▶ Process All Inboxes Now'}
                </button>

                {(jobRunning || jobFinished) && jobInboxes.length > 0 && (
                    <div style={styles.jobStatus}>
                        {jobInboxes.map(inbox => (
                            <div key={inbox.inboxId} style={styles.jobInboxRow}>
                                <span style={styles.jobIcon}>{STATE_ICON[inbox.state] ?? '⬜'}</span>
                                <span style={styles.jobEmail}>{inbox.emailAddress}</span>
                                {inbox.state === 'Processing' && (
                                    <span style={styles.jobDocs}>{inbox.documentsProcessed} docs...</span>
                                )}
                                {inbox.state === 'Done' && (
                                    <span style={styles.jobDone}>{inbox.documentsProcessed} documents</span>
                                )}
                                {inbox.state === 'Failed' && (
                                    <span style={styles.jobError}>{inbox.error || 'Failed'}</span>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}

const styles = {
    cardHeader: { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 16 },
    emptyState: { textAlign: 'center', padding: '30px 20px' },
    emptyIcon: { fontSize: 32, marginBottom: 8 },
    emptyTitle: { fontSize: 14, fontWeight: 600, color: '#1a2e3b', marginBottom: 4 },
    emptySub: { fontSize: 12, color: '#7a9ab0' },
    inboxRow: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '12px 0', borderBottom: '1px solid #e2eaef' },
    inboxLeft: { flex: 1, minWidth: 0 },
    inboxEmail: { fontSize: 13, fontWeight: 700, color: '#1a2e3b', marginBottom: 2, display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' },
    inboxJobBadge: { fontSize: 11, color: '#0d9488', fontWeight: 600 },
    inboxDesc: { fontSize: 11, color: '#7a9ab0', marginBottom: 2 },
    inboxMeta: { fontSize: 11, color: '#7a9ab0' },
    inboxActions: { display: 'flex', gap: 8, flexShrink: 0 },
    faxBadge: { fontSize: 10, fontWeight: 700, background: '#fef3c7', color: '#92400e', padding: '2px 8px', borderRadius: 20 },
    processBtn: { padding: '5px 10px', background: '#0d9488', color: '#fff', border: 'none', borderRadius: 6, fontSize: 11, fontWeight: 700, cursor: 'pointer', fontFamily: 'inherit', transition: 'opacity 0.2s' },
    editBtn: { padding: '5px 12px', background: 'transparent', border: '1px solid #e2eaef', borderRadius: 6, fontSize: 11, fontWeight: 600, cursor: 'pointer', color: '#4a6478', fontFamily: 'inherit' },
    removeBtn: { padding: '5px 12px', background: 'transparent', border: '1px solid #fca5a5', borderRadius: 6, fontSize: 11, fontWeight: 600, cursor: 'pointer', color: '#dc2626', fontFamily: 'inherit' },
    form: { background: '#f7f9fb', border: '1px solid #e2eaef', borderRadius: 10, padding: '16px', marginTop: 12 },
    formTitle: { fontSize: 13, fontWeight: 700, color: '#0f2d4a', marginBottom: 12 },
    formActions: { display: 'flex', gap: 10, marginTop: 16 },
    intervalRow: { display: 'flex', alignItems: 'center', gap: 10, marginBottom: 14 },
    intervalInput: { width: 90, marginBottom: 0 },
    intervalUnit: { fontSize: 12, color: '#4a6478', fontWeight: 600 },
    intervalHint: { fontSize: 11, color: '#7a9ab0' },
    toggleRow: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 20, padding: '8px 0' },
    toggleInfo: { flex: 1 },
    toggleLabel: { fontSize: 13, fontWeight: 600, color: '#1a2e3b', marginBottom: 2 },
    toggleDesc: { fontSize: 12, color: '#4a6478' },
    toggle: { width: 44, height: 24, borderRadius: 12, border: 'none', cursor: 'pointer', position: 'relative', flexShrink: 0, transition: 'background 0.2s', padding: 0 },
    toggleKnob: { position: 'absolute', top: 2, width: 20, height: 20, borderRadius: '50%', background: '#fff', display: 'block', transition: 'transform 0.2s', boxShadow: '0 1px 3px rgba(0,0,0,0.2)' },
    jobStatus: { marginTop: 14, background: '#f7f9fb', border: '1px solid #e2eaef', borderRadius: 8, padding: '10px 14px', display: 'flex', flexDirection: 'column', gap: 8 },
    jobInboxRow: { display: 'flex', alignItems: 'center', gap: 10, fontSize: 12 },
    jobIcon: { fontSize: 14, flexShrink: 0 },
    jobEmail: { flex: 1, color: '#1a2e3b', fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
    jobDocs: { color: '#0d9488', fontWeight: 600, flexShrink: 0 },
    jobDone: { color: '#059669', fontWeight: 600, flexShrink: 0 },
    jobError: { color: '#dc2626', fontWeight: 600, flexShrink: 0, maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis' },
};
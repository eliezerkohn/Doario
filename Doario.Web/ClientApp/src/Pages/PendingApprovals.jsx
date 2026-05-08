// PendingApprovals.jsx — AI suggestion queue

import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import AssignModal from './AssignModal';

const formatDate = (dateStr) => {
    const d = new Date(dateStr + 'Z');
    const now = new Date();
    const isToday = d.toDateString() === now.toDateString();
    if (isToday) return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return d.toLocaleDateString([], { month: 'short', day: 'numeric' });
};

const confColor = (c) => c >= 8 ? '#065f46' : c >= 5 ? '#92400e' : '#991b1b';
const confBg = (c) => c >= 8 ? '#d1fae5' : c >= 5 ? '#fef3c7' : '#fee2e2';

const PendingApprovals = ({ staff, selected, onSelect, onApproved }) => {
    const [suggestions, setSuggestions] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [approving, setApproving] = useState(null);

    // Background job state
    const [jobRunning, setJobRunning] = useState(false);
    const [jobApproved, setJobApproved] = useState(0);
    const [jobTotal, setJobTotal] = useState(0);
    const [jobFailed, setJobFailed] = useState(0);
    const [jobFinished, setJobFinished] = useState(false);

    const [overwriteDoc, setOverwriteDoc] = useState(null);
    const pollRef = useRef(null);

    const load = async () => {
        setLoading(true);
        try {
            const r = await axios.get('/api/assignment/pending-suggestions');
            setSuggestions(r.data);
        } catch {
            setError('Failed to load pending suggestions.');
        } finally {
            setLoading(false);
        }
    };

    // Check if a job is already running on mount
    useEffect(() => {
        load();
        checkJobStatus();
    }, []);

    // Poll job status every 2 seconds while running
    const startPolling = () => {
        if (pollRef.current) return;
        pollRef.current = setInterval(async () => {
            await checkJobStatus();
        }, 2000);
    };

    const stopPolling = () => {
        if (pollRef.current) {
            clearInterval(pollRef.current);
            pollRef.current = null;
        }
    };

    useEffect(() => {
        return () => stopPolling();
    }, []);

    const checkJobStatus = async () => {
        try {
            const r = await axios.get('/api/assignment/approve-all-status');
            const status = r.data;

            setJobApproved(status.approved);
            setJobTotal(status.total);
            setJobFailed(status.failed);

            if (status.isRunning) {
                setJobRunning(true);
                startPolling();
                // Update sidebar count as approvals come in
                onApproved();
            } else if (jobRunning && !status.isRunning) {
                // Job just finished
                setJobRunning(false);
                setJobFinished(true);
                stopPolling();
                setTimeout(() => setJobFinished(false), 4000);
                // Reload list and update sidebar
                await load();
                onApproved();
            }
        } catch { }
    };

    const handleApprove = async (suggestion) => {
        setApproving(suggestion.documentAiSuggestionId);
        try {
            await axios.post(`/api/assignment/approve/${suggestion.documentAiSuggestionId}`);
            setSuggestions(prev => prev.filter(s => s.documentAiSuggestionId !== suggestion.documentAiSuggestionId));
            onApproved();
        } catch {
            setError('Failed to approve suggestion.');
        } finally {
            setApproving(null);
        }
    };

    // Fire approve-all — returns immediately, processing continues server-side
    // Browser reload safe — job keeps running on the server
    const handleApproveAll = async () => {
        setError(null);
        try {
            const r = await axios.post('/api/assignment/approve-all');
            if (r.data.alreadyRunning) {
                // Job already running — just start polling
                startPolling();
                return;
            }
            if (r.data.total === 0) return;
            setJobTotal(r.data.total);
            setJobApproved(0);
            setJobFailed(0);
            setJobRunning(true);
            setJobFinished(false);
            startPolling();
        } catch {
            setError('Failed to start approval job.');
        }
    };

    const handleOverwriteAssigned = () => {
        setOverwriteDoc(null);
        load();
        onApproved();
    };

    const progressPct = jobTotal > 0
        ? Math.round((jobApproved / jobTotal) * 100)
        : 0;

    return (
        <div style={styles.panel}>
            <div style={styles.header}>
                <div style={styles.headerTop}>
                    <h2 style={styles.title}>Pending Approvals</h2>
                    {(suggestions.length > 0 || jobRunning) && (
                        <button
                            style={{ ...styles.approveAllBtn, opacity: jobRunning ? 0.6 : 1 }}
                            onClick={handleApproveAll}
                            disabled={jobRunning}
                        >
                            {jobRunning
                                ? `⏳ ${jobApproved}/${jobTotal}`
                                : `✓ Approve All (${suggestions.length})`}
                        </button>
                    )}
                </div>
                <div style={styles.headerSub}>
                    AI has suggested staff for these documents. Review and approve, or reassign.
                </div>

                {/* Live progress bar — updates from server polling */}
                {jobRunning && jobTotal > 0 && (
                    <div style={styles.progressWrap}>
                        <div style={styles.progressTrack}>
                            <div style={{ ...styles.progressFill, width: `${progressPct}%` }} />
                        </div>
                        <div style={styles.progressText}>
                            {jobApproved} of {jobTotal} approved — processing on server, safe to reload
                        </div>
                    </div>
                )}

                {/* Finished message */}
                {!jobRunning && jobFinished && (
                    <div style={styles.progressBar}>
                        ✓ {jobApproved} approved{jobFailed > 0 ? `, ${jobFailed} failed` : ''}
                    </div>
                )}
            </div>

            {error && (
                <div style={styles.error}>
                    {error}
                    <button style={styles.errorClose} onClick={() => setError(null)}>✕</button>
                </div>
            )}

            <div style={styles.list}>
                {loading && <div style={styles.empty}>Loading…</div>}
                {!loading && suggestions.length === 0 && !jobRunning && (
                    <div style={styles.emptyState}>
                        <div style={styles.emptyIcon}>🤖</div>
                        <div style={styles.emptyTitle}>No pending suggestions</div>
                        <div style={styles.emptySub}>
                            AI suggestions will appear here when new documents arrive
                        </div>
                    </div>
                )}

                {suggestions.map(s => {
                    const isSelected = selected?.documentId === s.documentId;
                    const isApproving = approving === s.documentAiSuggestionId;

                    return (
                        <div
                            key={s.documentAiSuggestionId}
                            style={{ ...styles.item, ...(isSelected ? styles.itemSelected : {}) }}
                            onClick={() => onSelect({
                                documentId: s.documentId,
                                originalFileName: s.originalFileName,
                                uploadedAt: s.uploadedAt,
                                statusId: 1,
                                statusName: 'Unassigned',
                            })}
                        >
                            <div style={styles.itemBody}>
                                <div style={styles.itemTop}>
                                    <span style={styles.sender}>{s.senderDisplayName || 'Unknown Sender'}</span>
                                    <span style={styles.date}>{formatDate(s.createdAt)}</span>
                                </div>
                                <div style={styles.filename}>{s.originalFileName}</div>
                                <div style={styles.suggestionRow}>
                                    <span style={styles.suggestionLabel}>AI suggests:</span>
                                    <span style={styles.suggestionName}>{s.suggestedStaffName}</span>
                                    <span style={{
                                        ...styles.confBadge,
                                        background: confBg(s.confidence),
                                        color: confColor(s.confidence)
                                    }}>
                                        {s.confidence}/10
                                    </span>
                                </div>
                                <div style={styles.actions} onClick={e => e.stopPropagation()}>
                                    <button
                                        style={{
                                            ...styles.approveBtn,
                                            opacity: (isApproving || jobRunning) ? 0.6 : 1
                                        }}
                                        disabled={isApproving || jobRunning}
                                        onClick={() => handleApprove(s)}
                                    >
                                        {isApproving ? 'Approving…' : '✓ Approve'}
                                    </button>
                                    <button
                                        style={{
                                            ...styles.overwriteBtn,
                                            opacity: jobRunning ? 0.5 : 1
                                        }}
                                        disabled={jobRunning}
                                        onClick={() => setOverwriteDoc({
                                            documentId: s.documentId,
                                            originalFileName: s.originalFileName,
                                            _suggestionId: s.documentAiSuggestionId,
                                        })}
                                    >
                                        ↩ Reassign
                                    </button>
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>

            {overwriteDoc && (
                <AssignModal
                    doc={overwriteDoc}
                    staff={staff}
                    suggestionId={overwriteDoc._suggestionId}
                    isOverwrite={true}
                    onClose={() => setOverwriteDoc(null)}
                    onAssigned={handleOverwriteAssigned}
                    onReverted={() => setOverwriteDoc(null)}
                />
            )}
        </div>
    );
};

const styles = {
    panel: { width: 340, minWidth: 340, background: '#faf9f8', borderRight: '1px solid #edebe9', display: 'flex', flexDirection: 'column', height: '100vh', fontFamily: "'Plus Jakarta Sans', sans-serif" },
    header: { padding: '14px 16px 10px', borderBottom: '1px solid #edebe9', background: '#fff' },
    headerTop: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 4 },
    title: { margin: 0, fontSize: 16, fontWeight: 700, color: '#1a2e3b' },
    headerSub: { fontSize: 11, color: '#6b8499' },
    progressWrap: { marginTop: 8 },
    progressTrack: { height: 4, background: '#e2eaef', borderRadius: 4, overflow: 'hidden', marginBottom: 4 },
    progressFill: { height: '100%', background: '#0d9488', borderRadius: 4, transition: 'width 0.3s ease' },
    progressText: { fontSize: 11, color: '#0d9488', fontWeight: 600 },
    progressBar: { marginTop: 8, fontSize: 11, color: '#0d9488', fontWeight: 600, background: '#f0fdf9', padding: '6px 10px', borderRadius: 6 },
    approveAllBtn: { padding: '6px 14px', background: '#0d9488', color: '#fff', border: 'none', borderRadius: 8, fontSize: 11, fontWeight: 700, cursor: 'pointer', fontFamily: 'inherit' },
    error: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 16px', background: '#fee2e2', borderBottom: '1px solid #fca5a5', fontSize: 12, color: '#991b1b' },
    errorClose: { background: 'transparent', border: 'none', cursor: 'pointer', fontSize: 12, color: '#991b1b', padding: 0, marginLeft: 8 },
    list: { flex: 1, overflowY: 'auto' },
    empty: { padding: '30px 20px', textAlign: 'center', color: '#a19f9d', fontSize: 13 },
    emptyState: { flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, padding: '60px 20px' },
    emptyIcon: { fontSize: 36 },
    emptyTitle: { fontSize: 14, fontWeight: 600, color: '#323130' },
    emptySub: { fontSize: 12, color: '#a19f9d', textAlign: 'center' },
    item: { display: 'flex', padding: '12px 14px', borderBottom: '1px solid #edebe9', cursor: 'pointer', background: '#faf9f8' },
    itemSelected: { background: '#e6f7f5', borderLeft: '3px solid #0d9488' },
    itemBody: { flex: 1, minWidth: 0 },
    itemTop: { display: 'flex', justifyContent: 'space-between', marginBottom: 2 },
    sender: { fontSize: 12, fontWeight: 700, color: '#1a2e3b', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 180 },
    date: { fontSize: 10, color: '#a19f9d', flexShrink: 0 },
    filename: { fontSize: 11, color: '#6b8499', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginBottom: 6 },
    suggestionRow: { display: 'flex', alignItems: 'center', gap: 6, marginBottom: 8 },
    suggestionLabel: { fontSize: 10, color: '#6b8499' },
    suggestionName: { fontSize: 11, fontWeight: 700, color: '#1a2e3b' },
    confBadge: { fontSize: 10, fontWeight: 700, padding: '1px 7px', borderRadius: 20, flexShrink: 0 },
    actions: { display: 'flex', gap: 8 },
    approveBtn: { padding: '5px 14px', background: '#0d9488', color: '#fff', border: 'none', borderRadius: 6, fontSize: 11, fontWeight: 700, cursor: 'pointer', fontFamily: 'inherit' },
    overwriteBtn: { padding: '5px 14px', background: 'transparent', color: '#4a6478', border: '1px solid #d0dce6', borderRadius: 6, fontSize: 11, fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit' },
};

export default PendingApprovals;
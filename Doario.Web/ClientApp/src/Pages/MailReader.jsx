// MailReader.jsx — Light A style

import React, { useState, useEffect } from 'react';
import axios from 'axios';

const formatFullDate = (dateStr) => {
    const d = new Date(dateStr + 'Z');
    return d.toLocaleDateString([], {
        weekday: 'long', year: 'numeric',
        month: 'long', day: 'numeric',
        hour: '2-digit', minute: '2-digit'
    });
};

const statusLabel = (statusId) => ({
    1: { label: 'Unassigned', color: '#d97706', bg: '#fef3c7' },
    2: { label: 'Assigned', color: '#059669', bg: '#d1fae5' },
    4: { label: 'Actioned', color: '#6b7280', bg: '#f3f4f6' },
    7: { label: 'Spam', color: '#dc2626', bg: '#fee2e2' },
    8: { label: 'Promotion', color: '#7c3aed', bg: '#ede9fe' },
    9: { label: 'Trashed', color: '#6b7280', bg: '#f3f4f6' },
}[statusId] ?? { label: 'Unknown', color: '#6b7280', bg: '#f3f4f6' });

const formatSummary = (html) => {
    if (!html) return '';
    return html
        .replace(/<br>/gi, '')
        .replace(/(<strong>(?!Document Type))/g, '<br>$1');
};

const MailReader = ({ doc, staff, onAssign, localAssigned, onStatusChanged, onDeleted }) => {
    const [moving, setMoving] = useState(false);
    const [feedback, setFeedback] = useState(null);
    const [assignment, setAssignment] = useState(null);
    const [confirmDelete, setConfirmDelete] = useState(false);
    const [checkData, setCheckData] = useState(null);

    useEffect(() => {
        setAssignment(null);
        setFeedback(null);
        setConfirmDelete(false);
        setCheckData(null);
        if (!doc) return;

        axios.get(`/api/assignment/${doc.documentId}`)
            .then(res => setAssignment(res.data))
            .catch(() => setAssignment(null));

        axios.get(`/api/admin/check/${doc.documentId}`)
            .then(res => setCheckData(res.data))
            .catch(() => setCheckData(null));
    }, [doc?.documentId]);

    if (!doc) {
        return (
            <div style={styles.empty}>
                <div style={styles.emptyIcon}>📬</div>
                <div style={styles.emptyTitle}>Select a document</div>
                <div style={styles.emptySub}>Click any item in the list to read it</div>
            </div>
        );
    }

    const effectiveStatusId = localAssigned?.current[doc.documentId] ? 2 : doc.statusId;
    const status = statusLabel(effectiveStatusId);
    const isAssigned = effectiveStatusId === 2;
    const isSpam = effectiveStatusId === 7;
    const isPromotion = effectiveStatusId === 8;
    const isTrashed = effectiveStatusId === 9;
    const isSpamOrPromo = isSpam || isPromotion;
    const canAssign = !!doc.aiSummary && staff.length > 0 && !isSpamOrPromo && !isTrashed;

    const handleNotSpam = async () => {
        setMoving(true); setFeedback(null);
        try {
            const res = await axios.post('/api/feedback/not-spam', { documentId: doc.documentId });
            onStatusChanged(doc.documentId, 1, 'Unassigned');
            setFeedback(res.data.senderWhitelisted
                ? `✅ Sender "${res.data.senderIdentifier}" whitelisted — future mail always lands in Inbox`
                : '✅ Document moved to Inbox');
        } catch { setFeedback('Something went wrong.'); }
        finally { setMoving(false); }
    };

    const handleNotPromotion = async () => {
        setMoving(true); setFeedback(null);
        try {
            await axios.post('/api/feedback/not-promotion', { documentId: doc.documentId });
            onStatusChanged(doc.documentId, 1, 'Unassigned');
            setFeedback('✅ Moved to Inbox — AI will learn from this correction');
        } catch { setFeedback('Something went wrong.'); }
        finally { setMoving(false); }
    };

    const handleTrash = async () => {
        setMoving(true); setFeedback(null);
        try {
            await axios.post('/api/admin/trash', { documentId: doc.documentId });
            onStatusChanged(doc.documentId, 9, 'Trashed');
            setFeedback('🗑 Moved to Trash — you can restore it from the Trash folder.');
        } catch { setFeedback('Something went wrong.'); }
        finally { setMoving(false); }
    };

    const handleRestore = async () => {
        setMoving(true); setFeedback(null);
        try {
            await axios.post('/api/admin/restore', { documentId: doc.documentId });
            onStatusChanged(doc.documentId, 1, 'Unassigned');
            setFeedback('✅ Document restored to Inbox.');
        } catch { setFeedback('Something went wrong.'); }
        finally { setMoving(false); }
    };

    const handleDeleteForever = async () => {
        setMoving(true); setFeedback(null);
        try {
            await axios.delete(`/api/admin/delete/${doc.documentId}`);
            if (onDeleted) onDeleted(doc.documentId);
        } catch { setFeedback('Something went wrong.'); setMoving(false); }
    };

    return (
        <div style={styles.reader}>

            {/* Top bar */}
            <div style={styles.topBar}>
                <div style={styles.topLeft}>
                    <span style={{ ...styles.statusPill, color: status.color, background: status.bg }}>
                        {status.label}
                    </span>
                    {checkData && (
                        <span style={styles.checkPill}>
                            💰 Check
                        </span>
                    )}
                    {!doc.aiSummary && !isTrashed && (
                        <span style={styles.processingPill}>
                            {doc.ocrText ? '⏳ Generating summary…' : '⏳ Processing…'}
                        </span>
                    )}
                </div>
                <div style={styles.topRight}>

                    {/* Trashed document actions */}
                    {isTrashed && (
                        <>
                            <button style={styles.restoreBtn} onClick={handleRestore} disabled={moving}>
                                {moving ? 'Restoring…' : '↩ Restore'}
                            </button>
                            {!confirmDelete ? (
                                <button style={styles.deleteForeverBtn}
                                    onClick={() => setConfirmDelete(true)} disabled={moving}>
                                    🗑 Delete Forever
                                </button>
                            ) : (
                                <div style={styles.confirmWrap}>
                                    <span style={styles.confirmText}>Are you sure?</span>
                                    <button style={styles.confirmYes}
                                        onClick={handleDeleteForever} disabled={moving}>
                                        {moving ? 'Deleting…' : 'Yes, delete'}
                                    </button>
                                    <button style={styles.confirmNo}
                                        onClick={() => setConfirmDelete(false)}>
                                        Cancel
                                    </button>
                                </div>
                            )}
                        </>
                    )}

                    {/* Normal document actions */}
                    {!isTrashed && (
                        <>
                            {isSpam && (
                                <button style={styles.moveBtnRed} onClick={handleNotSpam} disabled={moving}>
                                    {moving ? 'Moving…' : '✓ Not Spam'}
                                </button>
                            )}
                            {isPromotion && (
                                <button style={styles.moveBtnPurple} onClick={handleNotPromotion} disabled={moving}>
                                    {moving ? 'Moving…' : '✓ This is real mail'}
                                </button>
                            )}
                            {!isSpamOrPromo && (
                                <button
                                    style={{ ...styles.assignBtn, ...(canAssign ? {} : styles.assignBtnDisabled) }}
                                    disabled={!canAssign}
                                    title={!doc.aiSummary ? 'Waiting for AI summary' : ''}
                                    onClick={() => onAssign(doc)}
                                >
                                    {isAssigned ? 'Reassign' : 'Assign'}
                                </button>
                            )}
                            <button style={styles.trashBtn} onClick={handleTrash}
                                disabled={moving} title="Move to Trash">
                                🗑
                            </button>
                        </>
                    )}

                    {doc.sharePointUrl && (
                        <a href={doc.sharePointUrl} target="_blank" rel="noreferrer" style={styles.spLink}>
                            View in SharePoint ↗
                        </a>
                    )}
                </div>
            </div>

            {/* Feedback banner */}
            {feedback && <div style={styles.feedbackBanner}>{feedback}</div>}

            {/* Trashed banner */}
            {isTrashed && !feedback && (
                <div style={styles.trashedBanner}>
                    🗑 This document is in the Trash. Restore it to bring it back to the Inbox, or delete it forever to remove it permanently.
                </div>
            )}

            {/* Spam/Promo explanation */}
            {isSpam && !feedback && (
                <div style={styles.spamBanner}>
                    🚫 <b>Spam</b> — click "Not Spam" to move to Inbox and permanently whitelist this sender.
                </div>
            )}
            {isPromotion && !feedback && (
                <div style={styles.promoBanner}>
                    📢 <b>Promotion</b> — click "This is real mail" if it needs attention. The sender will not be whitelisted because they may send real mail too.
                </div>
            )}

            {/* Header */}
            <div style={styles.header}>
                <div style={styles.avatar}>
                    {(doc.senderDisplayName || 'U')[0].toUpperCase()}
                </div>
                <div style={styles.headerInfo}>
                    <div style={styles.senderName}>
                        {doc.senderDisplayName || 'Unknown Sender'}
                        {doc.senderEmail && (
                            <a href={`mailto:${doc.senderEmail}`} style={styles.senderEmail}>
                                &lt;{doc.senderEmail}&gt;
                            </a>
                        )}
                    </div>
                    <div style={styles.filename}>{doc.originalFileName}</div>
                    <div style={styles.date}>{formatFullDate(doc.uploadedAt)}</div>
                </div>
            </div>

            <div style={styles.divider} />

            {/* Assignment strip */}
            {assignment && (
                <div style={styles.assignmentStrip}>
                    <span style={styles.assignmentIcon}>👤</span>
                    <span style={styles.assignmentText}>
                        Assigned to <strong>{assignment.staffName}</strong>
                        <span style={styles.assignmentEmail}> &lt;{assignment.assignedToEmail}&gt;</span>
                        <span style={styles.assignmentDate}>
                            {' '}· {new Date(assignment.assignedAt + 'Z').toLocaleDateString([], {
                                month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
                            })}
                        </span>
                    </span>
                    {assignment.note && (
                        <span style={styles.assignmentNote}>📝 {assignment.note}</span>
                    )}
                    {assignment.deliveryStatus === 'permanent_fail' && (
                        <span style={styles.deliveryFail}
                            title={assignment.deliveryError || 'Email could not be delivered after 3 attempts.'}>
                            ⚠ Email failed — address may not exist
                        </span>
                    )}
                    {assignment.deliveryStatus === 'failed' && (
                        <span style={styles.deliveryWarn}
                            title={assignment.deliveryError || 'Delivery failed, retrying…'}>
                            ⏳ Email retrying…
                        </span>
                    )}
                </div>
            )}

            {/* AI Summary */}
            <div style={styles.body}>
                {doc.aiSummary ? (
                    <>
                        <div style={styles.summaryHeader}>
                            <span style={styles.summaryDot} />
                            <span style={styles.summaryTitle}>AI Summary</span>
                        </div>
                        <div style={styles.summaryBox}>
                            <div
                                style={styles.summary}
                                dangerouslySetInnerHTML={{ __html: formatSummary(doc.aiSummary) }}
                            />
                        </div>
                    </>
                ) : doc.ocrText ? (
                    <div style={styles.pending}>⏳ AI is generating the summary…</div>
                ) : (
                    <div style={styles.pending}>⏳ Document is being processed…</div>
                )}

                {checkData && (
                    <div style={styles.checkPanel}>
                        <div style={styles.checkPanelHeader}>
                            <span style={styles.checkPanelDot} />
                            <span style={styles.checkPanelTitle}>Check Detected</span>
                        </div>
                        <div style={styles.checkPanelBox}>
                            <div style={styles.checkGrid}>
                                <div style={styles.checkField}>
                                    <span style={styles.checkLabel}>Amount</span>
                                    <span style={styles.checkValue}>
                                        {checkData.checkAmount
                                            ? '$' + parseFloat(checkData.checkAmount).toLocaleString('en-US', { minimumFractionDigits: 2 })
                                            : '—'}
                                    </span>
                                </div>
                                <div style={styles.checkField}>
                                    <span style={styles.checkLabel}>Payer</span>
                                    <span style={styles.checkValue}>{checkData.checkPayerName || '—'}</span>
                                </div>
                                <div style={styles.checkField}>
                                    <span style={styles.checkLabel}>Check Number</span>
                                    <span style={styles.checkValue}>{checkData.checkNumber || '—'}</span>
                                </div>
                                <div style={styles.checkField}>
                                    <span style={styles.checkLabel}>Received</span>
                                    <span style={styles.checkValue}>{new Date(checkData.createdAt + 'Z').toLocaleDateString([], { month: 'short', day: 'numeric', year: 'numeric' })}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

const styles = {
    empty: {
        flex: 1, display: 'flex', flexDirection: 'column',
        alignItems: 'center', justifyContent: 'center',
        background: '#f0f4f8', color: '#6b8499', gap: 8,
    },
    emptyIcon: { fontSize: 48, marginBottom: 8 },
    emptyTitle: { fontSize: 16, fontWeight: 600, color: '#1a2e3b' },
    emptySub: { fontSize: 13 },
    reader: {
        flex: 1, background: '#fff', display: 'flex',
        flexDirection: 'column', height: '100vh', overflowY: 'auto',
        fontFamily: "'Plus Jakarta Sans', sans-serif",
    },
    topBar: {
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '14px 24px', borderBottom: '1px solid #e2eaef',
        gap: 12, flexWrap: 'wrap', background: '#fff',
    },
    topLeft: { display: 'flex', alignItems: 'center', gap: 8 },
    topRight: { display: 'flex', alignItems: 'center', gap: 10 },
    statusPill: {
        fontSize: 11, fontWeight: 600, padding: '4px 12px',
        borderRadius: 20, letterSpacing: 0.3,
    },
    processingPill: {
        fontSize: 11, color: '#d97706', background: '#fef3c7',
        padding: '4px 12px', borderRadius: 20,
    },
    assignBtn: {
        padding: '8px 20px', borderRadius: 8, border: 'none',
        fontSize: 12, fontWeight: 700, cursor: 'pointer',
        background: '#0d9488', color: '#fff', fontFamily: 'inherit',
    },
    assignBtnDisabled: { background: '#e2eaef', color: '#6b8499', cursor: 'not-allowed' },
    trashBtn: {
        padding: '7px 10px', borderRadius: 8, border: '1px solid #e2eaef',
        background: '#fff', color: '#6b7280', fontSize: 14,
        cursor: 'pointer', fontFamily: 'inherit', lineHeight: 1,
    },
    restoreBtn: {
        padding: '7px 16px', borderRadius: 8, border: '1px solid #0d9488',
        background: '#fff', color: '#0d9488', fontSize: 12,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    deleteForeverBtn: {
        padding: '7px 16px', borderRadius: 8, border: '1px solid #dc2626',
        background: '#fff', color: '#dc2626', fontSize: 12,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    confirmWrap: { display: 'flex', alignItems: 'center', gap: 8 },
    confirmText: { fontSize: 12, color: '#dc2626', fontWeight: 600 },
    confirmYes: {
        padding: '6px 14px', borderRadius: 8, border: 'none',
        background: '#dc2626', color: '#fff', fontSize: 12,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    confirmNo: {
        padding: '6px 14px', borderRadius: 8, border: '1px solid #e2eaef',
        background: '#fff', color: '#6b7280', fontSize: 12,
        cursor: 'pointer', fontFamily: 'inherit',
    },
    moveBtnRed: {
        padding: '7px 16px', borderRadius: 8, border: '1px solid #dc2626',
        background: '#fff', color: '#dc2626', fontSize: 12,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    moveBtnPurple: {
        padding: '7px 16px', borderRadius: 8, border: '1px solid #7c3aed',
        background: '#fff', color: '#7c3aed', fontSize: 12,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    spLink: { fontSize: 12, color: '#0d9488', textDecoration: 'none', fontWeight: 500 },
    feedbackBanner: {
        padding: '10px 24px', background: '#d1fae5',
        borderBottom: '1px solid #99e0d9', fontSize: 12, color: '#065f46',
    },
    trashedBanner: {
        padding: '10px 24px', background: '#f3f4f6',
        borderBottom: '1px solid #e2eaef', fontSize: 12, color: '#6b7280',
    },
    spamBanner: {
        padding: '10px 24px', background: '#fee2e2',
        borderBottom: '1px solid #fca5a5', fontSize: 12, color: '#991b1b',
    },
    promoBanner: {
        padding: '10px 24px', background: '#ede9fe',
        borderBottom: '1px solid #c4b5fd', fontSize: 12, color: '#5b21b6',
    },
    header: { display: 'flex', alignItems: 'flex-start', gap: 14, padding: '20px 24px' },
    avatar: {
        width: 44, height: 44, borderRadius: 10,
        background: 'linear-gradient(135deg, #0d9488, #0f2d4a)',
        color: '#fff', display: 'flex', alignItems: 'center',
        justifyContent: 'center', fontSize: 18, fontWeight: 800, flexShrink: 0,
    },
    headerInfo: { flex: 1, minWidth: 0 },
    senderName: {
        fontSize: 15, fontWeight: 700, color: '#1a2e3b',
        display: 'flex', alignItems: 'baseline', gap: 6,
        flexWrap: 'wrap', marginBottom: 3,
    },
    senderEmail: { fontSize: 12, color: '#0d9488', textDecoration: 'none', fontWeight: 400 },
    filename: { fontSize: 12, color: '#6b8499', marginBottom: 2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
    date: { fontSize: 11, color: '#6b8499' },
    divider: { height: 1, background: '#e2eaef', margin: '0 24px' },
    assignmentStrip: {
        display: 'flex', alignItems: 'baseline', flexWrap: 'wrap', gap: 6,
        padding: '10px 24px', background: '#f0fdf4',
        borderBottom: '1px solid #bbf7d0', fontSize: 12,
    },
    assignmentIcon: { fontSize: 13, flexShrink: 0 },
    assignmentText: { color: '#065f46' },
    assignmentEmail: { color: '#059669', fontWeight: 400 },
    assignmentDate: { color: '#6b8499' },
    assignmentNote: {
        marginLeft: 8, color: '#92400e', background: '#fef3c7',
        padding: '2px 8px', borderRadius: 4, fontSize: 11,
    },
    deliveryFail: {
        marginLeft: 8, color: '#991b1b', background: '#fee2e2',
        padding: '2px 8px', borderRadius: 4, fontSize: 11,
        fontWeight: 600, cursor: 'help',
    },
    deliveryWarn: {
        marginLeft: 8, color: '#92400e', background: '#fef3c7',
        padding: '2px 8px', borderRadius: 4, fontSize: 11, cursor: 'help',
    },
    body: { padding: '20px 24px', flex: 1 },
    summaryHeader: { display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 },
    summaryDot: { width: 8, height: 8, borderRadius: '50%', background: '#0d9488' },
    summaryTitle: {
        fontSize: 10, fontWeight: 700, color: '#0d9488',
        textTransform: 'uppercase', letterSpacing: 2,
    },
    summaryBox: {
        border: '1px solid #e2eaef', borderRadius: 10,
        padding: '16px 18px', background: '#f0f4f8',
    },
    summary: { fontSize: 14, lineHeight: 2, color: '#1a2e3b' },
    pending: { fontSize: 14, color: '#6b8499', padding: '20px 0' },
    checkPill: {
        fontSize: 11, fontWeight: 700, padding: '4px 12px',
        borderRadius: 20, background: '#fef3c7', color: '#92400e',
    },
    checkPanel: { marginTop: 24 },
    checkPanelHeader: { display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 },
    checkPanelDot: { width: 8, height: 8, borderRadius: '50%', background: '#d97706' },
    checkPanelTitle: {
        fontSize: 10, fontWeight: 700, color: '#d97706',
        textTransform: 'uppercase', letterSpacing: 2,
    },
    checkPanelBox: {
        border: '1px solid #fde68a', borderRadius: 10,
        padding: '16px 18px', background: '#fffbeb',
    },
    checkGrid: {
        display: 'grid', gridTemplateColumns: '1fr 1fr',
        gap: '14px 24px',
    },
    checkField: { display: 'flex', flexDirection: 'column', gap: 3 },
    checkLabel: { fontSize: 10, fontWeight: 700, color: '#92400e', textTransform: 'uppercase', letterSpacing: 1 },
    checkValue: { fontSize: 15, fontWeight: 700, color: '#1a2e3b' },
};

export default MailReader;
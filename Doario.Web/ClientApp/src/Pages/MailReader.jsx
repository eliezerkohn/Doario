// MailReader.jsx — Light A style

import React, { useState } from 'react';
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
}[statusId] ?? { label: 'Unknown', color: '#6b7280', bg: '#f3f4f6' });

const formatSummary = (html) => {
    if (!html) return '';
    return html
        .replace(/<br>/gi, '')
        .replace(/(<strong>(?!Document Type))/g, '<br>$1');
};

const MailReader = ({ doc, staff, onAssign, localAssigned, onStatusChanged }) => {
    const [moving, setMoving] = useState(false);
    const [feedback, setFeedback] = useState(null);

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
    const isSpamOrPromo = isSpam || isPromotion;
    const canAssign = !!doc.aiSummary && staff.length > 0 && !isSpamOrPromo;

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

    return (
        <div style={styles.reader}>

            {/* Top bar */}
            <div style={styles.topBar}>
                <div style={styles.topLeft}>
                    <span style={{ ...styles.statusPill, color: status.color, background: status.bg }}>
                        {status.label}
                    </span>
                    {!doc.aiSummary && (
                        <span style={styles.processingPill}>
                            {doc.ocrText ? '⏳ Generating summary…' : '⏳ Processing…'}
                        </span>
                    )}
                </div>
                <div style={styles.topRight}>
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
                    {doc.sharePointUrl && (
                        <a href={doc.sharePointUrl} target="_blank" rel="noreferrer" style={styles.spLink}>
                            View in SharePoint ↗
                        </a>
                    )}
                </div>
            </div>

            {/* Feedback banner */}
            {feedback && <div style={styles.feedbackBanner}>{feedback}</div>}

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
    spamBanner: {
        padding: '10px 24px', background: '#fee2e2',
        borderBottom: '1px solid #fca5a5', fontSize: 12, color: '#991b1b',
    },
    promoBanner: {
        padding: '10px 24px', background: '#ede9fe',
        borderBottom: '1px solid #c4b5fd', fontSize: 12, color: '#5b21b6',
    },

    header: {
        display: 'flex', alignItems: 'flex-start',
        gap: 14, padding: '20px 24px',
    },
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

    body: { padding: '20px 24px', flex: 1 },

    summaryHeader: {
        display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12,
    },
    summaryDot: {
        width: 8, height: 8, borderRadius: '50%', background: '#0d9488',
    },
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
};

export default MailReader;
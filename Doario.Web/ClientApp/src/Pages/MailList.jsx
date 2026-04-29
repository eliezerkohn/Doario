// MailList.jsx — Light A style

import React, { useState } from 'react';

const stripMarkup = (html) => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
};

const formatDate = (dateStr) => {
    const d = new Date(dateStr + 'Z');
    const now = new Date();
    const isToday = d.toDateString() === now.toDateString();
    if (isToday) return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return d.toLocaleDateString([], { month: 'short', day: 'numeric' });
};

const statusColor = (statusId) => ({
    1: '#d97706', 2: '#059669', 4: '#6b7280',
    7: '#dc2626', 8: '#7c3aed',
}[statusId] ?? '#6b7280');

const MailList = ({ docs, selected, loading, folder, onSelect, onMarkUnread }) => {
    const [search, setSearch] = useState('');
    const [menuDocId, setMenuDocId] = useState(null);

    const filtered = docs.filter(d => {
        if (!search) return true;
        const q = search.toLowerCase();
        return (
            d.originalFileName?.toLowerCase().includes(q) ||
            stripMarkup(d.aiSummary).toLowerCase().includes(q) ||
            d.ocrText?.toLowerCase().includes(q)
        );
    });

    return (
        <div style={styles.panel}>
            <div style={styles.header}>
                <h2 style={styles.title}>{folder}</h2>
                <span style={styles.count}>{docs.length}</span>
            </div>

            <div style={styles.searchWrap}>
                <div style={styles.searchRow}>
                    <input
                        style={styles.search}
                        placeholder="Search mail…"
                        value={search}
                        onChange={e => setSearch(e.target.value)}
                    />
                    {search && (
                        <button
                            style={styles.clearBtn}
                            onClick={() => setSearch('')}
                            title="Clear search"
                        >
                            ✕
                        </button>
                    )}
                </div>
            </div>

            <div style={styles.list}>
                {loading && <div style={styles.empty}>Loading…</div>}
                {!loading && filtered.length === 0 && (
                    <div style={styles.empty}>No documents in {folder}</div>
                )}
                {filtered.map(doc => {
                    const isSelected = selected?.documentId === doc.documentId;
                    const isViewed = doc.isViewed === true;
                    const plain = stripMarkup(doc.aiSummary);
                    const hasOcr = !!doc.ocrText;
                    const hasSummary = !!doc.aiSummary;
                    const senderLabel = doc.senderDisplayName || doc.senderEmail || 'Unknown Sender';
                    const isCheck = !!doc.isCheck;

                    return (
                        <div
                            key={doc.documentId}
                            style={{
                                ...styles.item,
                                ...(isSelected ? styles.itemSelected : {}),
                            }}
                            onClick={() => onSelect(doc)}
                        >
                            <div style={{ ...styles.dot, background: statusColor(doc.statusId) }} />
                            <div style={styles.itemBody}>
                                <div style={styles.itemTop}>
                                    <span style={{ ...styles.sender, fontWeight: isViewed ? 500 : 700 }}>
                                        {senderLabel}
                                    </span>
                                    <span style={styles.date}>{formatDate(doc.uploadedAt)}</span>
                                </div>
                                <div style={styles.itemFilenameRow}>
                                    <span style={styles.filename}>{doc.originalFileName}</span>
                                    {isCheck && <span style={styles.checkBadge}>💰 Check</span>}
                                </div>
                                <div style={styles.preview}>
                                    {hasSummary
                                        ? plain.substring(0, 80) + (plain.length > 80 ? '…' : '')
                                        : hasOcr ? 'AI summary pending…' : 'Processing…'}
                                </div>
                            </div>

                            <div style={styles.rowMenu} onClick={e => e.stopPropagation()}>
                                <button
                                    style={styles.rowMenuBtn}
                                    onClick={e => {
                                        e.stopPropagation();
                                        setMenuDocId(id => id === doc.documentId ? null : doc.documentId);
                                    }}
                                >···</button>
                                {menuDocId === doc.documentId && (
                                    <div style={styles.rowDropdown} onMouseLeave={() => setMenuDocId(null)}>
                                        <button
                                            style={styles.rowDropdownItem}
                                            onClick={() => { onMarkUnread(doc.documentId); setMenuDocId(null); }}
                                        >
                                            ✉ Mark as unread
                                        </button>
                                    </div>
                                )}
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};

const styles = {
    panel: {
        width: 310, minWidth: 310,
        background: '#ffffff',
        borderRight: '1px solid #e2eaef',
        display: 'flex', flexDirection: 'column', height: '100vh',
        fontFamily: "'Plus Jakarta Sans', sans-serif",
    },
    header: {
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '18px 16px 10px', borderBottom: '1px solid #e2eaef',
    },
    title: { margin: 0, fontSize: 16, fontWeight: 700, color: '#1a2e3b' },
    count: {
        fontSize: 11, color: '#6b8499',
        background: '#f0f4f8', padding: '2px 9px', borderRadius: 20,
    },
    searchWrap: { padding: '10px 12px 8px' },
    searchRow: {
        position: 'relative',
        display: 'flex',
        alignItems: 'center',
    },
    search: {
        width: '100%', background: '#f0f4f8',
        border: '1px solid #e2eaef', color: '#1a2e3b',
        padding: '8px 32px 8px 12px', borderRadius: 8, fontSize: 12,
        outline: 'none', fontFamily: 'inherit',
        boxSizing: 'border-box',
    },
    clearBtn: {
        position: 'absolute',
        right: 8,
        background: 'transparent',
        border: 'none',
        cursor: 'pointer',
        fontSize: 11,
        color: '#6b8499',
        padding: '2px 4px',
        borderRadius: 4,
        lineHeight: 1,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
    },
    list: { flex: 1, overflowY: 'auto' },
    empty: { padding: '40px 20px', textAlign: 'center', color: '#6b8499', fontSize: 13 },
    item: {
        display: 'flex', alignItems: 'flex-start', gap: 10,
        padding: '12px 14px', borderBottom: '1px solid #e2eaef',
        cursor: 'pointer', transition: 'background 0.1s', background: '#fff',
    },
    itemSelected: { background: '#e6f7f5', borderLeft: '3px solid #0d9488' },
    dot: { width: 7, height: 7, borderRadius: '50%', marginTop: 6, flexShrink: 0 },
    itemBody: { flex: 1, minWidth: 0 },
    itemTop: { display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 6, marginBottom: 3 },
    sender: { fontSize: 12, color: '#1a2e3b', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 160 },
    date: { fontSize: 10, color: '#6b8499', flexShrink: 0 },
    itemFilenameRow: { display: 'flex', alignItems: 'center', gap: 6, marginBottom: 2 },
    filename: { fontSize: 11, color: '#6b8499', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
    checkBadge: { fontSize: 10, fontWeight: 700, background: '#fef3c7', color: '#92400e', padding: '1px 7px', borderRadius: 20, flexShrink: 0 },
    preview: { fontSize: 11, color: '#6b8499', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
    rowMenu: { position: 'relative', flexShrink: 0, display: 'flex', alignItems: 'center' },
    rowMenuBtn: {
        background: 'transparent', border: 'none', cursor: 'pointer',
        fontSize: 14, color: '#6b8499', padding: '2px 4px',
        borderRadius: 4, letterSpacing: 2, lineHeight: 1,
    },
    rowDropdown: {
        position: 'absolute', right: 0, top: '100%',
        background: '#fff', border: '1px solid #e2eaef',
        borderRadius: 8, boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
        zIndex: 200, minWidth: 150,
    },
    rowDropdownItem: {
        display: 'block', width: '100%', padding: '8px 14px',
        background: 'transparent', border: 'none', cursor: 'pointer',
        fontSize: 13, color: '#1a2e3b', textAlign: 'left', whiteSpace: 'nowrap',
    },
};

export default MailList;
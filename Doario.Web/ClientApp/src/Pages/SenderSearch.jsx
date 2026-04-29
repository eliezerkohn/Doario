// SenderSearch.jsx — search documents by sender, with dropdown of all known senders

import React, { useState, useEffect } from 'react';
import axios from 'axios';

const formatDate = (dateStr) => {
    const d = new Date(dateStr + 'Z');
    const now = new Date();
    const isToday = d.toDateString() === now.toDateString();
    if (isToday) return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return d.toLocaleDateString([], { month: 'short', day: 'numeric' });
};

const statusColor = (statusId) => ({
    1: '#c8a64c', 2: '#107c10', 4: '#5c5c5c',
    7: '#d13438', 8: '#8764b8',
}[statusId] ?? '#5c5c5c');

const stripMarkup = (html) => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
};

const TABS = ['All', 'Unassigned', 'Assigned', 'Actioned'];

const SenderSearch = ({ selected, onSelect }) => {
    const [input, setInput] = useState('');
    const [query, setQuery] = useState('');
    const [results, setResults] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [tab, setTab] = useState('All');
    const [showDrop, setShowDrop] = useState(false);
    const [senders, setSenders] = useState([]);

    // Load all known senders on mount — same pattern as staff list in MailSearch
    useEffect(() => {
        axios.get('/api/admin/senders')
            .then(r => setSenders(r.data))
            .catch(() => setSenders([]));
    }, []);

    const filteredSenders = senders.filter(s =>
        !input ||
        s.displayName?.toLowerCase().includes(input.toLowerCase()) ||
        s.email?.toLowerCase().includes(input.toLowerCase())
    );

    const doSearch = async (term) => {
        const trimmed = term.trim();
        if (!trimmed) return;
        setLoading(true);
        setError(null);
        setResults([]);
        setShowDrop(false);
        try {
            const r = await axios.get(`/api/admin/by-sender?q=${encodeURIComponent(trimmed)}`);
            setResults(r.data);
            setQuery(trimmed);
        } catch {
            setError('Search failed. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const handleSelect = (s) => {
        const term = s.displayName || s.email;
        setInput(term);
        setShowDrop(false);
        doSearch(term);
    };

    const handleKeyDown = (e) => {
        if (e.key === 'Enter') doSearch(input);
        if (e.key === 'Escape') setShowDrop(false);
    };

    const handleClear = () => {
        setInput('');
        setQuery('');
        setResults([]);
        setError(null);
        setTab('All');
        setShowDrop(false);
    };

    const tabStatusMap = {
        'Unassigned': [1],
        'Assigned': [2],
        'Actioned': [4],
    };

    const filtered = tab === 'All'
        ? results
        : results.filter(d => (tabStatusMap[tab] ?? []).includes(d.statusId));

    return (
        <div style={styles.panel}>

            <div style={styles.header}>
                <h2 style={styles.title}>Search by Sender</h2>
            </div>

            <div style={styles.searchSection}>
                <div style={styles.inputWrap}>
                    <div style={styles.inputRow}>
                        <input
                            style={styles.input}
                            placeholder="Sender name or email…"
                            value={input}
                            onChange={e => { setInput(e.target.value); setShowDrop(true); }}
                            onFocus={() => setShowDrop(true)}
                            onKeyDown={handleKeyDown}
                        />
                        {input && (
                            <button style={styles.clearBtn} onClick={handleClear} title="Clear">
                                ✕
                            </button>
                        )}
                    </div>
                    <button style={styles.searchBtn} onClick={() => doSearch(input)}>
                        Search
                    </button>
                </div>

                {showDrop && filteredSenders.length > 0 && (
                    <div style={styles.dropdown}>
                        {filteredSenders.map((s, i) => {
                            const label = s.displayName || s.email;
                            const sub = s.displayName ? s.email : null;
                            return (
                                <div
                                    key={i}
                                    style={styles.dropItem}
                                    onMouseDown={() => handleSelect(s)}
                                >
                                    <div style={styles.dropName}>{label}</div>
                                    {sub && <div style={styles.dropEmail}>{sub}</div>}
                                    <div style={styles.dropCount}>
                                        {s.documentCount} doc{s.documentCount !== 1 ? 's' : ''}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                )}
            </div>

            {query && (
                <>
                    <div style={styles.tabs}>
                        {TABS.map(t => (
                            <button
                                key={t}
                                style={{ ...styles.tab, ...(tab === t ? styles.tabActive : {}) }}
                                onClick={() => setTab(t)}
                            >
                                {t}
                            </button>
                        ))}
                        <span style={styles.resultCount}>
                            {filtered.length} result{filtered.length !== 1 ? 's' : ''}
                        </span>
                    </div>

                    <div style={styles.list}>
                        {loading && <div style={styles.empty}>Searching…</div>}
                        {error && <div style={styles.empty}>{error}</div>}
                        {!loading && !error && filtered.length === 0 && (
                            <div style={styles.empty}>
                                No {tab !== 'All' ? tab.toLowerCase() + ' ' : ''}documents found for "{query}"
                            </div>
                        )}
                        {filtered.map(doc => {
                            const isSelected = selected?.documentId === doc.documentId;
                            const plain = stripMarkup(doc.aiSummary);
                            return (
                                <div
                                    key={doc.documentId}
                                    style={{ ...styles.item, ...(isSelected ? styles.itemSelected : {}) }}
                                    onClick={() => onSelect(doc)}
                                >
                                    <div style={{ ...styles.dot, background: statusColor(doc.statusId) }} />
                                    <div style={styles.itemBody}>
                                        <div style={styles.itemTop}>
                                            <span style={styles.sender}>
                                                {doc.senderDisplayName || 'Unknown Sender'}
                                            </span>
                                            <span style={styles.date}>{formatDate(doc.uploadedAt)}</span>
                                        </div>
                                        {doc.senderEmail && (
                                            <div style={styles.senderEmail}>{doc.senderEmail}</div>
                                        )}
                                        <div style={styles.filename}>{doc.originalFileName}</div>
                                        <div style={styles.preview}>
                                            {plain ? plain.substring(0, 80) + (plain.length > 80 ? '…' : '') : 'Processing…'}
                                        </div>
                                        <div style={styles.statusRow}>
                                            <span style={{ ...styles.statusPill, color: statusColor(doc.statusId) }}>
                                                {doc.statusName}
                                            </span>
                                            <span style={styles.receivedDate}>
                                                Received {formatDate(doc.uploadedAt)}
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </>
            )}

            {!query && !loading && (
                <div style={styles.emptyState}>
                    <div style={styles.emptyIcon}>✉️</div>
                    <div style={styles.emptyTitle}>Search by sender</div>
                    <div style={styles.emptySub}>
                        Select a sender from the dropdown or type a name to find all documents from them
                    </div>
                </div>
            )}
        </div>
    );
};

const styles = {
    panel: {
        width: 320, minWidth: 320, background: '#faf9f8',
        borderRight: '1px solid #edebe9',
        display: 'flex', flexDirection: 'column', height: '100vh',
    },
    header: { padding: '16px 16px 8px', borderBottom: '1px solid #edebe9' },
    title: { margin: 0, fontSize: 18, fontWeight: 700, color: '#323130' },
    searchSection: { padding: '12px', borderBottom: '1px solid #edebe9', position: 'relative' },
    inputWrap: { display: 'flex', gap: 6 },
    inputRow: { position: 'relative', flex: 1, display: 'flex', alignItems: 'center' },
    input: {
        flex: 1, background: '#fff', border: '1px solid #edebe9',
        color: '#323130', padding: '7px 28px 7px 10px',
        borderRadius: 4, fontSize: 13, outline: 'none', fontFamily: 'inherit',
    },
    clearBtn: {
        position: 'absolute', right: 6,
        background: 'transparent', border: 'none',
        cursor: 'pointer', fontSize: 11, color: '#a19f9d',
        padding: '2px 4px', borderRadius: 4, lineHeight: 1,
        display: 'flex', alignItems: 'center', justifyContent: 'center',
    },
    searchBtn: {
        background: '#0f6cbd', color: '#fff', border: 'none',
        padding: '7px 14px', borderRadius: 4, fontSize: 12,
        fontWeight: 600, cursor: 'pointer',
    },
    dropdown: {
        position: 'absolute', left: 12, right: 12, top: '100%',
        background: '#fff', border: '1px solid #edebe9',
        borderRadius: 4, boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
        zIndex: 100, maxHeight: 260, overflowY: 'auto',
    },
    dropItem: {
        padding: '8px 12px', cursor: 'pointer',
        borderBottom: '1px solid #f3f2f1',
    },
    dropName: { fontSize: 12, fontWeight: 600, color: '#323130' },
    dropEmail: { fontSize: 11, color: '#a19f9d', marginTop: 1 },
    dropCount: { fontSize: 10, color: '#0f6cbd', marginTop: 2 },
    tabs: {
        display: 'flex', alignItems: 'center', gap: 4,
        padding: '8px 12px', borderBottom: '1px solid #edebe9',
    },
    tab: {
        padding: '4px 10px', border: '1px solid #edebe9',
        borderRadius: 20, fontSize: 11, fontWeight: 600,
        cursor: 'pointer', background: '#fff', color: '#6b7280',
    },
    tabActive: { background: '#0f6cbd', color: '#fff', borderColor: '#0f6cbd' },
    resultCount: { fontSize: 11, color: '#a19f9d', marginLeft: 'auto' },
    list: { flex: 1, overflowY: 'auto' },
    empty: { padding: '30px 20px', textAlign: 'center', color: '#a19f9d', fontSize: 13 },
    item: {
        display: 'flex', alignItems: 'flex-start', gap: 10,
        padding: '10px 14px', borderBottom: '1px solid #edebe9',
        cursor: 'pointer', background: '#faf9f8',
    },
    itemSelected: { background: '#dce6f7' },
    dot: { width: 8, height: 8, borderRadius: '50%', marginTop: 6, flexShrink: 0 },
    itemBody: { flex: 1, minWidth: 0 },
    itemTop: { display: 'flex', justifyContent: 'space-between', marginBottom: 2 },
    sender: { fontSize: 12, fontWeight: 600, color: '#323130', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 160 },
    date: { fontSize: 11, color: '#a19f9d', flexShrink: 0 },
    senderEmail: { fontSize: 11, color: '#0f6cbd', marginBottom: 2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
    filename: { fontSize: 11, color: '#605e5c', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginBottom: 2 },
    preview: { fontSize: 11, color: '#a19f9d', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginBottom: 4 },
    statusRow: { display: 'flex', alignItems: 'center', gap: 8 },
    statusPill: { fontSize: 10, fontWeight: 700 },
    receivedDate: { fontSize: 10, color: '#a19f9d' },
    emptyState: {
        flex: 1, display: 'flex', flexDirection: 'column',
        alignItems: 'center', justifyContent: 'center',
        gap: 8, padding: '40px 20px',
    },
    emptyIcon: { fontSize: 36 },
    emptyTitle: { fontSize: 14, fontWeight: 600, color: '#605e5c' },
    emptySub: { fontSize: 12, color: '#a19f9d', textAlign: 'center' },
};

export default SenderSearch;
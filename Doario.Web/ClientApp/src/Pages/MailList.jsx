// MailList.jsx — Light A style

import React, { useState, useEffect } from 'react';
import axios from 'axios';

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

const SEARCH_TYPES = [
    { value: 'mail', label: 'Mail', icon: '📬', placeholder: 'Search mail…' },
    { value: 'staff', label: 'By Staff', icon: '👤', placeholder: 'Staff name or email…' },
    { value: 'sender', label: 'By Sender', icon: '✉️', placeholder: 'Sender name or email…' },
    { value: 'checks', label: 'Checks', icon: '💰', placeholder: 'Payer, check number, amount…' },
];

const MailList = ({ docs, selected, loading, loadingMore, hasMore, folder, onSelect, onMarkUnread, onLoadMore }) => {
    const [search, setSearch] = useState('');
    const [searchType, setSearchType] = useState('mail');
    const [menuDocId, setMenuDocId] = useState(null);
    const [showSearchDrop, setShowSearchDrop] = useState(false);
    const [apiResults, setApiResults] = useState(null);
    const [apiLoading, setApiLoading] = useState(false);
    const [staff, setStaff] = useState([]);
    const [senders, setSenders] = useState([]);
    const [showSuggestions, setShowSuggestions] = useState(false);

    useEffect(() => {
        axios.get('/api/admin/senders').then(r => setSenders(r.data)).catch(() => { });
    }, []);

    useEffect(() => {
        setSearch('');
        setApiResults(null);
        setSearchType('mail');
    }, [folder]);

    const currentType = SEARCH_TYPES.find(t => t.value === searchType);

    const handleTypeSelect = (type) => {
        setSearchType(type);
        setSearch('');
        setApiResults(null);
        setShowSearchDrop(false);
        setShowSuggestions(false);
    };

    const handleSearchChange = (val) => {
        setSearch(val);
        setShowSuggestions(val.length > 0 && searchType !== 'mail');
        if (searchType === 'mail') setApiResults(null);
    };

    const doSearch = async (term) => {
        if (!term.trim()) { setApiResults(null); return; }
        setShowSuggestions(false);
        setApiLoading(true);
        try {
            let results = [];
            if (searchType === 'staff') {
                const r = await axios.get(`/api/assignment/by-email?email=${encodeURIComponent(term)}`);
                results = r.data.map(a => ({
                    documentId: a.documentId,
                    uploadedAt: a.uploadedAt,
                    originalFileName: a.originalFileName,
                    sharePointUrl: a.sharePointUrl,
                    senderDisplayName: a.senderDisplayName,
                    senderEmail: a.senderEmail,
                    aiSummary: a.aiSummary,
                    statusId: a.statusId,
                    statusName: a.statusName,
                    isViewed: true,
                }));
            } else if (searchType === 'sender') {
                const r = await axios.get(`/api/admin/by-sender?q=${encodeURIComponent(term)}`);
                results = r.data;
            } else if (searchType === 'checks') {
                const r = await axios.get('/api/admin/checks');
                const q = term.toLowerCase();
                results = r.data.filter(c =>
                    c.checkPayerName?.toLowerCase().includes(q) ||
                    c.checkNumber?.toLowerCase().includes(q) ||
                    c.checkAmount?.toString().includes(q) ||
                    c.senderDisplayName?.toLowerCase().includes(q) ||
                    c.originalFileName?.toLowerCase().includes(q)
                );
            }
            setApiResults(results);
        } catch {
            setApiResults([]);
        } finally {
            setApiLoading(false);
        }
    };

    const handleKeyDown = (e) => {
        if (e.key === 'Enter') { setShowSuggestions(false); doSearch(search); }
        if (e.key === 'Escape') { setShowSuggestions(false); }
    };

    const handleClear = () => {
        setSearch('');
        setApiResults(null);
        setShowSuggestions(false);
    };

    const suggestions = searchType === 'staff'
        ? staff.filter(s =>
            search && (
                s.email.toLowerCase().includes(search.toLowerCase()) ||
                `${s.firstName} ${s.lastName}`.toLowerCase().includes(search.toLowerCase())
            )).slice(0, 5)
        : searchType === 'sender'
            ? senders.filter(s =>
                search && (
                    s.displayName?.toLowerCase().includes(search.toLowerCase()) ||
                    s.email?.toLowerCase().includes(search.toLowerCase())
                )).slice(0, 5)
            : [];

    const localFiltered = docs.filter(d => {
        if (!search) return true;
        const q = search.toLowerCase();
        return (
            d.originalFileName?.toLowerCase().includes(q) ||
            stripMarkup(d.aiSummarySnippet || d.aiSummary).toLowerCase().includes(q)
        );
    });

    const displayDocs = searchType === 'mail'
        ? localFiltered
        : apiResults !== null ? apiResults : docs;

    const isSearchMode = searchType !== 'mail' && apiResults !== null;
    const isSearchLoading = apiLoading;

    return (
        <div style={styles.panel}>
            <div style={styles.header}>
                <h2 style={styles.title}>{folder}</h2>
                <span style={styles.count}>{isSearchMode ? `${displayDocs.length} results` : docs.length}</span>
            </div>

            <div style={styles.searchWrap}>
                <div style={styles.searchRow}>
                    <div style={styles.typeIndicator}>{currentType.icon}</div>

                    <input
                        style={styles.search}
                        placeholder={currentType.placeholder}
                        value={search}
                        onChange={e => handleSearchChange(e.target.value)}
                        onKeyDown={handleKeyDown}
                        onFocus={() => search && searchType !== 'mail' && setShowSuggestions(true)}
                    />

                    {search && (
                        <button style={styles.clearBtn} onClick={handleClear} title="Clear">✕</button>
                    )}

                    <button
                        style={styles.searchTypeBtn}
                        onClick={() => setShowSearchDrop(d => !d)}
                        title="Change search type"
                    >▾</button>

                    {showSearchDrop && (
                        <div style={styles.searchTypeDrop} onMouseLeave={() => setShowSearchDrop(false)}>
                            <div style={styles.searchTypeLabel}>Search by</div>
                            {SEARCH_TYPES.map(t => (
                                <button
                                    key={t.value}
                                    style={{
                                        ...styles.searchTypeItem,
                                        ...(searchType === t.value ? styles.searchTypeItemActive : {}),
                                    }}
                                    onClick={() => handleTypeSelect(t.value)}
                                >
                                    {t.icon} {t.label}
                                </button>
                            ))}
                        </div>
                    )}

                    {showSuggestions && suggestions.length > 0 && (
                        <div style={styles.suggestions}>
                            {suggestions.map((s, i) => {
                                const label = searchType === 'staff'
                                    ? `${s.firstName} ${s.lastName}`
                                    : s.displayName || s.email;
                                const sub = s.email;
                                return (
                                    <div
                                        key={i}
                                        style={styles.suggestionItem}
                                        onMouseDown={() => {
                                            const term = searchType === 'staff' ? s.email : label;
                                            setSearch(term);
                                            setShowSuggestions(false);
                                            doSearch(term);
                                        }}
                                    >
                                        <div style={styles.suggestionLabel}>{label}</div>
                                        {sub && <div style={styles.suggestionSub}>{sub}</div>}
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </div>

                {searchType !== 'mail' && (
                    <button
                        style={styles.searchBtn}
                        onClick={() => doSearch(search)}
                        disabled={isSearchLoading}
                    >
                        {isSearchLoading ? '…' : 'Search'}
                    </button>
                )}
            </div>

            {isSearchMode && (
                <div style={styles.searchBanner}>
                    <span>{currentType.icon} Searching {currentType.label} for "{search}"</span>
                    <button style={styles.searchBannerClose} onClick={handleClear}>✕ Clear</button>
                </div>
            )}

            <div style={styles.list}>
                {(loading || isSearchLoading) && <div style={styles.empty}>Loading…</div>}
                {!loading && !isSearchLoading && displayDocs.length === 0 && (
                    <div style={styles.empty}>
                        {isSearchMode ? `No results for "${search}"` : `No documents in ${folder}`}
                    </div>
                )}
                {!loading && !isSearchLoading && displayDocs.map(doc => {
                    const isSelected = selected?.documentId === doc.documentId;
                    const isViewed = doc.isViewed === true;
                    const snippet = doc.aiSummarySnippet || stripMarkup(doc.aiSummary);
                    const hasSnippet = !!snippet;
                    const hasOcr = !!doc.ocrText;
                    const senderLabel = doc.senderDisplayName || doc.senderEmail || 'Unknown Sender';
                    const isCheck = !!doc.isCheck;
                    const conf = doc.aiConfidence ?? 0;
                    const hasConfidence = conf > 0;

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
                                    {hasConfidence && (
                                        <span style={{
                                            ...styles.confBadge,
                                            ...(conf >= 8 ? styles.confHigh : conf >= 5 ? styles.confMid : styles.confLow)
                                        }}>
                                            {conf}/10
                                        </span>
                                    )}
                                </div>
                                <div style={styles.preview}>
                                    {hasSnippet
                                        ? snippet.substring(0, 80) + (snippet.length > 80 ? '…' : '')
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

                {/* Load More */}
                {!loading && !isSearchLoading && !isSearchMode && hasMore && (
                    <div style={styles.loadMoreWrap}>
                        <button
                            style={styles.loadMoreBtn}
                            onClick={onLoadMore}
                            disabled={loadingMore}
                        >
                            {loadingMore ? 'Loading…' : 'Load More'}
                        </button>
                    </div>
                )}

                {!loading && !isSearchLoading && !isSearchMode && !hasMore && docs.length > 0 && (
                    <div style={styles.allLoaded}>All documents loaded</div>
                )}
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
    searchWrap: { padding: '10px 12px 8px', display: 'flex', flexDirection: 'column', gap: 6 },
    searchRow: { position: 'relative', display: 'flex', alignItems: 'center', gap: 4 },
    typeIndicator: { fontSize: 13, flexShrink: 0, padding: '0 2px' },
    search: {
        flex: 1, background: '#f0f4f8',
        border: '1px solid #e2eaef', color: '#1a2e3b',
        padding: '8px', borderRadius: 8, fontSize: 12,
        outline: 'none', fontFamily: 'inherit',
        boxSizing: 'border-box', minWidth: 0,
    },
    clearBtn: {
        background: 'transparent', border: 'none', cursor: 'pointer',
        fontSize: 11, color: '#6b8499', padding: '2px 4px',
        borderRadius: 4, lineHeight: 1, flexShrink: 0,
    },
    searchTypeBtn: {
        background: 'transparent', border: '1px solid #e2eaef',
        borderRadius: 6, cursor: 'pointer', fontSize: 11,
        color: '#6b8499', padding: '5px 7px',
        lineHeight: 1, flexShrink: 0,
    },
    searchTypeDrop: {
        position: 'absolute', right: 0, top: '110%',
        background: '#fff', border: '1px solid #e2eaef',
        borderRadius: 8, boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
        zIndex: 300, minWidth: 160, padding: '6px 0',
    },
    searchTypeLabel: {
        fontSize: 10, fontWeight: 700, color: '#7a9ab0',
        textTransform: 'uppercase', letterSpacing: 1,
        padding: '4px 14px 6px',
    },
    searchTypeItem: {
        display: 'block', width: '100%', padding: '8px 14px',
        background: 'transparent', border: 'none', cursor: 'pointer',
        fontSize: 12, color: '#1a2e3b', textAlign: 'left',
        fontFamily: 'inherit',
    },
    searchTypeItemActive: { background: '#f0f4f8', fontWeight: 700, color: '#0d9488' },
    suggestions: {
        position: 'absolute', left: 0, right: 32, top: '110%',
        background: '#fff', border: '1px solid #e2eaef',
        borderRadius: 8, boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
        zIndex: 300, maxHeight: 200, overflowY: 'auto',
    },
    suggestionItem: { padding: '8px 12px', cursor: 'pointer', borderBottom: '1px solid #f0f4f8' },
    suggestionLabel: { fontSize: 12, fontWeight: 600, color: '#1a2e3b' },
    suggestionSub: { fontSize: 11, color: '#6b8499', marginTop: 1 },
    searchBtn: {
        padding: '7px 14px', background: '#0d9488', color: '#fff',
        border: 'none', borderRadius: 8, fontSize: 12, fontWeight: 600,
        cursor: 'pointer', fontFamily: 'inherit',
    },
    searchBanner: {
        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
        padding: '6px 12px', background: '#e6f7f5',
        borderBottom: '1px solid #99e0d9', fontSize: 11, color: '#0d9488',
    },
    searchBannerClose: {
        background: 'transparent', border: 'none', cursor: 'pointer',
        fontSize: 11, color: '#0d9488', fontWeight: 600, padding: 0,
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
    confBadge: { fontSize: 10, fontWeight: 700, padding: '1px 7px', borderRadius: 20, flexShrink: 0 },
    confHigh: { background: '#d1fae5', color: '#065f46' },
    confMid: { background: '#fef3c7', color: '#92400e' },
    confLow: { background: '#fee2e2', color: '#991b1b' },
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
    loadMoreWrap: { padding: '16px', textAlign: 'center' },
    loadMoreBtn: {
        padding: '8px 24px', background: '#fff', border: '1px solid #d1dde6',
        borderRadius: 8, fontSize: 12, fontWeight: 600, color: '#4a6478',
        cursor: 'pointer', fontFamily: 'inherit',
    },
    allLoaded: { padding: '16px', textAlign: 'center', fontSize: 11, color: '#7a9ab0' },
};

export default MailList;
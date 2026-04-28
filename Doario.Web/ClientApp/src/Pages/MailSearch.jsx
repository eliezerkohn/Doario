// MailSearch.jsx — staff email search panel
// Replaces MailList when the Search folder is active.
// Admin selects a staff email from a dropdown, results appear in the
// reading pane exactly like normal mail.

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
    1: '#c8a64c',
    2: '#107c10',
    4: '#5c5c5c',
    7: '#d13438',
    8: '#8764b8',
}[statusId] ?? '#5c5c5c');

const stripMarkup = (html) => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
};

// Filter tabs
const TABS = ['All', 'Active', 'Completed'];

const MailSearch = ({ staff, selected, onSelect }) => {
    const [email, setEmail] = useState('');
    const [query, setQuery] = useState('');   // committed search
    const [results, setResults] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [tab, setTab] = useState('All');
    const [showDrop, setShowDrop] = useState(false);

    // Filter staff list by what admin typed
    const filteredStaff = staff.filter(s =>
        !email ||
        s.email.toLowerCase().includes(email.toLowerCase()) ||
        `${s.firstName} ${s.lastName}`.toLowerCase().includes(email.toLowerCase())
    );

    const doSearch = async (emailToSearch) => {
        if (!emailToSearch) return;
        setLoading(true);
        setError(null);
        setResults([]);
        try {
            const r = await axios.get(`/api/assignment/by-email?email=${encodeURIComponent(emailToSearch)}`);
            setResults(r.data);
            setQuery(emailToSearch);
        } catch {
            setError('Search failed. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const handleSelect = (s) => {
        setEmail(s.email);
        setShowDrop(false);
        doSearch(s.email);
    };

    const handleKeyDown = (e) => {
        if (e.key === 'Enter') {
            setShowDrop(false);
            doSearch(email);
        }
    };

    // Apply tab filter
    const filtered = results.filter(d => {
        if (tab === 'Active') return d.statusId === 2; // Assigned
        if (tab === 'Completed') return d.statusId === 4; // Actioned
        return true; // All
    });

    return (
        <div style={styles.panel}>

            {/* Header */}
            <div style={styles.header}>
                <h2 style={styles.title}>Search by Staff</h2>
            </div>

            {/* Email input with dropdown */}
            <div style={styles.searchSection}>
                <div style={styles.inputWrap}>
                    <input
                        style={styles.input}
                        placeholder="Type name or email…"
                        value={email}
                        onChange={e => { setEmail(e.target.value); setShowDrop(true); }}
                        onFocus={() => setShowDrop(true)}
                        onKeyDown={handleKeyDown}
                    />
                    <button
                        style={styles.searchBtn}
                        onClick={() => { setShowDrop(false); doSearch(email); }}
                    >Search</button>
                </div>

                {/* Staff dropdown */}
                {showDrop && filteredStaff.length > 0 && (
                    <div style={styles.dropdown}>
                        {filteredStaff.map(s => (
                            <div
                                key={s.importedStaffId}
                                style={styles.dropItem}
                                onMouseDown={() => handleSelect(s)}
                            >
                                <div style={styles.dropName}>{s.firstName} {s.lastName}</div>
                                <div style={styles.dropEmail}>{s.email}</div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* Results */}
            {query && (
                <>
                    {/* Tab filter */}
                    <div style={styles.tabs}>
                        {TABS.map(t => (
                            <button
                                key={t}
                                style={{ ...styles.tab, ...(tab === t ? styles.tabActive : {}) }}
                                onClick={() => setTab(t)}
                            >{t}</button>
                        ))}
                        <span style={styles.resultCount}>{filtered.length} result{filtered.length !== 1 ? 's' : ''}</span>
                    </div>

                    <div style={styles.list}>
                        {loading && <div style={styles.empty}>Searching…</div>}
                        {error && <div style={styles.empty}>{error}</div>}
                        {!loading && !error && filtered.length === 0 && (
                            <div style={styles.empty}>No {tab !== 'All' ? tab.toLowerCase() + ' ' : ''}documents found for {query}</div>
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
                                        <div style={styles.filename}>{doc.originalFileName}</div>
                                        <div style={styles.preview}>
                                            {plain ? plain.substring(0, 80) + (plain.length > 80 ? '…' : '') : 'Processing…'}
                                        </div>
                                        <div style={styles.statusRow}>
                                            <span style={{ ...styles.statusPill, color: statusColor(doc.statusId) }}>
                                                {doc.statusName}
                                            </span>
                                            <span style={styles.assignedDate}>
                                                Assigned {formatDate(doc.assignedAt)}
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </>
            )}

            {/* Empty state before search */}
            {!query && !loading && (
                <div style={styles.emptyState}>
                    <div style={styles.emptyIcon}>🔍</div>
                    <div style={styles.emptyTitle}>Search by staff email</div>
                    <div style={styles.emptySub}>Select a staff member to see all documents assigned to them</div>
                </div>
            )}
        </div>
    );
};

const styles = {
    panel: {
        width: 320, minWidth: 320, background: '#faf9f8',
        borderRight: '1px solid #edebe9', display: 'flex',
        flexDirection: 'column', height: '100vh',
    },
    header: { padding: '16px 16px 8px', borderBottom: '1px solid #edebe9' },
    title: { margin: 0, fontSize: 18, fontWeight: 700, color: '#323130' },

    searchSection: { padding: '12px', borderBottom: '1px solid #edebe9', position: 'relative' },
    inputWrap: { display: 'flex', gap: 6 },
    input: {
        flex: 1, background: '#fff', border: '1px solid #edebe9',
        color: '#323130', padding: '7px 10px', borderRadius: 4,
        fontSize: 13, outline: 'none', fontFamily: 'inherit',
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
        zIndex: 100, maxHeight: 220, overflowY: 'auto',
    },
    dropItem: {
        padding: '8px 12px', cursor: 'pointer', borderBottom: '1px solid #f3f2f1',
        transition: 'background 0.1s',
    },
    dropName: { fontSize: 12, fontWeight: 600, color: '#323130' },
    dropEmail: { fontSize: 11, color: '#a19f9d', marginTop: 1 },

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
    filename: { fontSize: 11, color: '#605e5c', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginBottom: 2 },
    preview: { fontSize: 11, color: '#a19f9d', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginBottom: 4 },
    statusRow: { display: 'flex', alignItems: 'center', gap: 8 },
    statusPill: { fontSize: 10, fontWeight: 700 },
    assignedDate: { fontSize: 10, color: '#a19f9d' },

    emptyState: {
        flex: 1, display: 'flex', flexDirection: 'column',
        alignItems: 'center', justifyContent: 'center',
        gap: 8, padding: '40px 20px',
    },
    emptyIcon: { fontSize: 36 },
    emptyTitle: { fontSize: 14, fontWeight: 600, color: '#605e5c' },
    emptySub: { fontSize: 12, color: '#a19f9d', textAlign: 'center' },
};

export default MailSearch;
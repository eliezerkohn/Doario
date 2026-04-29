// ChecksSearch.jsx — search checks by payer name or amount

import React, { useState, useEffect } from 'react';
import axios from 'axios';

const formatDate = (dateStr) => {
    const d = new Date(dateStr + 'Z');
    const now = new Date();
    const isToday = d.toDateString() === now.toDateString();
    if (isToday) return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return d.toLocaleDateString([], { month: 'short', day: 'numeric' });
};

const stripMarkup = (html) => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
};

const ChecksSearch = ({ selected, onSelect }) => {
    const [input, setInput] = useState('');
    const [results, setResults] = useState([]);
    const [allChecks, setAllChecks] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // Load all checks on mount
    useEffect(() => {
        setLoading(true);
        axios.get('/api/admin/checks')
            .then(r => { setAllChecks(r.data); setResults(r.data); })
            .catch(() => setError('Failed to load checks.'))
            .finally(() => setLoading(false));
    }, []);

    const handleSearch = (val) => {
        setInput(val);
        if (!val.trim()) {
            setResults(allChecks);
            return;
        }
        const q = val.toLowerCase();
        setResults(allChecks.filter(c =>
            c.checkPayerName?.toLowerCase().includes(q) ||
            c.checkNumber?.toLowerCase().includes(q) ||
            c.checkAmount?.toString().includes(q) ||
            c.senderDisplayName?.toLowerCase().includes(q) ||
            c.originalFileName?.toLowerCase().includes(q)
        ));
    };

    const handleClear = () => {
        setInput('');
        setResults(allChecks);
    };

    const totalAmount = results.reduce((sum, c) => sum + (parseFloat(c.checkAmount) || 0), 0);

    return (
        <div style={styles.panel}>

            <div style={styles.header}>
                <h2 style={styles.title}>Search Checks</h2>
            </div>

            <div style={styles.searchSection}>
                <div style={styles.inputRow}>
                    <input
                        style={styles.input}
                        placeholder="Payer name, check number, amount…"
                        value={input}
                        onChange={e => handleSearch(e.target.value)}
                    />
                    {input && (
                        <button style={styles.clearBtn} onClick={handleClear} title="Clear">✕</button>
                    )}
                </div>
            </div>

            {!loading && !error && results.length > 0 && (
                <div style={styles.summary}>
                    <span style={styles.summaryCount}>{results.length} check{results.length !== 1 ? 's' : ''}</span>
                    <span style={styles.summaryTotal}>
                        Total: <strong>${totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}</strong>
                    </span>
                </div>
            )}

            <div style={styles.list}>
                {loading && <div style={styles.empty}>Loading…</div>}
                {error && <div style={styles.empty}>{error}</div>}
                {!loading && !error && results.length === 0 && (
                    <div style={styles.empty}>No checks found</div>
                )}
                {results.map(doc => {
                    const isSelected = selected?.documentId === doc.documentId;
                    const plain = stripMarkup(doc.aiSummary);
                    return (
                        <div
                            key={doc.documentId}
                            style={{ ...styles.item, ...(isSelected ? styles.itemSelected : {}) }}
                            onClick={() => onSelect(doc)}
                        >
                            <div style={styles.checkIcon}>💰</div>
                            <div style={styles.itemBody}>
                                <div style={styles.itemTop}>
                                    <span style={styles.payer}>{doc.checkPayerName || 'Unknown Payer'}</span>
                                    <span style={styles.amount}>
                                        {doc.checkAmount
                                            ? '$' + parseFloat(doc.checkAmount).toLocaleString('en-US', { minimumFractionDigits: 2 })
                                            : '—'}
                                    </span>
                                </div>
                                {doc.checkNumber && (
                                    <div style={styles.checkNumber}>#{doc.checkNumber}</div>
                                )}
                                <div style={styles.filename}>{doc.originalFileName}</div>
                                <div style={styles.preview}>
                                    {plain ? plain.substring(0, 70) + (plain.length > 70 ? '…' : '') : 'Processing…'}
                                </div>
                                <div style={styles.date}>{formatDate(doc.uploadedAt)}</div>
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
        width: 320, minWidth: 320, background: '#faf9f8',
        borderRight: '1px solid #edebe9',
        display: 'flex', flexDirection: 'column', height: '100vh',
        fontFamily: "'Plus Jakarta Sans', sans-serif",
    },
    header: { padding: '16px 16px 8px', borderBottom: '1px solid #edebe9' },
    title: { margin: 0, fontSize: 18, fontWeight: 700, color: '#323130' },
    searchSection: { padding: '10px 12px', borderBottom: '1px solid #edebe9' },
    inputRow: { position: 'relative', display: 'flex', alignItems: 'center' },
    input: {
        width: '100%', background: '#fff', border: '1px solid #fde68a',
        color: '#1a2e3b', padding: '8px 32px 8px 12px',
        borderRadius: 8, fontSize: 12, outline: 'none',
        fontFamily: 'inherit', boxSizing: 'border-box',
    },
    clearBtn: {
        position: 'absolute', right: 8,
        background: 'transparent', border: 'none',
        cursor: 'pointer', fontSize: 11, color: '#a19f9d',
        padding: '2px 4px', borderRadius: 4, lineHeight: 1,
    },
    summary: {
        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
        padding: '8px 14px', background: '#f3f2f1',
        borderBottom: '1px solid #edebe9', fontSize: 12,
    },
    summaryCount: { color: '#323130', fontWeight: 600 },
    summaryTotal: { color: '#605e5c' },
    list: { flex: 1, overflowY: 'auto' },
    empty: { padding: '40px 20px', textAlign: 'center', color: '#a19f9d', fontSize: 13 },
    item: {
        display: 'flex', alignItems: 'flex-start', gap: 10,
        padding: '12px 14px', borderBottom: '1px solid #edebe9',
        cursor: 'pointer', background: '#faf9f8',
    },
    itemSelected: { background: '#dce6f7', borderLeft: '3px solid #0f6cbd' },
    checkIcon: { fontSize: 18, flexShrink: 0, marginTop: 2 },
    itemBody: { flex: 1, minWidth: 0 },
    itemTop: { display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 6, marginBottom: 2 },
    payer: { fontSize: 12, fontWeight: 700, color: '#1a2e3b', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 150 },
    amount: { fontSize: 13, fontWeight: 800, color: '#059669', flexShrink: 0 },
    checkNumber: { fontSize: 11, color: '#92400e', marginBottom: 2 },
    filename: { fontSize: 11, color: '#6b8499', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginBottom: 2 },
    preview: { fontSize: 11, color: '#6b8499', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginBottom: 2 },
    date: { fontSize: 10, color: '#a19f9d' },
};

export default ChecksSearch;
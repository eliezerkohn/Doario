// BatchScanPage.jsx — Scan → AI splits → review → confirm to save

import React, { useState, useEffect } from 'react';
import axios from 'axios';

const BRIDGE_URL = 'http://localhost:5100';

const STATUS = {
    CHECKING: 'checking',
    READY: 'ready',
    NOT_FOUND: 'not_found',
    SCANNING: 'scanning',
    SPLITTING: 'splitting',
    DONE: 'done',
    ERROR: 'error',
};

export default function BatchScanPage() {

    const [bridgeStatus, setBridgeStatus] = useState(STATUS.CHECKING);
    const [scanners, setScanners] = useState([]);
    const [selectedScanner, setSelectedScanner] = useState('');
    const [status, setStatus] = useState(STATUS.CHECKING);
    const [documents, setDocuments] = useState([]);
    const [selected, setSelected] = useState(null);
    const [error, setError] = useState(null);
    const [batchScanId, setBatchScanId] = useState(null);

    useEffect(() => { checkBridge(); }, []);

    const checkBridge = async () => {
        setStatus(STATUS.CHECKING);
        try {
            const healthRes = await axios.get(`${BRIDGE_URL}/health`, { timeout: 3000 });
            const scannersRes = await axios.get(`${BRIDGE_URL}/scanners`, { timeout: 3000 });
            const scannerList = scannersRes.data.scanners ?? [];
            setScanners(scannerList);

            const configured = healthRes.data.selectedScanner;
            if (configured && scannerList.includes(configured)) {
                setSelectedScanner(configured);
            } else if (scannerList.length > 0) {
                setSelectedScanner(scannerList[0]);
            }

            if (healthRes.data.isConfigured && scannerList.length > 0) {
                setBridgeStatus(STATUS.READY);
                setStatus(STATUS.READY);
            } else {
                setBridgeStatus(STATUS.NOT_FOUND);
                setStatus(STATUS.NOT_FOUND);
            }
        } catch {
            setBridgeStatus(STATUS.NOT_FOUND);
            setStatus(STATUS.NOT_FOUND);
        }
    };

    // ── Step 1: Scan all pages ────────────────────────────────────────────────
    const handleScan = async () => {
        setStatus(STATUS.SCANNING);
        setDocuments([]);
        setSelected(null);
        setError(null);
        setBatchScanId(null);

        try {
            const scanRes = await axios.post(
                `${BRIDGE_URL}/scan`,
                { scanner: selectedScanner },
                { timeout: 120000 }
            );

            if (!scanRes.data.success) {
                setError(scanRes.data.error || 'Scan failed.');
                setStatus(STATUS.ERROR);
                return;
            }

            const pages = scanRes.data.pages;
            if (!pages || pages.length === 0) {
                setError('No pages received from scanner.');
                setStatus(STATUS.ERROR);
                return;
            }

            // ── Step 2: Send to backend for AI splitting ──────────────────────
            // Backend returns boundaries only — nothing uploaded to SharePoint yet
            setStatus(STATUS.SPLITTING);

            const splitRes = await axios.post(`${BRIDGE_URL}/upload`, { pages });

            if (!splitRes.data.documents) {
                setError(`Split error: ${JSON.stringify(splitRes.data)}`);
                setStatus(STATUS.ERROR);
                return;
            }

            setBatchScanId(splitRes.data.batchScanId);

            const docs = splitRes.data.documents.map((d, i) => {
                const nonBlankPages = (d.pages ?? []).filter(p => p && p.length > 0);
                const pageCount = nonBlankPages.length;
                return {
                    tempId: d.tempId,
                    index: i,
                    label: `Document ${i + 1}`,
                    pageRange: pageCount === 1
                        ? `1 page`
                        : `${pageCount} pages`,
                    pageStart: d.pageStart,
                    pageEnd: d.pageEnd,
                    pages: nonBlankPages,
                    previewBase64: nonBlankPages[0] ?? null,
                    confirmed: false,
                    confirming: false,
                    documentId: null,
                    sharePointUrl: null,
                };
            });

            setDocuments(docs);
            setSelected(docs[0] ?? null);
            setStatus(STATUS.DONE);

        } catch (err) {
            setError(err.response?.data?.error || err.message || 'Scan failed.');
            setStatus(STATUS.ERROR);
        }
    };

    // ── Confirm one document — builds PDF, uploads to SharePoint, creates DB record ──
    const handleConfirm = async (doc) => {
        setDocuments(prev => prev.map(d =>
            d.tempId === doc.tempId ? { ...d, confirming: true } : d
        ));

        try {
            const res = await axios.post(`${BRIDGE_URL}/confirm`, {
                pages: doc.pages,
                batchScanId: batchScanId,
                documentIndex: doc.index,
                pageStart: doc.pageStart,
                pageEnd: doc.pageEnd,
            });

            setDocuments(prev => prev.map(d =>
                d.tempId === doc.tempId ? {
                    ...d,
                    confirmed: true,
                    confirming: false,
                    documentId: res.data.documentId,
                    sharePointUrl: res.data.sharePointUrl,
                } : d
            ));

            setSelected(prev =>
                prev?.tempId === doc.tempId ? {
                    ...prev,
                    confirmed: true,
                    confirming: false,
                    documentId: res.data.documentId,
                    sharePointUrl: res.data.sharePointUrl,
                } : prev
            );

        } catch (err) {
            setDocuments(prev => prev.map(d =>
                d.tempId === doc.tempId ? { ...d, confirming: false } : d
            ));
            setError(err.response?.data?.error || 'Confirm failed. Please try again.');
        }
    };

    // ── Confirm all unconfirmed documents ─────────────────────────────────────
    const handleConfirmAll = async () => {
        const unconfirmed = documents.filter(d => !d.confirmed && !d.confirming);
        for (const doc of unconfirmed) {
            await handleConfirm(doc);
        }
    };

    // ── Delete — removes from list, never uploaded ────────────────────────────
    const handleDelete = (doc) => {
        const remaining = documents.filter(d => d.tempId !== doc.tempId);
        setDocuments(remaining);
        if (selected?.tempId === doc.tempId) {
            setSelected(remaining[0] ?? null);
        }
    };

    // ── Rescan one document ───────────────────────────────────────────────────
    const handleRescan = async (doc) => {
        setError(null);
        try {
            const scanRes = await axios.post(
                `${BRIDGE_URL}/scan`,
                { scanner: selectedScanner },
                { timeout: 120000 }
            );

            if (!scanRes.data.success) {
                setError(scanRes.data.error || 'Rescan failed.');
                return;
            }

            const newPages = scanRes.data.pages;

            // If already confirmed, tell backend to delete old SharePoint file + DB record
            const rescanRes = await axios.post(`${BRIDGE_URL}/rescan`, {
                pages: newPages,
                oldDocumentId: doc.documentId ?? null,
                oldSharePointUrl: doc.sharePointUrl ?? null,
            });

            if (!rescanRes.data.pages) {
                setError(`Rescan error: ${JSON.stringify(rescanRes.data)}`);
                return;
            }

            const updated = {
                ...doc,
                pages: rescanRes.data.pages,
                previewBase64: rescanRes.data.previewBase64 ?? newPages[0],
                confirmed: false,
                confirming: false,
                documentId: null,
                sharePointUrl: null,
            };

            setDocuments(prev => prev.map(d =>
                d.tempId === doc.tempId ? updated : d
            ));
            setSelected(prev =>
                prev?.tempId === doc.tempId ? updated : prev
            );

        } catch (err) {
            setError(err.response?.data?.error || 'Rescan failed.');
        }
    };

    // ── Rescan all — start from scratch ──────────────────────────────────────
    const handleRescanAll = () => {
        setDocuments([]);
        setSelected(null);
        setBatchScanId(null);
        setError(null);
        setStatus(STATUS.READY);
    };

    const allConfirmed = documents.length > 0 && documents.every(d => d.confirmed);
    const anyConfirmed = documents.some(d => d.confirmed);
    const openSharePoint = (url) => window.open(url, '_blank');

    return (
        <div style={S.page}>

            {/* Header */}
            <div style={S.header}>
                <div style={S.headerLeft}>
                    <div style={S.title}>Scan Documents</div>
                    <div style={S.subtitle}>Scan directly from your mailroom scanner into Doario</div>
                </div>
                <div style={S.headerRight}>

                    {bridgeStatus === STATUS.READY && scanners.length > 0 && (
                        <div style={S.scannerPickerWrap}>
                            <select
                                style={S.scannerSelect}
                                value={selectedScanner}
                                onChange={e => setSelectedScanner(e.target.value)}
                                disabled={status === STATUS.SCANNING || status === STATUS.SPLITTING}
                            >
                                {scanners.map(s => (
                                    <option key={s} value={s}>{s}</option>
                                ))}
                            </select>
                            <button style={S.btnRefresh} onClick={checkBridge}
                                disabled={status === STATUS.SCANNING || status === STATUS.SPLITTING}
                                title="Refresh scanner list">⟳</button>
                        </div>
                    )}

                    {status === STATUS.DONE && documents.length > 0 && (
                        <button style={S.btnSecondary} onClick={handleRescanAll}>
                            Rescan All
                        </button>
                    )}

                    <button
                        style={{
                            ...S.btnPrimary,
                            opacity: (
                                status === STATUS.SCANNING ||
                                status === STATUS.SPLITTING ||
                                bridgeStatus !== STATUS.READY ||
                                !selectedScanner
                            ) ? 0.5 : 1,
                            cursor: (bridgeStatus !== STATUS.READY || !selectedScanner)
                                ? 'not-allowed' : 'pointer',
                        }}
                        onClick={handleScan}
                        disabled={
                            status === STATUS.SCANNING ||
                            status === STATUS.SPLITTING ||
                            bridgeStatus !== STATUS.READY ||
                            !selectedScanner
                        }
                    >
                        {status === STATUS.SCANNING ? 'Scanning...' :
                            status === STATUS.SPLITTING ? 'Analysing...' : 'Scan Now'}
                    </button>
                </div>
            </div>

            {/* Bridge not found */}
            {bridgeStatus === STATUS.NOT_FOUND && (
                <div style={S.bridgeWarning}>
                    <div style={S.bridgeWarningTitle}>DoarioScan Bridge not detected</div>
                    <div style={S.bridgeWarningText}>
                        The DoarioScan Bridge app must be running on this PC to scan.
                        Scanning is only available from the mailroom PC where DoarioScan is installed.
                        Download and install it from Settings → Integrations, then click Retry.
                    </div>
                    <button style={S.btnSecondary} onClick={checkBridge}>Retry</button>
                </div>
            )}

            {/* Checking */}
            {status === STATUS.CHECKING && bridgeStatus === STATUS.CHECKING && (
                <div style={S.centeredMsg}>Checking for DoarioScan Bridge...</div>
            )}

            {/* Progress */}
            {(status === STATUS.SCANNING || status === STATUS.SPLITTING) && (
                <div style={S.progressWrap}>
                    <div style={S.progressText}>
                        {status === STATUS.SCANNING
                            ? 'Scanning — please wait...'
                            : 'Analysing pages — detecting document boundaries...'}
                    </div>
                </div>
            )}

            {/* Error */}
            {status === STATUS.ERROR && error && (
                <div style={S.errorBox}>
                    <div style={S.errorTitle}>Error</div>
                    <div style={S.errorText}>{error}</div>
                    <button style={S.btnSecondary} onClick={() => { setStatus(STATUS.READY); setError(null); }}>
                        Try Again
                    </button>
                </div>
            )}

            {/* Inline error (during confirm/rescan) */}
            {status === STATUS.DONE && error && (
                <div style={S.inlineError}>
                    ⚠ {error}
                    <button style={S.inlineErrorClose} onClick={() => setError(null)}>✕</button>
                </div>
            )}

            {/* Results */}
            {status === STATUS.DONE && documents.length > 0 && (
                <div style={S.results}>

                    {/* Document list */}
                    <div style={S.docList}>
                        <div style={S.docListHeader}>
                            <div style={S.docListTitle}>
                                {documents.length} document{documents.length !== 1 ? 's' : ''} detected
                            </div>
                            <div style={{ display: 'flex', gap: 6 }}>
                                {allConfirmed
                                    ? <div style={S.allConfirmedBadge}>✓ All saved</div>
                                    : <>
                                        <button style={S.btnConfirmAll} onClick={handleConfirmAll}>
                                            Save All
                                        </button>
                                        <button style={S.btnRescanAll} onClick={handleRescanAll}>
                                            Rescan All
                                        </button>
                                    </>
                                }
                            </div>
                        </div>

                        {documents.map(doc => (
                            <div
                                key={doc.tempId}
                                style={{
                                    ...S.docItem,
                                    background: selected?.tempId === doc.tempId
                                        ? 'rgba(13,148,136,0.08)' : '#fff',
                                    borderLeft: selected?.tempId === doc.tempId
                                        ? '3px solid #0d9488' : '3px solid transparent',
                                }}
                                onClick={() => setSelected(doc)}
                            >
                                <div style={S.docItemLeft}>
                                    <div style={S.docItemIcon}>📄</div>
                                    <div>
                                        <div style={S.docItemLabel}>{doc.label}</div>
                                        <div style={S.docItemPages}>{doc.pageRange}</div>
                                    </div>
                                </div>
                                <div style={S.docItemRight}>
                                    {doc.confirming
                                        ? <span style={S.savingTag}>Saving…</span>
                                        : doc.confirmed
                                            ? <span style={S.confirmedTag}>✓ Saved</span>
                                            : <span style={S.pendingTag}>Review</span>
                                    }
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* Preview pane */}
                    {selected && (
                        <div style={S.preview}>
                            <div style={S.previewHeader}>
                                <div style={S.previewTitle}>
                                    {selected.label}
                                    <span style={S.previewPageCount}>
                                        {' '}— {selected.pages?.length ?? 1} page{(selected.pages?.length ?? 1) !== 1 ? 's' : ''}
                                    </span>
                                </div>
                                <div style={S.previewActions}>
                                    {/* Delete — only if not yet confirmed */}
                                    {!selected.confirmed && (
                                        <button
                                            style={S.btnDelete}
                                            onClick={() => handleDelete(selected)}
                                            title="Remove this document — it will not be saved"
                                        >
                                            🗑 Delete
                                        </button>
                                    )}

                                    {/* Rescan — always available */}
                                    <button
                                        style={S.btnRescan}
                                        onClick={() => handleRescan(selected)}
                                    >
                                        ↺ Rescan
                                    </button>

                                    {/* Confirm — only if not yet confirmed */}
                                    {!selected.confirmed && (
                                        <button
                                            style={S.btnConfirm}
                                            disabled={selected.confirming}
                                            onClick={() => handleConfirm(selected)}
                                        >
                                            {selected.confirming ? 'Saving…' : '✓ Confirm & Save'}
                                        </button>
                                    )}

                                    {/* View in SharePoint — only after confirmed */}
                                    {selected.confirmed && selected.sharePointUrl && (
                                        <button
                                            style={S.spBtn}
                                            onClick={() => openSharePoint(selected.sharePointUrl)}
                                        >
                                            View in SharePoint ↗
                                        </button>
                                    )}
                                </div>
                            </div>

                            {/* All pages stacked vertically */}
                            <div style={S.previewImageWrap}>
                                {(selected.pages ?? []).map((p, i) => (
                                    p ? (
                                        <div key={i} style={S.previewPageWrap}>
                                            <div style={S.previewPageLabel}>Page {i + 1}</div>
                                            <img
                                                src={`data:image/png;base64,${p}`}
                                                alt={`Page ${i + 1}`}
                                                style={S.previewImage}
                                            />
                                        </div>
                                    ) : null
                                ))}
                            </div>

                            <div style={S.previewMeta}>
                                <div style={S.previewMetaItem}>
                                    <span style={S.metaLabel}>Scanner</span>
                                    <span style={S.metaValue}>{selectedScanner}</span>
                                </div>
                                <div style={S.previewMetaItem}>
                                    <span style={S.metaLabel}>Pages</span>
                                    <span style={S.metaValue}>{selected.pageRange}</span>
                                </div>
                                <div style={S.previewMetaItem}>
                                    <span style={S.metaLabel}>Status</span>
                                    <span style={S.metaValue}>
                                        {selected.confirmed
                                            ? '✓ Saved — OCR in progress'
                                            : 'Waiting for confirmation'}
                                    </span>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            )}

            {/* Empty state */}
            {status === STATUS.READY && documents.length === 0 && (
                <div style={S.emptyState}>
                    <div style={S.emptyIcon}>🖨️</div>
                    <div style={S.emptyTitle}>Ready to scan</div>
                    <div style={S.emptySub}>
                        Place your documents in the scanner feeder and click Scan Now.<br />
                        Doario will automatically detect document boundaries.<br />
                        You can review, rescan, or delete each document before saving.
                    </div>
                </div>
            )}

        </div>
    );
}

const S = {
    page: {
        fontFamily: "'Plus Jakarta Sans', sans-serif",
        background: '#f7f9fb', minHeight: '100vh', color: '#1a2e3b',
    },
    header: {
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '24px 32px', background: '#fff', borderBottom: '1px solid #e2eaef',
    },
    headerLeft: {},
    headerRight: { display: 'flex', alignItems: 'center', gap: 12 },
    title: { fontSize: 20, fontWeight: 800, color: '#0f2d4a' },
    subtitle: { fontSize: 12, color: '#7a9ab0', marginTop: 3 },
    scannerPickerWrap: { display: 'flex', alignItems: 'center', gap: 6 },
    scannerSelect: {
        padding: '7px 12px', border: '1px solid #d0dce6', borderRadius: 8,
        fontSize: 12, color: '#1a2e3b', background: '#fff',
        fontFamily: 'inherit', cursor: 'pointer', outline: 'none', maxWidth: 220,
    },
    btnRefresh: {
        padding: '7px 10px', background: 'transparent', border: '1px solid #d0dce6',
        borderRadius: 8, fontSize: 14, color: '#4a6478', cursor: 'pointer',
        fontFamily: 'inherit', lineHeight: 1,
    },
    btnPrimary: {
        padding: '10px 24px', background: '#0d9488', color: '#fff',
        border: 'none', borderRadius: 8, fontSize: 13, fontWeight: 700,
        cursor: 'pointer', fontFamily: 'inherit',
    },
    btnSecondary: {
        padding: '8px 18px', background: 'transparent', color: '#0d9488',
        border: '1px solid #0d9488', borderRadius: 8, fontSize: 13,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    bridgeWarning: {
        margin: '24px 32px', background: '#fff8e1',
        border: '1px solid #ffe082', borderRadius: 12, padding: '20px 24px',
    },
    bridgeWarningTitle: { fontSize: 14, fontWeight: 700, color: '#b45309', marginBottom: 6 },
    bridgeWarningText: { fontSize: 13, color: '#78550a', lineHeight: 1.6, marginBottom: 14 },
    centeredMsg: { textAlign: 'center', padding: '60px 32px', fontSize: 13, color: '#7a9ab0' },
    progressWrap: {
        display: 'flex', flexDirection: 'column', alignItems: 'center',
        justifyContent: 'center', padding: '80px 32px', gap: 16,
    },
    progressText: { fontSize: 14, color: '#4a6478', fontWeight: 500 },
    errorBox: {
        margin: '24px 32px', background: '#fff5f5',
        border: '1px solid #fca5a5', borderRadius: 12, padding: '20px 24px',
    },
    errorTitle: { fontSize: 14, fontWeight: 700, color: '#e53e3e', marginBottom: 6 },
    errorText: { fontSize: 13, color: '#c53030', lineHeight: 1.6, marginBottom: 14 },
    inlineError: {
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '10px 24px', background: '#fff5f5',
        borderBottom: '1px solid #fca5a5', fontSize: 12, color: '#c53030',
    },
    inlineErrorClose: {
        background: 'transparent', border: 'none', cursor: 'pointer',
        fontSize: 12, color: '#c53030',
    },
    results: { display: 'flex', height: 'calc(100vh - 89px)', overflow: 'hidden' },
    docList: {
        width: 280, minWidth: 280, background: '#fff',
        borderRight: '1px solid #e2eaef',
        display: 'flex', flexDirection: 'column', overflowY: 'auto',
    },
    docListHeader: {
        padding: '16px', borderBottom: '1px solid #e2eaef',
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
    },
    docListTitle: { fontSize: 13, fontWeight: 700, color: '#0f2d4a' },
    btnConfirmAll: {
        padding: '5px 10px', background: '#0d9488', color: '#fff',
        border: 'none', borderRadius: 6, fontSize: 11, fontWeight: 600,
        cursor: 'pointer', fontFamily: 'inherit',
    },
    btnRescanAll: {
        padding: '5px 10px', background: 'transparent', color: '#4a6478',
        border: '1px solid #d0dce6', borderRadius: 6, fontSize: 11,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    allConfirmedBadge: { fontSize: 11, color: '#0d9488', fontWeight: 600 },
    docItem: {
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '12px 16px', borderBottom: '1px solid #f0f4f7',
        cursor: 'pointer', transition: 'background 0.1s',
    },
    docItemLeft: { display: 'flex', alignItems: 'center', gap: 10 },
    docItemIcon: { fontSize: 16 },
    docItemLabel: { fontSize: 13, fontWeight: 600, color: '#1a2e3b' },
    docItemPages: { fontSize: 11, color: '#7a9ab0', marginTop: 2 },
    docItemRight: {},
    confirmedTag: { fontSize: 11, fontWeight: 600, color: '#0d9488' },
    savingTag: { fontSize: 11, fontWeight: 600, color: '#7a9ab0' },
    pendingTag: {
        fontSize: 10, fontWeight: 600, color: '#d97706',
        background: 'rgba(217,119,6,0.1)', padding: '2px 8px', borderRadius: 10,
    },
    preview: {
        flex: 1, display: 'flex', flexDirection: 'column',
        background: '#f7f9fb', overflow: 'hidden',
    },
    previewHeader: {
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '16px 24px', background: '#fff', borderBottom: '1px solid #e2eaef',
        flexShrink: 0,
    },
    previewTitle: { fontSize: 15, fontWeight: 700, color: '#0f2d4a' },
    previewPageCount: { fontSize: 12, fontWeight: 400, color: '#7a9ab0' },
    previewActions: { display: 'flex', alignItems: 'center', gap: 10 },
    btnDelete: {
        padding: '7px 14px', background: 'transparent', color: '#dc2626',
        border: '1px solid #dc2626', borderRadius: 7, fontSize: 12,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    btnRescan: {
        padding: '7px 14px', background: 'transparent', color: '#4a6478',
        border: '1px solid #d0dce6', borderRadius: 7, fontSize: 12,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    btnConfirm: {
        padding: '7px 18px', background: '#0d9488', color: '#fff',
        border: 'none', borderRadius: 7, fontSize: 12,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    spBtn: {
        padding: '7px 14px', background: 'transparent', color: '#0d9488',
        border: '1px solid #0d9488', borderRadius: 7, fontSize: 12,
        fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
    },
    previewImageWrap: {
        flex: 1, display: 'flex', flexDirection: 'column',
        alignItems: 'center', padding: '24px', gap: 24, overflowY: 'auto',
    },
    previewPageWrap: {
        display: 'flex', flexDirection: 'column',
        alignItems: 'center', width: '100%', maxWidth: 700,
    },
    previewPageLabel: {
        fontSize: 11, color: '#7a9ab0', fontWeight: 600,
        textTransform: 'uppercase', letterSpacing: '1px',
        marginBottom: 8, alignSelf: 'flex-start',
    },
    previewImage: {
        width: '100%', borderRadius: 8,
        boxShadow: '0 4px 20px rgba(0,0,0,0.1)', background: '#fff',
    },
    previewMeta: {
        padding: '16px 24px', background: '#fff',
        borderTop: '1px solid #e2eaef', display: 'flex', gap: 32, flexShrink: 0,
    },
    previewMetaItem: { display: 'flex', flexDirection: 'column', gap: 2 },
    metaLabel: {
        fontSize: 10, color: '#7a9ab0',
        textTransform: 'uppercase', letterSpacing: '1px',
    },
    metaValue: { fontSize: 12, color: '#1a2e3b', fontWeight: 500 },
    emptyState: {
        display: 'flex', flexDirection: 'column', alignItems: 'center',
        justifyContent: 'center', padding: '80px 32px', gap: 12, textAlign: 'center',
    },
    emptyIcon: { fontSize: 40 },
    emptyTitle: { fontSize: 18, fontWeight: 700, color: '#0f2d4a' },
    emptySub: { fontSize: 13, color: '#7a9ab0', lineHeight: 1.8 },
};
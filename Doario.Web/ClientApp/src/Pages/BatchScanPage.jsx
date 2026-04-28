// BatchScanPage.jsx — Scan directly from the Doario portal
// Talks to DoarioScan Bridge on localhost:5100

import React, { useState, useEffect } from 'react';
import axios from 'axios';

const BRIDGE_URL = 'http://localhost:5100';

const STATUS = {
    CHECKING: 'checking',
    READY: 'ready',
    NOT_FOUND: 'not_found',
    SCANNING: 'scanning',
    UPLOADING: 'uploading',
    DONE: 'done',
    ERROR: 'error',
};

export default function BatchScanPage() {

    const [bridgeStatus, setBridgeStatus] = useState(STATUS.CHECKING);
    const [scannerName, setScannerName] = useState('');
    const [status, setStatus] = useState(STATUS.CHECKING);
    const [documents, setDocuments] = useState([]);
    const [selected, setSelected] = useState(null);
    const [error, setError] = useState(null);

    useEffect(() => { checkBridge(); }, []);

    const checkBridge = async () => {
        setStatus(STATUS.CHECKING);
        try {
            const res = await axios.get(`${BRIDGE_URL}/health`, { timeout: 3000 });
            if (res.data.scannerReady) {
                setBridgeStatus(STATUS.READY);
                setScannerName(res.data.selectedScanner);
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

    const handleScan = async () => {
        setStatus(STATUS.SCANNING);
        setDocuments([]);
        setSelected(null);
        setError(null);

        try {
            const scanRes = await axios.post(`${BRIDGE_URL}/scan`, {}, { timeout: 120000 });

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

            setStatus(STATUS.UPLOADING);

            const ingestRes = await axios.post(`${BRIDGE_URL}/upload`, { pages });

            if (!ingestRes.data.documents) {
                setError(`Backend error: ${JSON.stringify(ingestRes.data)}`);
                setStatus(STATUS.ERROR);
                return;
            }

            const docs = ingestRes.data.documents.map((d, i) => ({
                ...d,
                label: `Document ${i + 1}`,
                pages: `Pages ${d.pageStart}-${d.pageEnd}`,
                confirmed: false,
            }));

            setDocuments(docs);
            setSelected(docs[0] ?? null);
            setStatus(STATUS.DONE);

        } catch (err) {
            setError(err.response?.data?.error || err.message || 'Scan failed.');
            setStatus(STATUS.ERROR);
        }
    };

    const handleRescan = async (doc) => {
        setError(null);
        try {
            const scanRes = await axios.post(`${BRIDGE_URL}/scan`, {}, { timeout: 120000 });

            if (!scanRes.data.success) {
                setError(scanRes.data.error || 'Rescan failed.');
                return;
            }

            const pages = scanRes.data.pages;
            const ingestRes = await axios.post(`${BRIDGE_URL}/upload`, { pages });

            setDocuments(prev => prev.map(d =>
                d.documentId === doc.documentId
                    ? {
                        ...d,
                        documentId: ingestRes.data.documentId,
                        sharePointUrl: ingestRes.data.sharePointUrl,
                        previewBase64: pages[0],
                        confirmed: false,
                    }
                    : d
            ));
            setSelected(prev =>
                prev?.documentId === doc.documentId
                    ? {
                        ...prev,
                        documentId: ingestRes.data.documentId,
                        sharePointUrl: ingestRes.data.sharePointUrl,
                        previewBase64: pages[0],
                        confirmed: false,
                    }
                    : prev
            );
        } catch (err) {
            setError(err.response?.data?.error || 'Rescan failed.');
        }
    };

    const handleConfirm = (documentId) => {
        setDocuments(prev => prev.map(d =>
            d.documentId === documentId ? { ...d, confirmed: true } : d
        ));
        // Sync selected so Confirm button disappears immediately
        setSelected(prev =>
            prev?.documentId === documentId ? { ...prev, confirmed: true } : prev
        );
    };

    const handleConfirmAll = () => {
        setDocuments(prev => prev.map(d => ({ ...d, confirmed: true })));
        setSelected(prev => prev ? { ...prev, confirmed: true } : prev);
    };

    const allConfirmed = documents.length > 0 && documents.every(d => d.confirmed);

    const openSharePoint = (url) => {
        window.open(url, '_blank');
    };

    return (
        <div style={S.page}>

            {/* Header */}
            <div style={S.header}>
                <div style={S.headerLeft}>
                    <div style={S.title}>Scan Documents</div>
                    <div style={S.subtitle}>
                        Scan directly from your mailroom scanner into Doario
                    </div>
                </div>
                <div style={S.headerRight}>
                    {bridgeStatus === STATUS.READY && (
                        <div style={S.scannerBadge}>{scannerName}</div>
                    )}
                    <button
                        style={{
                            ...S.btnPrimary,
                            opacity: (
                                status === STATUS.SCANNING ||
                                status === STATUS.UPLOADING ||
                                bridgeStatus !== STATUS.READY
                            ) ? 0.5 : 1,
                            cursor: bridgeStatus !== STATUS.READY ? 'not-allowed' : 'pointer',
                        }}
                        onClick={handleScan}
                        disabled={
                            status === STATUS.SCANNING ||
                            status === STATUS.UPLOADING ||
                            bridgeStatus !== STATUS.READY
                        }
                    >
                        {status === STATUS.SCANNING ? 'Scanning...' :
                            status === STATUS.UPLOADING ? 'Processing...' :
                                'Scan Now'}
                    </button>
                </div>
            </div>

            {/* Bridge not found */}
            {bridgeStatus === STATUS.NOT_FOUND && (
                <div style={S.bridgeWarning}>
                    <div style={S.bridgeWarningTitle}>DoarioScan Bridge not detected</div>
                    <div style={S.bridgeWarningText}>
                        The DoarioScan Bridge app must be running on this PC to scan.
                        Download and install it from Settings then Integrations, then click Retry.
                    </div>
                    <button style={S.btnSecondary} onClick={checkBridge}>Retry</button>
                </div>
            )}

            {/* Checking */}
            {status === STATUS.CHECKING && bridgeStatus === STATUS.CHECKING && (
                <div style={S.centeredMsg}>Checking for DoarioScan Bridge...</div>
            )}

            {/* Progress */}
            {(status === STATUS.SCANNING || status === STATUS.UPLOADING) && (
                <div style={S.progressWrap}>
                    <div style={S.progressText}>
                        {status === STATUS.SCANNING
                            ? 'Scanning — please wait...'
                            : 'Processing pages — uploading to SharePoint...'}
                    </div>
                </div>
            )}

            {/* Error */}
            {status === STATUS.ERROR && error && (
                <div style={S.errorBox}>
                    <div style={S.errorTitle}>Scan failed</div>
                    <div style={S.errorText}>{error}</div>
                    <button style={S.btnSecondary} onClick={() => setStatus(STATUS.READY)}>
                        Try Again
                    </button>
                </div>
            )}

            {/* Results */}
            {status === STATUS.DONE && documents.length > 0 && (
                <div style={S.results}>

                    {/* Left — document list */}
                    <div style={S.docList}>
                        <div style={S.docListHeader}>
                            <div style={S.docListTitle}>
                                {documents.length} document{documents.length !== 1 ? 's' : ''} detected
                            </div>
                            {allConfirmed
                                ? <div style={S.allConfirmedBadge}>All confirmed</div>
                                : (
                                    <button style={S.btnConfirmAll} onClick={handleConfirmAll}>
                                        Confirm All
                                    </button>
                                )
                            }
                        </div>

                        {documents.map(doc => (
                            <div
                                key={doc.documentId}
                                style={{
                                    ...S.docItem,
                                    background: selected?.documentId === doc.documentId
                                        ? 'rgba(13,148,136,0.08)' : '#fff',
                                    borderLeft: selected?.documentId === doc.documentId
                                        ? '3px solid #0d9488' : '3px solid transparent',
                                }}
                                onClick={() => setSelected(doc)}
                            >
                                <div style={S.docItemLeft}>
                                    <div style={S.docItemIcon}>doc</div>
                                    <div>
                                        <div style={S.docItemLabel}>{doc.label}</div>
                                        <div style={S.docItemPages}>{doc.pages}</div>
                                    </div>
                                </div>
                                <div style={S.docItemRight}>
                                    {doc.confirmed
                                        ? <span style={S.confirmedTag}>done</span>
                                        : <span style={S.pendingTag}>Pending</span>
                                    }
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* Right — preview */}
                    {selected && (
                        <div style={S.preview}>
                            <div style={S.previewHeader}>
                                <div style={S.previewTitle}>{selected.label}</div>
                                <div style={S.previewActions}>
                                    <button
                                        style={S.btnRescan}
                                        onClick={() => handleRescan(selected)}
                                    >
                                        Rescan
                                    </button>
                                    {!selected.confirmed && (
                                        <button
                                            style={S.btnConfirm}
                                            onClick={() => handleConfirm(selected.documentId)}
                                        >
                                            Confirm
                                        </button>
                                    )}
                                    {selected.sharePointUrl && (
                                        <button
                                            style={S.spBtn}
                                            onClick={() => openSharePoint(selected.sharePointUrl)}
                                        >
                                            View in SharePoint
                                        </button>
                                    )}
                                </div>
                            </div>

                            {selected.previewBase64 ? (
                                <div style={S.previewImageWrap}>
                                    <img
                                        src={`data:image/png;base64,${selected.previewBase64}`}
                                        alt="Document preview"
                                        style={S.previewImage}
                                    />
                                </div>
                            ) : (
                                <div style={S.previewPlaceholder}>
                                    <div style={{ fontSize: 13, color: '#7a9ab0', marginTop: 8 }}>
                                        Preview not available
                                    </div>
                                </div>
                            )}

                            <div style={S.previewMeta}>
                                <div style={S.previewMetaItem}>
                                    <span style={S.metaLabel}>Pages</span>
                                    <span style={S.metaValue}>{selected.pages}</span>
                                </div>
                                <div style={S.previewMetaItem}>
                                    <span style={S.metaLabel}>Status</span>
                                    <span style={S.metaValue}>
                                        OCR + AI summary in progress...
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
                    <div style={S.emptyTitle}>Ready to scan</div>
                    <div style={S.emptySub}>
                        Place your documents in the scanner feeder.
                        Use a blank page between each document for automatic splitting.
                    </div>
                </div>
            )}

        </div>
    );
}

const S = {
    page: {
        fontFamily: "'Plus Jakarta Sans', sans-serif",
        background: '#f7f9fb',
        minHeight: '100vh',
        color: '#1a2e3b',
    },
    header: {
        display: 'flex', alignItems: 'center',
        justifyContent: 'space-between',
        padding: '24px 32px',
        background: '#fff',
        borderBottom: '1px solid #e2eaef',
    },
    headerLeft: {},
    headerRight: {
        display: 'flex', alignItems: 'center', gap: 12,
    },
    title: {
        fontSize: 20, fontWeight: 800, color: '#0f2d4a',
    },
    subtitle: {
        fontSize: 12, color: '#7a9ab0', marginTop: 3,
    },
    scannerBadge: {
        fontSize: 12, fontWeight: 600,
        background: 'rgba(13,148,136,0.08)',
        color: '#0d9488', padding: '6px 12px',
        borderRadius: 20,
    },
    btnPrimary: {
        padding: '10px 24px', background: '#0d9488',
        color: '#fff', border: 'none', borderRadius: 8,
        fontSize: 13, fontWeight: 700, cursor: 'pointer',
        fontFamily: 'inherit',
    },
    btnSecondary: {
        padding: '8px 18px', background: 'transparent',
        color: '#0d9488', border: '1px solid #0d9488',
        borderRadius: 8, fontSize: 13, fontWeight: 600,
        cursor: 'pointer', fontFamily: 'inherit',
    },
    bridgeWarning: {
        margin: '24px 32px',
        background: '#fff8e1', border: '1px solid #ffe082',
        borderRadius: 12, padding: '20px 24px',
    },
    bridgeWarningTitle: {
        fontSize: 14, fontWeight: 700, color: '#b45309', marginBottom: 6,
    },
    bridgeWarningText: {
        fontSize: 13, color: '#78550a', lineHeight: 1.6, marginBottom: 14,
    },
    centeredMsg: {
        textAlign: 'center', padding: '60px 32px',
        fontSize: 13, color: '#7a9ab0',
    },
    progressWrap: {
        display: 'flex', flexDirection: 'column',
        alignItems: 'center', justifyContent: 'center',
        padding: '80px 32px', gap: 16,
    },
    progressText: {
        fontSize: 14, color: '#4a6478', fontWeight: 500,
    },
    errorBox: {
        margin: '24px 32px',
        background: '#fff5f5', border: '1px solid #fca5a5',
        borderRadius: 12, padding: '20px 24px',
    },
    errorTitle: {
        fontSize: 14, fontWeight: 700, color: '#e53e3e', marginBottom: 6,
    },
    errorText: {
        fontSize: 13, color: '#c53030', lineHeight: 1.6, marginBottom: 14,
    },
    results: {
        display: 'flex', height: 'calc(100vh - 89px)',
        overflow: 'hidden',
    },
    docList: {
        width: 280, minWidth: 280,
        background: '#fff',
        borderRight: '1px solid #e2eaef',
        display: 'flex', flexDirection: 'column',
        overflowY: 'auto',
    },
    docListHeader: {
        padding: '16px', borderBottom: '1px solid #e2eaef',
        display: 'flex', alignItems: 'center',
        justifyContent: 'space-between',
    },
    docListTitle: {
        fontSize: 13, fontWeight: 700, color: '#0f2d4a',
    },
    btnConfirmAll: {
        padding: '5px 12px', background: '#0d9488',
        color: '#fff', border: 'none', borderRadius: 6,
        fontSize: 11, fontWeight: 600, cursor: 'pointer',
        fontFamily: 'inherit',
    },
    allConfirmedBadge: {
        fontSize: 11, color: '#0d9488', fontWeight: 600,
    },
    docItem: {
        display: 'flex', alignItems: 'center',
        justifyContent: 'space-between',
        padding: '12px 16px',
        borderBottom: '1px solid #f0f4f7',
        cursor: 'pointer', transition: 'background 0.1s',
    },
    docItemLeft: {
        display: 'flex', alignItems: 'center', gap: 10,
    },
    docItemIcon: { fontSize: 11, color: '#7a9ab0' },
    docItemLabel: {
        fontSize: 13, fontWeight: 600, color: '#1a2e3b',
    },
    docItemPages: {
        fontSize: 11, color: '#7a9ab0', marginTop: 2,
    },
    docItemRight: {},
    confirmedTag: {
        fontSize: 11, fontWeight: 600, color: '#0d9488',
    },
    pendingTag: {
        fontSize: 10, fontWeight: 600,
        color: '#d97706', background: 'rgba(217,119,6,0.1)',
        padding: '2px 8px', borderRadius: 10,
    },
    preview: {
        flex: 1, display: 'flex', flexDirection: 'column',
        background: '#f7f9fb', overflowY: 'auto',
    },
    previewHeader: {
        display: 'flex', alignItems: 'center',
        justifyContent: 'space-between',
        padding: '16px 24px',
        background: '#fff', borderBottom: '1px solid #e2eaef',
    },
    previewTitle: {
        fontSize: 15, fontWeight: 700, color: '#0f2d4a',
    },
    previewActions: {
        display: 'flex', alignItems: 'center', gap: 10,
    },
    btnRescan: {
        padding: '7px 14px', background: 'transparent',
        color: '#4a6478', border: '1px solid #d0dce6',
        borderRadius: 7, fontSize: 12, fontWeight: 600,
        cursor: 'pointer', fontFamily: 'inherit',
    },
    btnConfirm: {
        padding: '7px 14px', background: '#0d9488',
        color: '#fff', border: 'none', borderRadius: 7,
        fontSize: 12, fontWeight: 600, cursor: 'pointer',
        fontFamily: 'inherit',
    },
    spBtn: {
        padding: '7px 14px', background: 'transparent',
        color: '#0d9488', border: '1px solid #0d9488',
        borderRadius: 7, fontSize: 12, fontWeight: 600,
        cursor: 'pointer', fontFamily: 'inherit',
    },
    previewImageWrap: {
        flex: 1, display: 'flex',
        alignItems: 'flex-start', justifyContent: 'center',
        padding: '24px', overflowY: 'auto',
    },
    previewImage: {
        maxWidth: '100%', maxHeight: '70vh',
        borderRadius: 8, boxShadow: '0 4px 20px rgba(0,0,0,0.1)',
        background: '#fff',
    },
    previewPlaceholder: {
        flex: 1, display: 'flex', flexDirection: 'column',
        alignItems: 'center', justifyContent: 'center',
        padding: '60px',
    },
    previewMeta: {
        padding: '16px 24px',
        background: '#fff', borderTop: '1px solid #e2eaef',
        display: 'flex', gap: 32,
    },
    previewMetaItem: {
        display: 'flex', flexDirection: 'column', gap: 2,
    },
    metaLabel: {
        fontSize: 10, color: '#7a9ab0',
        textTransform: 'uppercase', letterSpacing: '1px',
    },
    metaValue: {
        fontSize: 12, color: '#1a2e3b', fontWeight: 500,
    },
    emptyState: {
        display: 'flex', flexDirection: 'column',
        alignItems: 'center', justifyContent: 'center',
        padding: '80px 32px', gap: 12, textAlign: 'center',
    },
    emptyTitle: {
        fontSize: 18, fontWeight: 700, color: '#0f2d4a',
    },
    emptySub: {
        fontSize: 13, color: '#7a9ab0', lineHeight: 1.8,
    },
};
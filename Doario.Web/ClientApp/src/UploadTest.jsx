import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';

export default function UploadTest() {

    const navigate = useNavigate();

    const [file, setFile] = useState(null);
    const [preview, setPreview] = useState(null);
    const [result, setResult] = useState(null);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(false);

    const handleFileChange = (e) => {
        const f = e.target.files[0];
        if (!f) return;
        setFile(f);
        setPreview(f.type.startsWith('image/') ? URL.createObjectURL(f) : null);
        setResult(null);
        setError(null);
    };

    const handleCameraCapture = (e) => {
        const f = e.target.files[0];
        if (!f) return;
        setFile(f);
        setPreview(URL.createObjectURL(f));
        setResult(null);
        setError(null);
    };

    const handleUpload = async () => {
        if (!file) return;
        const form = new FormData();
        form.append('file', file);
        setLoading(true);
        setError(null);
        setResult(null);
        try {
            const res = await axios.post('/api/upload', form);
            setResult(res.data);
            setFile(null);
            setPreview(null);
            document.getElementById('file-picker').value = '';
            document.getElementById('camera-capture').value = '';
        } catch (err) {
            setError(err.response?.data || 'Upload failed');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={S.page}>
            <div style={S.inner}>

                {/* Page header */}
                <div style={S.pageHeader}>
                    <div style={S.pageTitle}>Upload Documents</div>
                    <div style={S.pageSub}>
                        Upload a file, take a photo, or scan directly from your scanner
                    </div>
                </div>

                <div style={S.grid}>

                    {/* ── Upload File card ── */}
                    <div style={S.card}>
                        <div style={S.cardTitle}>Upload File</div>
                        <div style={S.cardSub}>
                            Select a PDF, image, or take a photo with your camera
                        </div>

                        <input
                            id="file-picker"
                            type="file"
                            accept=".pdf,.jpg,.jpeg,.png,.tiff"
                            onChange={handleFileChange}
                            style={{ display: 'none' }}
                        />
                        <input
                            id="camera-capture"
                            type="file"
                            accept="image/*"
                            capture="environment"
                            onChange={handleCameraCapture}
                            style={{ display: 'none' }}
                        />

                        <div style={S.btnRow}>
                            <button
                                style={S.btnOutline}
                                onClick={() => document.getElementById('file-picker').click()}
                            >
                                📁 Choose File
                            </button>
                            <button
                                style={S.btnOutline}
                                onClick={() => document.getElementById('camera-capture').click()}
                            >
                                📷 Camera
                            </button>
                        </div>

                        {preview && (
                            <div style={S.previewWrap}>
                                <img src={preview} alt="Preview" style={S.previewImg} />
                            </div>
                        )}

                        {file && !preview && (
                            <div style={S.fileName}>📄 {file.name}</div>
                        )}

                        {file && (
                            <button
                                style={{
                                    ...S.btnPrimary,
                                    opacity: loading ? 0.6 : 1,
                                    marginTop: 16,
                                }}
                                onClick={handleUpload}
                                disabled={loading}
                            >
                                {loading ? 'Uploading...' : 'Upload to SharePoint'}
                            </button>
                        )}

                        {result && (
                            <div style={S.successBox}>
                                <div style={S.successTitle}>Upload successful</div>
                                <div style={S.successSub}>
                                    OCR and AI summary in progress
                                </div>
                                <button
                                    style={{ ...S.btnOutline, marginTop: 10 }}
                                    onClick={() => window.open(result.sharePointUrl, '_blank')}
                                >
                                    View in SharePoint
                                </button>
                            </div>
                        )}

                        {error && (
                            <div style={S.errorBox}>{String(error)}</div>
                        )}
                    </div>

                    {/* ── Scan from Scanner card ── */}
                    <div style={S.card}>
                        <div style={S.cardTitle}>Scan from Scanner</div>
                        <div style={S.cardSub}>
                            Scan directly from your connected scanner into Doario.
                            Preview each document, confirm, and batch scan with
                            automatic document splitting.
                        </div>
                        <button
                            style={{ ...S.btnPrimary, marginTop: 8 }}
                            onClick={() => navigate('/scan')}
                        >
                            🖨️ Go to Scan Page
                        </button>
                    </div>

                </div>
            </div>
        </div>
    );
}

// ── Styles ────────────────────────────────────────────────────────
const S = {
    page: {
        fontFamily: "'Plus Jakarta Sans', sans-serif",
        background: '#f7f9fb',
        minHeight: 'calc(100vh - 56px)',
        color: '#1a2e3b',
    },
    inner: {
        maxWidth: 900,
        margin: '0 auto',
        padding: '40px 32px',
    },
    pageHeader: {
        marginBottom: 32,
    },
    pageTitle: {
        fontSize: 22,
        fontWeight: 800,
        color: '#0f2d4a',
        marginBottom: 4,
    },
    pageSub: {
        fontSize: 13,
        color: '#7a9ab0',
    },
    grid: {
        display: 'grid',
        gridTemplateColumns: '1fr 1fr',
        gap: 24,
    },
    card: {
        background: '#fff',
        border: '1px solid #e2eaef',
        borderRadius: 12,
        padding: '24px 28px',
    },
    cardTitle: {
        fontSize: 15,
        fontWeight: 700,
        color: '#0f2d4a',
        marginBottom: 4,
    },
    cardSub: {
        fontSize: 12,
        color: '#7a9ab0',
        marginBottom: 20,
        lineHeight: 1.6,
    },
    btnRow: {
        display: 'flex',
        gap: 10,
        marginBottom: 16,
    },
    btnPrimary: {
        width: '100%',
        padding: '10px 0',
        background: '#0d9488',
        color: '#fff',
        border: 'none',
        borderRadius: 8,
        fontSize: 13,
        fontWeight: 700,
        cursor: 'pointer',
        fontFamily: 'inherit',
    },
    btnOutline: {
        padding: '8px 16px',
        background: 'transparent',
        color: '#0d9488',
        border: '1px solid #0d9488',
        borderRadius: 8,
        fontSize: 12,
        fontWeight: 600,
        cursor: 'pointer',
        fontFamily: 'inherit',
    },
    previewWrap: {
        marginBottom: 12,
        borderRadius: 8,
        overflow: 'hidden',
        border: '1px solid #e2eaef',
    },
    previewImg: {
        width: '100%',
        maxHeight: 200,
        objectFit: 'contain',
        display: 'block',
        background: '#f7f9fb',
    },
    fileName: {
        fontSize: 12,
        color: '#4a6478',
        marginBottom: 8,
        padding: '8px 12px',
        background: '#f7f9fb',
        borderRadius: 6,
        border: '1px solid #e2eaef',
    },
    successBox: {
        marginTop: 16,
        padding: '14px 16px',
        background: 'rgba(13,148,136,0.06)',
        border: '1px solid rgba(13,148,136,0.2)',
        borderRadius: 8,
    },
    successTitle: {
        fontSize: 13,
        fontWeight: 700,
        color: '#0d9488',
        marginBottom: 2,
    },
    successSub: {
        fontSize: 12,
        color: '#4a6478',
    },
    errorBox: {
        marginTop: 16,
        padding: '12px 16px',
        background: 'rgba(229,62,62,0.06)',
        border: '1px solid rgba(229,62,62,0.2)',
        borderRadius: 8,
        fontSize: 12,
        color: '#e53e3e',
    },
};
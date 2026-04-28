// IntegrationsSettings.jsx

import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { S, Shared } from './SettingsShared';

export default function IntegrationsSettings() {

    // ── State ────────────────────────────────────────────────────
    const [keyInfo, setKeyInfo] = useState({ hasKey: false, prefix: null });
    const [loading, setLoading] = useState(true);
    const [newKey, setNewKey] = useState(null);
    const [keyCopied, setKeyCopied] = useState(false);
    const [confirmRegenerate, setConfirmRegenerate] = useState(false);
    const [keyError, setKeyError] = useState(null);
    const [keyMsg, setKeyMsg] = useState(null);
    const [generating, setGenerating] = useState(false);

    // ── Load ─────────────────────────────────────────────────────
    useEffect(() => { loadKeyInfo(); }, []);

    const loadKeyInfo = async () => {
        setLoading(true);
        try {
            const res = await axios.get('/api/settings/api-key');
            setKeyInfo(res.data);
        } catch {
            setKeyError('Failed to load API key info.');
        } finally {
            setLoading(false);
        }
    };

    // ── Handlers ─────────────────────────────────────────────────
    const handleGenerate = async () => {
        setGenerating(true);
        setKeyError(null);
        setKeyMsg(null);
        setNewKey(null);
        try {
            const res = await axios.post('/api/settings/generate-api-key');
            setNewKey(res.data.apiKey);
            setKeyInfo({ hasKey: true, prefix: res.data.apiKey.substring(0, 16) + '...' });
        } catch {
            setKeyError('Failed to generate API key.');
        } finally {
            setGenerating(false);
        }
    };

    const handleRegenerate = async () => {
        if (!confirmRegenerate) {
            setConfirmRegenerate(true);
            return;
        }
        setConfirmRegenerate(false);
        setGenerating(true);
        setKeyError(null);
        setKeyMsg(null);
        setNewKey(null);
        try {
            const res = await axios.post('/api/settings/regenerate-api-key');
            setNewKey(res.data.apiKey);
            setKeyInfo({ hasKey: true, prefix: res.data.apiKey.substring(0, 16) + '...' });
            setKeyMsg('Previous key is now invalid. Copy your new key below.');
        } catch {
            setKeyError('Failed to regenerate API key.');
        } finally {
            setGenerating(false);
        }
    };

    const handleCopy = () => {
        if (!newKey) return;
        navigator.clipboard.writeText(newKey);
        setKeyCopied(true);
        setTimeout(() => setKeyCopied(false), 2000);
    };

    const handleCancelRegenerate = () => {
        setConfirmRegenerate(false);
    };

    // ── Render ───────────────────────────────────────────────────
    return (
        <div>
            <div style={S.sectionTitle}>Integrations</div>
            <div style={S.sectionSub}>Connect DoarioScan Bridge to your mailroom scanner</div>

            {/* ── DoarioScan Bridge ── */}
            <div style={S.card}>
                <div style={cardTitle}>DoarioScan Bridge</div>
                <div style={cardSub}>
                    Install the DoarioScan Bridge app on your mailroom PC.
                    It connects your scanner to Doario and lets you scan
                    directly from the portal — no files saved to your PC.
                </div>

                {/* How it works steps */}
                <div style={stepsWrap}>
                    {[
                        { n: '1', text: 'Download and install DoarioScan Bridge on your mailroom PC' },
                        { n: '2', text: 'Open the Bridge settings window from the system tray' },
                        { n: '3', text: 'Paste your API key and select your scanner' },
                        { n: '4', text: 'Scan directly from the Doario portal — done' },
                    ].map(step => (
                        <div key={step.n} style={stepRow}>
                            <div style={stepNum}>{step.n}</div>
                            <div style={stepText}>{step.text}</div>
                        </div>
                    ))}
                </div>

                {/* Download button — placeholder until Day 13 build */}
                <button style={{ ...S.btnPrimary, opacity: 0.5, cursor: 'not-allowed' }} disabled>
                    ⬇️ Download DoarioScan Bridge
                </button>
                <div style={{ fontSize: 11, color: '#7a9ab0', marginTop: 6 }}>
                    Installer available soon
                </div>
            </div>

            {/* ── API Key ── */}
            <div style={S.card}>
                <div style={cardTitle}>API Key</div>
                <div style={cardSub}>
                    This key authenticates the DoarioScan Bridge app.
                    Copy it once and paste it into the Bridge settings window on your mailroom PC.
                    It is never shown in full again after generation.
                </div>

                {loading ? <Shared.Loading /> : (
                    <>
                        {/* Active key display */}
                        {keyInfo.hasKey && !newKey && (
                            <div style={activeKeyWrap}>
                                <div style={activeKeyLabel}>Active key</div>
                                <div style={activeKeyPrefix}>{keyInfo.prefix}</div>
                            </div>
                        )}

                        {/* New key — shown once */}
                        {newKey && (
                            <div style={newKeyWrap}>
                                <div style={newKeyWarning}>
                                    ⚠️ Copy this key now — it will not be shown again
                                </div>
                                <div style={S.apiKeyBox}>{newKey}</div>
                                <button style={S.btnSecondary} onClick={handleCopy}>
                                    {keyCopied ? '✅ Copied!' : '📋 Copy to Clipboard'}
                                </button>
                            </div>
                        )}

                        {/* Warning message after regenerate */}
                        {keyMsg && (
                            <div style={{ ...S.msg, ...S.msgWarning, marginBottom: 14 }}>
                                ⚠️ {keyMsg}
                            </div>
                        )}

                        {/* Action buttons */}
                        <div style={{ ...S.row, marginTop: 4 }}>
                            {!keyInfo.hasKey && (
                                <button
                                    style={{ ...S.btnPrimary, opacity: generating ? 0.6 : 1 }}
                                    onClick={handleGenerate}
                                    disabled={generating}
                                >
                                    {generating ? 'Generating...' : 'Generate API Key'}
                                </button>
                            )}

                            {keyInfo.hasKey && !confirmRegenerate && (
                                <button
                                    style={{ ...S.btnDanger, opacity: generating ? 0.6 : 1 }}
                                    onClick={handleRegenerate}
                                    disabled={generating}
                                >
                                    🔄 Regenerate Key
                                </button>
                            )}

                            {keyInfo.hasKey && confirmRegenerate && (
                                <>
                                    <div style={confirmText}>
                                        This will invalidate your current key immediately.
                                        The Bridge app will stop working until you update it.
                                        Are you sure?
                                    </div>
                                    <div style={{ ...S.row, marginTop: 10 }}>
                                        <button
                                            style={{ ...S.btnDanger, opacity: generating ? 0.6 : 1 }}
                                            onClick={handleRegenerate}
                                            disabled={generating}
                                        >
                                            {generating ? 'Regenerating...' : 'Yes, Regenerate'}
                                        </button>
                                        <button
                                            style={S.btnSecondary}
                                            onClick={handleCancelRegenerate}
                                        >
                                            Cancel
                                        </button>
                                    </div>
                                </>
                            )}
                        </div>

                        {keyError && (
                            <div style={{ ...S.msg, ...S.msgError }}>❌ {keyError}</div>
                        )}
                    </>
                )}
            </div>
        </div>
    );
}

// ── Local styles ─────────────────────────────────────────────────
const cardTitle = {
    fontSize: 14, fontWeight: 700, color: '#0f2d4a', marginBottom: 4,
};
const cardSub = {
    fontSize: 12, color: '#7a9ab0', marginBottom: 16, lineHeight: 1.6,
};
const stepsWrap = {
    display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 20,
};
const stepRow = {
    display: 'flex', alignItems: 'flex-start', gap: 12,
};
const stepNum = {
    width: 24, height: 24, borderRadius: '50%',
    background: '#0d9488', color: '#fff',
    fontSize: 11, fontWeight: 700,
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    flexShrink: 0, marginTop: 1,
};
const stepText = {
    fontSize: 13, color: '#4a6478', lineHeight: 1.6,
};
const activeKeyWrap = {
    background: '#f7f9fb', border: '1px solid #e2eaef',
    borderRadius: 8, padding: '12px 16px', marginBottom: 16,
};
const activeKeyLabel = {
    fontSize: 10, color: '#7a9ab0',
    textTransform: 'uppercase', letterSpacing: '1.5px', marginBottom: 4,
};
const activeKeyPrefix = {
    fontFamily: 'monospace', fontSize: 14,
    fontWeight: 700, color: '#0f2d4a',
};
const newKeyWrap = {
    marginBottom: 16,
};
const newKeyWarning = {
    fontSize: 12, fontWeight: 600, color: '#b45309',
    background: 'rgba(180,83,9,0.08)', padding: '8px 12px',
    borderRadius: 8, marginBottom: 10,
};
const confirmText = {
    fontSize: 12, color: '#e53e3e',
    background: 'rgba(229,62,62,0.06)',
    padding: '10px 14px', borderRadius: 8,
    lineHeight: 1.6, width: '100%',
};
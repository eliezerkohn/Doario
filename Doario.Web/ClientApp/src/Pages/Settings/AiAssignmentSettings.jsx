// AiAssignmentSettings.jsx

import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { S, Shared } from './SettingsShared';

const MODES = [
    {
        value: 'AutoAssign',
        label: 'Auto Assign',
        icon: '⚡',
        desc: 'AI assigns documents directly to staff with no approval needed.',
    },
    {
        value: 'SuggestAndApprove',
        label: 'Suggest & Approve',
        icon: '👁',
        desc: 'AI suggests a staff member. You control whether high-confidence suggestions are assigned automatically.',
    },
    {
        value: 'Off',
        label: 'Off',
        icon: '🔕',
        desc: 'AI does not suggest or assign. All documents must be assigned manually.',
    },
];

export default function AiAssignmentSettings() {
    const [mode, setMode] = useState(null);
    const [threshold, setThreshold] = useState(8);
    const [autoAssignHighConf, setAutoAssignHighConf] = useState(true);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [msg, setMsg] = useState(null);
    const [error, setError] = useState(null);

    useEffect(() => {
        axios.get('/api/settings/ai-assignment')
            .then(r => {
                setMode(r.data.mode);
                const t = r.data.confidenceThreshold ?? 8;
                // threshold 0 means auto-assign is disabled
                setAutoAssignHighConf(t > 0);
                setThreshold(t > 0 ? t : 8);
            })
            .catch(() => setError('Failed to load AI assignment settings.'))
            .finally(() => setLoading(false));
    }, []);

    const save = async (newMode, newThreshold) => {
        setSaving(true);
        setMsg(null);
        setError(null);
        try {
            const res = await axios.put('/api/settings/ai-assignment', {
                mode: newMode,
                confidenceThreshold: newThreshold,
            });
            // Re-fetch confirmed value from DB
            const confirmed = await axios.get('/api/settings/ai-assignment');
            setMode(confirmed.data.mode);
            const t = confirmed.data.confidenceThreshold ?? 8;
            setAutoAssignHighConf(t > 0);
            setThreshold(t > 0 ? t : 8);
            setMsg('AI assignment settings updated.');
            setTimeout(() => setMsg(null), 3000);
        } catch (err) {
            console.error('Save failed:', err);
            setError('Failed to update settings.');
        } finally {
            setSaving(false);
        }
    };

    const handleModeClick = (newMode) => {
        if (saving) return;
        setMode(newMode);
        const effectiveThreshold = newMode === 'SuggestAndApprove'
            ? (autoAssignHighConf ? threshold : 0)
            : 8; // Off and AutoAssign don't use threshold — reset to default
        save(newMode, effectiveThreshold);
    };

    const handleToggleChange = (enabled) => {
        setAutoAssignHighConf(enabled);
        // 0 = never auto-assign, threshold value = auto-assign above that level
        save(mode, enabled ? threshold : 0);
    };

    const handleThresholdChange = (val) => {
        setThreshold(val);
    };

    const handleThresholdSave = () => {
        save(mode, autoAssignHighConf ? threshold : 0);
    };

    if (loading) return <Shared.Loading />;

    const confColor = threshold >= 8 ? '#065f46' : threshold >= 5 ? '#92400e' : '#991b1b';
    const confBg = threshold >= 8 ? '#d1fae5' : threshold >= 5 ? '#fef3c7' : '#fee2e2';

    return (
        <div>
            <div style={S.sectionTitle}>AI Assignment</div>
            <div style={S.sectionSub}>
                Control how the AI handles document assignment. The confidence score is always shown regardless of mode.
            </div>

            {msg && <div style={{ ...S.msg, ...S.msgSuccess }}>✅ {msg}</div>}
            {error && <div style={{ ...S.msg, ...S.msgError }}>❌ {error}</div>}

            {/* Mode cards */}
            <div style={styles.modeGrid}>
                {MODES.map(m => {
                    const isActive = mode === m.value;
                    return (
                        <div
                            key={m.value}
                            style={{
                                ...styles.modeCard,
                                ...(isActive ? styles.modeCardActive : {}),
                                opacity: saving ? 0.7 : 1,
                            }}
                            onClick={() => handleModeClick(m.value)}
                        >
                            <div style={styles.modeTop}>
                                <span style={styles.modeIcon}>{m.icon}</span>
                                <span style={styles.modeLabel}>{m.label}</span>
                                {isActive && <span style={styles.activeBadge}>Active</span>}
                            </div>
                            <div style={styles.modeDesc}>{m.desc}</div>
                        </div>
                    );
                })}
            </div>

            {/* Suggest & Approve options */}
            {mode === 'SuggestAndApprove' && (
                <div style={S.card}>
                    <div style={S.sectionTitle}>Auto-assign options</div>
                    <div style={S.sectionSub}>
                        Choose whether high-confidence suggestions should be assigned automatically.
                    </div>
                    <Shared.Divider />

                    {/* Toggle */}
                    <div style={styles.toggleRow}>
                        <div style={styles.toggleInfo}>
                            <div style={styles.toggleLabel}>Auto-assign at high confidence</div>
                            <div style={styles.toggleDesc}>
                                {autoAssignHighConf
                                    ? 'Documents meeting the confidence threshold are assigned automatically.'
                                    : 'All suggestions go to Pending Approvals — you approve every assignment.'}
                            </div>
                        </div>
                        <button
                            style={{
                                ...styles.toggle,
                                background: autoAssignHighConf ? '#0d9488' : '#d0dce6',
                            }}
                            onClick={() => handleToggleChange(!autoAssignHighConf)}
                            disabled={saving}
                        >
                            <span style={{
                                ...styles.toggleKnob,
                                transform: autoAssignHighConf ? 'translateX(20px)' : 'translateX(2px)',
                            }} />
                        </button>
                    </div>

                    {/* Threshold slider — only when toggle is on */}
                    {autoAssignHighConf && (
                        <>
                            <Shared.Divider />
                            <div style={S.sectionTitle}>Confidence threshold</div>
                            <div style={S.sectionSub}>
                                Documents at or above this level are assigned automatically. Below it they go to Pending Approvals.
                            </div>

                            <div style={styles.thresholdRow}>
                                <input
                                    type="range"
                                    min={1}
                                    max={10}
                                    value={threshold}
                                    style={styles.slider}
                                    onChange={e => handleThresholdChange(parseInt(e.target.value))}
                                    onMouseUp={handleThresholdSave}
                                    onTouchEnd={handleThresholdSave}
                                />
                                <span style={{ ...styles.thresholdBadge, background: confBg, color: confColor }}>
                                    {threshold}/10
                                </span>
                            </div>
                            <div style={styles.thresholdLabels}>
                                <span>1 — Auto-assign everything</span>
                                <span>10 — Only perfect confidence</span>
                            </div>
                            <div style={styles.thresholdExplain}>
                                At <strong>{threshold}/10</strong>:&nbsp;
                                {threshold <= 3
                                    ? 'Almost all documents will be auto-assigned.'
                                    : threshold <= 6
                                        ? 'Most documents will be auto-assigned. Lower confidence ones go to Pending Approvals.'
                                        : threshold <= 8
                                            ? 'Only high-confidence suggestions auto-assign. Others go to Pending Approvals.'
                                            : 'Only near-certain or perfect suggestions auto-assign. Most will go to Pending Approvals.'}
                            </div>
                        </>
                    )}
                </div>
            )}

            {/* Confidence colour guide */}
            <div style={S.card}>
                <div style={S.sectionTitle}>Confidence colour guide</div>
                <Shared.Divider />
                <div style={styles.confRow}>
                    <span style={{ ...styles.confBadge, background: '#d1fae5', color: '#065f46' }}>8-10</span>
                    <span style={styles.confText}>High — AI is very confident</span>
                </div>
                <div style={styles.confRow}>
                    <span style={{ ...styles.confBadge, background: '#fef3c7', color: '#92400e' }}>5-7</span>
                    <span style={styles.confText}>Medium — review recommended</span>
                </div>
                <div style={styles.confRow}>
                    <span style={{ ...styles.confBadge, background: '#fee2e2', color: '#991b1b' }}>1-4</span>
                    <span style={styles.confText}>Low — manual assignment advised</span>
                </div>
            </div>
        </div>
    );
}

const styles = {
    modeGrid: { display: 'flex', flexDirection: 'column', gap: 12, marginBottom: 24 },
    modeCard: {
        border: '2px solid #e2eaef', borderRadius: 12,
        padding: '16px 20px', cursor: 'pointer',
        background: '#fff', transition: 'all 0.15s',
    },
    modeCardActive: { border: '2px solid #0d9488', background: 'rgba(13,148,136,0.04)' },
    modeTop: { display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 },
    modeIcon: { fontSize: 18 },
    modeLabel: { fontSize: 14, fontWeight: 700, color: '#1a2e3b', flex: 1 },
    activeBadge: {
        fontSize: 10, fontWeight: 700, background: '#0d9488',
        color: '#fff', padding: '2px 8px', borderRadius: 20,
    },
    modeDesc: { fontSize: 12, color: '#4a6478', lineHeight: 1.6 },
    toggleRow: {
        display: 'flex', alignItems: 'center',
        justifyContent: 'space-between', gap: 20, padding: '4px 0',
    },
    toggleInfo: { flex: 1 },
    toggleLabel: { fontSize: 13, fontWeight: 600, color: '#1a2e3b', marginBottom: 3 },
    toggleDesc: { fontSize: 12, color: '#4a6478' },
    toggle: {
        width: 44, height: 24, borderRadius: 12, border: 'none',
        cursor: 'pointer', position: 'relative',
        flexShrink: 0, transition: 'background 0.2s', padding: 0,
    },
    toggleKnob: {
        position: 'absolute', top: 2,
        width: 20, height: 20, borderRadius: '50%',
        background: '#fff', display: 'block',
        transition: 'transform 0.2s',
        boxShadow: '0 1px 3px rgba(0,0,0,0.2)',
    },
    thresholdRow: { display: 'flex', alignItems: 'center', gap: 16, marginBottom: 8, marginTop: 12 },
    slider: { flex: 1, accentColor: '#0d9488', cursor: 'pointer' },
    thresholdBadge: {
        fontSize: 14, fontWeight: 800, padding: '4px 14px',
        borderRadius: 20, flexShrink: 0, minWidth: 56, textAlign: 'center',
    },
    thresholdLabels: {
        display: 'flex', justifyContent: 'space-between',
        fontSize: 10, color: '#7a9ab0', marginBottom: 12,
    },
    thresholdExplain: {
        fontSize: 12, color: '#4a6478', lineHeight: 1.6,
        background: '#f0f4f8', padding: '10px 14px', borderRadius: 8,
    },
    confRow: { display: 'flex', alignItems: 'center', gap: 12, marginBottom: 10 },
    confBadge: { fontSize: 11, fontWeight: 700, padding: '3px 10px', borderRadius: 20, flexShrink: 0 },
    confText: { fontSize: 12, color: '#4a6478' },
};
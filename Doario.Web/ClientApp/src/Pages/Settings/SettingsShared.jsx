// SettingsShared.jsx — shared styles and components for all settings pages

import React from 'react';

export const S = {
    sectionTitle: {
        fontSize: 17, fontWeight: 800, color: '#0f2d4a', marginBottom: 4,
    },
    sectionSub: {
        fontSize: 12, color: '#7a9ab0', marginBottom: 20,
    },
    card: {
        background: '#fff',
        border: '1px solid #e2eaef',
        borderRadius: 12,
        padding: '24px 28px',
        marginBottom: 24,
    },
    label: {
        fontSize: 12, fontWeight: 600, color: '#4a6478',
        marginBottom: 5, display: 'block',
    },
    input: {
        width: '100%', padding: '9px 12px',
        border: '1px solid #d0dce6', borderRadius: 8,
        fontSize: 13, color: '#1a2e3b', fontFamily: 'inherit',
        boxSizing: 'border-box', marginBottom: 14,
        outline: 'none', background: '#fff',
    },
    inputReadOnly: {
        background: '#f7f9fb', color: '#7a9ab0', cursor: 'default',
    },
    divider: {
        border: 'none', borderTop: '1px solid #f0f4f7', margin: '20px 0',
    },
    btnPrimary: {
        padding: '9px 22px', background: '#0d9488',
        color: '#fff', border: 'none', borderRadius: 8,
        fontSize: 13, fontWeight: 600, cursor: 'pointer',
        fontFamily: 'inherit',
    },
    btnSecondary: {
        padding: '9px 22px', background: 'transparent',
        color: '#0d9488', border: '1px solid #0d9488',
        borderRadius: 8, fontSize: 13, fontWeight: 600,
        cursor: 'pointer', fontFamily: 'inherit',
    },
    btnDanger: {
        padding: '9px 22px', background: 'transparent',
        color: '#e53e3e', border: '1px solid #e53e3e',
        borderRadius: 8, fontSize: 13, fontWeight: 600,
        cursor: 'pointer', fontFamily: 'inherit',
    },
    msg: {
        fontSize: 12, padding: '8px 12px',
        borderRadius: 8, marginTop: 12,
    },
    msgSuccess: {
        color: '#0d9488', background: 'rgba(13,148,136,0.08)',
    },
    msgError: {
        color: '#e53e3e', background: 'rgba(229,62,62,0.08)',
    },
    msgWarning: {
        color: '#b45309', background: 'rgba(180,83,9,0.08)',
    },
    tagGreen: {
        display: 'inline-block', padding: '3px 12px',
        borderRadius: 20, fontSize: 11, fontWeight: 600,
        background: 'rgba(13,148,136,0.1)', color: '#0d9488',
    },
    apiKeyBox: {
        background: '#0f2d4a', color: '#99e0d9',
        padding: '12px 16px', borderRadius: 8,
        fontFamily: 'monospace', fontSize: 13,
        letterSpacing: '0.5px', wordBreak: 'break-all',
        marginBottom: 10,
    },
    csvErrRow: {
        fontSize: 11, color: '#e53e3e',
        background: 'rgba(229,62,62,0.06)',
        padding: '4px 8px', borderRadius: 4, marginBottom: 2,
    },
    row: {
        display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap',
    },
};

export const Shared = {
    Loading: () => (
        <div style={{ fontSize: 13, color: '#7a9ab0', padding: '20px 0' }}>
            Loading...
        </div>
    ),
    Divider: () => <hr style={S.divider} />,
};
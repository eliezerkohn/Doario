// SearchPanel.jsx — unified search panel with type selector

import React, { useState } from 'react';
import MailSearch from './MailSearch';
import SenderSearch from './SenderSearch';
import ChecksSearch from './ChecksSearch';

const TYPES = [
    { value: 'staff', label: 'By Staff', icon: '👤' },
    { value: 'sender', label: 'By Sender', icon: '✉️' },
    { value: 'checks', label: 'Checks', icon: '💰' },
];

const SearchPanel = ({ staff, selected, onSelect, initialType }) => {
    const [type, setType] = useState(
        initialType === 'Staff' ? 'staff' :
            initialType === 'Sender' ? 'sender' :
                initialType === 'Checks' ? 'checks' : 'staff'
    );

    return (
        <div style={styles.wrap}>
            {/* Type selector */}
            <div style={styles.typeBar}>
                <span style={styles.typeLabel}>Search</span>
                <div style={styles.typeButtons}>
                    {TYPES.map(t => (
                        <button
                            key={t.value}
                            style={{
                                ...styles.typeBtn,
                                ...(type === t.value ? styles.typeBtnActive : {}),
                            }}
                            onClick={() => setType(t.value)}
                        >
                            <span style={styles.typeBtnIcon}>{t.icon}</span>
                            {t.label}
                        </button>
                    ))}
                </div>
            </div>

            {/* Panel content — no header since we show it above */}
            <div style={styles.content}>
                {type === 'staff' && (
                    <MailSearch
                        staff={staff}
                        selected={selected}
                        onSelect={onSelect}
                        hideHeader
                    />
                )}
                {type === 'sender' && (
                    <SenderSearch
                        selected={selected}
                        onSelect={onSelect}
                        hideHeader
                    />
                )}
                {type === 'checks' && (
                    <ChecksSearch
                        selected={selected}
                        onSelect={onSelect}
                        hideHeader
                    />
                )}
            </div>
        </div>
    );
};

const styles = {
    wrap: {
        width: 320, minWidth: 320,
        display: 'flex', flexDirection: 'column', height: '100vh',
        background: '#faf9f8', borderRight: '1px solid #edebe9',
        fontFamily: "'Plus Jakarta Sans', sans-serif",
    },
    typeBar: {
        padding: '12px 14px',
        borderBottom: '1px solid #edebe9',
        background: '#fff',
    },
    typeLabel: {
        display: 'block',
        fontSize: 11, fontWeight: 700, color: '#7a9ab0',
        textTransform: 'uppercase', letterSpacing: 1.5,
        marginBottom: 8,
    },
    typeButtons: {
        display: 'flex', gap: 6,
    },
    typeBtn: {
        flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center',
        gap: 5, padding: '7px 6px',
        border: '1px solid #e2eaef', borderRadius: 8,
        background: '#fff', color: '#4a6478',
        fontSize: 12, fontWeight: 600, cursor: 'pointer',
        fontFamily: 'inherit', transition: 'all 0.15s',
    },
    typeBtnActive: {
        background: '#0f2d4a', color: '#fff',
        border: '1px solid #0f2d4a',
    },
    typeBtnIcon: { fontSize: 13 },
    content: {
        flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden',
    },
};

export default SearchPanel;
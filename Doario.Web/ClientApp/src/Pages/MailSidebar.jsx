// MailSidebar.jsx — Light A style: dark navy sidebar, teal accents

import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const C = {
    navy: '#0f2d4a',
    teal: '#0d9488',
    tealMid: '#99e0d9',
    text: 'rgba(255,255,255,0.45)',
    textAct: '#ffffff',
    activeBg: 'rgba(13,148,136,0.25)',
};

const folders = [
    { name: 'Inbox', icon: '📥', showCount: true, canMarkAllRead: true, group: 'Main' },
    { name: 'Unassigned', icon: '📋', showCount: true, canMarkAllRead: false, group: 'Main' },
    { name: 'Assigned', icon: '✅', showCount: false, canMarkAllRead: false, group: 'Main' },
    { name: 'Actioned', icon: '☑️', showCount: false, canMarkAllRead: false, group: 'Main' },
    { name: 'Scan', icon: '🖨️', showCount: false, canMarkAllRead: false, group: 'Tools' },
    { name: 'Search by Staff', icon: '🔍', showCount: false, canMarkAllRead: false, group: 'Tools' },
    { name: 'Spam', icon: '🚫', showCount: true, canMarkAllRead: false, group: 'Filtered' },
    { name: 'Promotions', icon: '📢', showCount: true, canMarkAllRead: false, group: 'Filtered' },
];

// Folders that navigate to a separate page instead of filtering the mail list
const navigateFolders = {
    'Scan': '/scan',
    'Settings': '/settings/organisation',
};

const MailSidebar = ({ folder, onFolder, counts, onMarkAllRead }) => {
    const navigate = useNavigate();
    const [menuOpen, setMenuOpen] = useState(false);
    const [badgeHovered, setBadgeHovered] = useState(false);

    const groups = ['Main', 'Tools', 'Filtered'];

    const handleFolderClick = (f) => {
        setMenuOpen(false);
        if (navigateFolders[f.name]) {
            navigate(navigateFolders[f.name]);
        } else {
            onFolder(f.name);
        }
    };

    return (
        <div style={S.sidebar}>

            {/* Logo */}
            <div style={S.logo}>
                <div style={S.logoText}>Do<span style={{ color: C.tealMid }}>a</span>rio</div>
                <div style={S.logoSub}>Mail Room</div>
            </div>

            {/* Nav */}
            <nav style={S.nav}>
                {groups.map(group => (
                    <div key={group}>
                        <div style={S.groupLabel}>{group}</div>
                        {folders.filter(f => f.group === group).map(f => {
                            const active = folder === f.name;
                            const count = counts[f.name] ?? 0;
                            const showBadge = f.showCount && count > 0;
                            const isInbox = f.canMarkAllRead;

                            return (
                                <div key={f.name} style={S.folderRow}>
                                    <button
                                        style={{
                                            ...S.folderBtn,
                                            color: active ? C.textAct : C.text,
                                            background: active ? C.activeBg : 'transparent',
                                        }}
                                        onClick={() => handleFolderClick(f)}
                                    >
                                        <span style={S.folderIcon}>{f.icon}</span>
                                        <span style={S.folderLabel}>{f.name}</span>
                                        {showBadge && (
                                            <span
                                                style={S.badge}
                                                onMouseEnter={() => isInbox && setBadgeHovered(true)}
                                                onMouseLeave={() => { if (!menuOpen) setBadgeHovered(false); }}
                                                onClick={e => {
                                                    if (!isInbox) return;
                                                    e.stopPropagation();
                                                    setMenuOpen(o => !o);
                                                }}
                                            >
                                                {isInbox && badgeHovered ? '···' : count}
                                            </span>
                                        )}
                                    </button>

                                    {isInbox && menuOpen && (
                                        <div
                                            style={S.menuDropdown}
                                            onMouseLeave={() => { setMenuOpen(false); setBadgeHovered(false); }}
                                        >
                                            <button
                                                style={S.menuItem}
                                                onClick={() => { onMarkAllRead(); setMenuOpen(false); setBadgeHovered(false); }}
                                            >
                                                ✓ Mark all as read
                                            </button>
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                ))}
            </nav>

            {/* Bottom actions */}
            <div style={S.bottom}>
                <button style={S.settingsBtn} onClick={() => navigate('/settings/organisation')}>
                    ⚙️ Settings
                </button>
                <button style={S.uploadBtn} onClick={() => navigate('/upload-test')}>
                    + Upload Document
                </button>
            </div>

        </div>
    );
};

const S = {
    sidebar: {
        width: 230, minWidth: 230,
        background: '#0f2d4a',
        display: 'flex', flexDirection: 'column', height: '100vh',
        fontFamily: "'Plus Jakarta Sans', sans-serif",
    },
    logo: {
        padding: '20px 18px 16px',
        borderBottom: '1px solid rgba(255,255,255,0.08)',
    },
    logoText: { fontSize: 22, fontWeight: 800, color: '#fff', letterSpacing: '-0.5px' },
    logoSub: {
        fontSize: 10, color: 'rgba(255,255,255,0.35)',
        letterSpacing: '2.5px', textTransform: 'uppercase', marginTop: 3,
    },
    nav: { flex: 1, padding: '10px', overflowY: 'auto' },
    groupLabel: {
        fontSize: 9, color: 'rgba(255,255,255,0.25)',
        letterSpacing: '2px', textTransform: 'uppercase',
        padding: '10px 10px 4px',
    },
    folderRow: { position: 'relative' },
    folderBtn: {
        display: 'flex', alignItems: 'center', gap: 10,
        width: '100%', padding: '9px 10px', border: 'none',
        borderRadius: 8, cursor: 'pointer', fontSize: 13,
        fontWeight: 500, transition: 'all 0.15s', marginBottom: 2,
        textAlign: 'left', fontFamily: 'inherit',
    },
    folderIcon: { fontSize: 15, width: 18, textAlign: 'center' },
    folderLabel: { flex: 1, color: 'inherit' },
    badge: {
        fontSize: 10, fontWeight: 700, background: '#0d9488',
        color: '#fff', padding: '2px 7px', borderRadius: 20,
        cursor: 'pointer', userSelect: 'none',
    },
    menuDropdown: {
        position: 'absolute', right: 8, top: '100%',
        background: '#fff', border: '1px solid #e2eaef',
        borderRadius: 8, boxShadow: '0 4px 12px rgba(0,0,0,0.12)',
        zIndex: 100, minWidth: 160,
    },
    menuItem: {
        display: 'block', width: '100%', padding: '8px 14px',
        background: 'transparent', border: 'none', cursor: 'pointer',
        fontSize: 13, color: '#1a2e3b', textAlign: 'left',
    },
    bottom: {
        padding: '12px',
        borderTop: '1px solid rgba(255,255,255,0.08)',
        display: 'flex', flexDirection: 'column', gap: 8,
    },
    settingsBtn: {
        width: '100%', padding: '9px 0',
        background: 'transparent',
        border: '1px solid rgba(255,255,255,0.15)',
        color: 'rgba(255,255,255,0.6)', borderRadius: 8,
        fontSize: 12, fontWeight: 600, cursor: 'pointer',
        fontFamily: 'inherit',
    },
    uploadBtn: {
        width: '100%', padding: '10px 0',
        background: '#0d9488', color: '#fff', border: 'none',
        borderRadius: 8, fontSize: 12, fontWeight: 700,
        cursor: 'pointer', fontFamily: 'inherit',
    },
};

export default MailSidebar;
// SettingsLayout.jsx — Settings shell with left tab navigation

import React from 'react';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';

const tabs = [
    { label: 'Organisation', path: '/settings/organisation', icon: '🏢' },
    { label: 'Staff', path: '/settings/staff', icon: '👥' },
    { label: 'Integrations', path: '/settings/integrations', icon: '🔌' },
    { label: 'Subscription', path: '/settings/subscription', icon: '💳' },
    { label: 'Extraction Fields', path: '/settings/extraction-fields', icon: '🔍' },
];

export default function SettingsLayout() {
    const navigate = useNavigate();
    const location = useLocation();

    return (
        <div style={S.page}>

            {/* Page header */}
            <div style={S.header}>
                <div style={S.headerTitle}>Settings</div>
                <div style={S.headerSub}>Manage your organisation, staff, and integrations</div>
            </div>

            <div style={S.body}>

                {/* Left tab nav */}
                <div style={S.nav}>
                    {tabs.map(tab => {
                        const active = location.pathname === tab.path;
                        return (
                            <button
                                key={tab.path}
                                style={{
                                    ...S.navItem,
                                    background: active ? 'rgba(13,148,136,0.1)' : 'transparent',
                                    color: active ? '#0d9488' : '#4a6478',
                                    fontWeight: active ? 700 : 500,
                                    borderLeft: active ? '3px solid #0d9488' : '3px solid transparent',
                                }}
                                onClick={() => navigate(tab.path)}
                            >
                                <span style={S.navIcon}>{tab.icon}</span>
                                {tab.label}
                            </button>
                        );
                    })}
                </div>

                {/* Right content area */}
                <div style={S.content}>
                    <Outlet />
                </div>

            </div>
        </div>
    );
}

const S = {
    page: {
        fontFamily: "'Plus Jakarta Sans', sans-serif",
        color: '#1a2e3b',
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        background: '#f7f9fb',
    },
    header: {
        padding: '28px 40px 20px',
        borderBottom: '1px solid #e2eaef',
        background: '#fff',
    },
    headerTitle: {
        fontSize: 20, fontWeight: 800, color: '#0f2d4a',
    },
    headerSub: {
        fontSize: 12, color: '#7a9ab0', marginTop: 3,
    },
    body: {
        display: 'flex',
        flex: 1,
        overflow: 'hidden',
    },
    nav: {
        width: 200,
        minWidth: 200,
        background: '#fff',
        borderRight: '1px solid #e2eaef',
        padding: '16px 8px',
        display: 'flex',
        flexDirection: 'column',
        gap: 2,
    },
    navItem: {
        display: 'flex',
        alignItems: 'center',
        gap: 10,
        padding: '10px 14px',
        border: 'none',
        borderRadius: 8,
        cursor: 'pointer',
        fontSize: 13,
        textAlign: 'left',
        fontFamily: 'inherit',
        transition: 'all 0.15s',
    },
    navIcon: {
        fontSize: 15, width: 20, textAlign: 'center',
    },
    content: {
        flex: 1,
        overflowY: 'auto',
        padding: '32px 40px',
        maxWidth: 680,
    },
};
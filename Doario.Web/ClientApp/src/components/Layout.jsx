import React from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';

const Layout = () => {
    const location = useLocation();
    const navigate = useNavigate();

    const isMailPortal = location.pathname.startsWith('/admin/queue');

    if (isMailPortal) {
        return <Outlet />;
    }

    const isSettings = location.pathname.startsWith('/settings');
    const isUpload = location.pathname.startsWith('/upload-test');

    return (
        <div style={S.shell}>

            {/* ── Topbar ── */}
            <div style={S.topbar}>
                <div style={S.topbarLeft}>
                    <Link to="/admin/queue" style={S.logo}>
                        Do<span style={{ color: '#99e0d9' }}>a</span>rio
                    </Link>
                </div>
                <div style={S.topbarNav}>
                    <Link to="/upload-test" style={{
                        ...S.navLink,
                        ...(isUpload ? S.navLinkActive : {})
                    }}>
                        Upload
                    </Link>
                    <Link to="/settings/organisation" style={{
                        ...S.navLink,
                        ...(isSettings ? S.navLinkActive : {})
                    }}>
                        Settings
                    </Link>
                    <Link to="/admin/queue" style={S.navBtnMailQueue}>
                        Mail Queue
                    </Link>
                </div>
            </div>

            {/* ── Page content ── */}
            <div style={S.content}>
                <Outlet />
            </div>

        </div>
    );
};

const S = {
    shell: {
        display: 'flex',
        flexDirection: 'column',
        minHeight: '100vh',
        background: '#f7f9fb',
        fontFamily: "'Plus Jakarta Sans', sans-serif",
    },
    topbar: {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 32px',
        height: 56,
        background: '#0f2d4a',
        borderBottom: '1px solid rgba(255,255,255,0.08)',
        position: 'sticky',
        top: 0,
        zIndex: 100,
    },
    topbarLeft: {
        display: 'flex',
        alignItems: 'center',
        gap: 24,
    },
    logo: {
        fontSize: 20,
        fontWeight: 800,
        color: '#fff',
        textDecoration: 'none',
        letterSpacing: '-0.5px',
        fontFamily: "'Plus Jakarta Sans', sans-serif",
    },
    topbarNav: {
        display: 'flex',
        alignItems: 'center',
        gap: 8,
    },
    navLink: {
        fontSize: 13,
        fontWeight: 500,
        color: 'rgba(255,255,255,0.6)',
        textDecoration: 'none',
        padding: '6px 14px',
        borderRadius: 6,
        transition: 'all 0.15s',
    },
    navLinkActive: {
        color: '#fff',
        background: 'rgba(255,255,255,0.1)',
    },
    navBtnMailQueue: {
        fontSize: 13,
        fontWeight: 600,
        color: '#fff',
        textDecoration: 'none',
        padding: '7px 16px',
        borderRadius: 8,
        background: '#0d9488',
        marginLeft: 8,
    },
    content: {
        flex: 1,
    },
};

export default Layout;
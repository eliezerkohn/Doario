import React from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';

const Layout = () => {
    const location = useLocation();
    const isMailPortal = location.pathname.startsWith('/admin/queue');

    return (
        <div style={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
            {/* Navbar — hidden on mail portal (it has its own sidebar) */}
            {!isMailPortal && (
                <header>
                    <nav className="navbar navbar-expand-sm navbar-dark fixed-top bg-dark border-bottom box-shadow">
                        <div className="container">
                            <a className="navbar-brand">Doario</a>
                            <button className="navbar-toggler" type="button" data-toggle="collapse"
                                data-target=".navbar-collapse" aria-controls="navbarSupportedContent"
                                aria-expanded="false" aria-label="Toggle navigation">
                                <span className="navbar-toggler-icon"></span>
                            </button>
                            <div className="navbar-collapse collapse d-sm-inline-flex justify-content-between">
                                <ul className="navbar-nav flex-grow-1">
                                    <li className="nav-item"><Link to="/" className='nav-link text-light'>Home</Link></li>
                                    <li className="nav-item"><Link to="/upload-test" className='nav-link text-light'>📷 Upload</Link></li>
                                    <li className="nav-item"><Link to="/admin/queue" className='nav-link text-light'>📬 Mail Queue</Link></li>
                                </ul>
                            </div>
                        </div>
                    </nav>
                </header>
            )}

            {/* Page content */}
            {isMailPortal ? (
                // Mail portal gets full screen — no container, no margin
                <Outlet />
            ) : (
                <div className="container mt-5">
                    <Outlet />
                </div>
            )}
        </div>
    );
};

export default Layout;
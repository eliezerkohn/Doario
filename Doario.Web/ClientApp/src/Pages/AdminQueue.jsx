// AdminQueue.jsx — mail room queue page
// Status labels come from server (doc.statusName) — no hardcoded maps.
// Polls every 5 seconds to update OCR/AI progress without hard refresh.

import React, { useEffect, useState, useRef, useCallback } from 'react';
import axios from 'axios';
import DocumentCard from './DocumentCard';
import AssignModal from './AssignModal';

const POLL_MS = 5000;

const badgeClass = (statusId) => ({
    1: 'bg-warning text-dark',
    2: 'bg-success',
    3: 'bg-info text-dark',
    4: 'bg-primary',
    5: 'bg-secondary',
    6: 'bg-danger',
}[statusId] ?? 'bg-secondary');

const StatusPills = ({ docs }) => {
    const counts = docs.reduce((acc, d) => {
        acc[d.statusId] = { count: (acc[d.statusId]?.count ?? 0) + 1, name: d.statusName };
        return acc;
    }, {});
    return (
        <div className="d-flex flex-wrap gap-2 mb-3">
            {Object.entries(counts).map(([id, { count, name }]) => (
                <span key={id} className={`badge ${badgeClass(parseInt(id))}`}>
                    {count} {name ?? 'Unknown'}
                </span>
            ))}
        </div>
    );
};

const stripMarkup = (html) => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
};

const AdminQueue = () => {
    const [docs, setDocs] = useState([]);
    const [staff, setStaff] = useState([]);
    const [loading, setLoading] = useState(true);
    const [loadingMore, setLoadingMore] = useState(false);
    const [error, setError] = useState(null);
    const [search, setSearch] = useState('');
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const [assigningDoc, setAssigningDoc] = useState(null);

    // Tracks locally-assigned docs with timestamp.
    // Poll keeps statusId=2 for 15s to prevent flicker.
    const localAssigned = useRef({});
    const pollRef = useRef(null);

    const loadDocs = async (pageNum, append = false) => {
        try {
            const r = await axios.get(`/api/admin/queue?page=${pageNum}`);
            if (append) {
                setDocs(prev => [...prev, ...r.data]);
            } else {
                setDocs(r.data);
            }
            setHasMore(r.data.length === 50);
        } catch (e) {
            setError(e.response?.status === 403
                ? 'Access denied — DoarioAdmin role required.'
                : 'Failed to load queue.');
        }
    };

    const silentRefresh = useCallback(async () => {
        try {
            const r = await axios.get('/api/admin/queue?page=1&pageSize=500');
            const now = Date.now();
            const map = new Map(r.data.map(d => [d.documentId, d]));

            setDocs(prev => prev.map(d => {
                const server = map.get(d.documentId);
                if (!server) return d;

                const assignedAt = localAssigned.current[d.documentId];
                const recentlyAssigned = assignedAt && (now - assignedAt < 15000);

                // Server hasn't caught up — keep optimistic statusId=2
                if (recentlyAssigned && server.statusId !== 2) {
                    return { ...server, statusId: 2, statusName: 'Assigned' };
                }

                // Server confirmed — clear local override
                if (server.statusId === 2) {
                    delete localAssigned.current[d.documentId];
                }

                return server;
            }));
        } catch { /* never surface background poll errors */ }
    }, []);

    const loadStaff = async () => {
        try {
            const r = await axios.get('/api/assignment/staff');
            setStaff(r.data);
        } catch { /* silent */ }
    };

    useEffect(() => {
        Promise.all([loadDocs(1), loadStaff()]).finally(() => setLoading(false));
        pollRef.current = setInterval(silentRefresh, POLL_MS);
        return () => clearInterval(pollRef.current);
    }, [silentRefresh]);

    const handleLoadMore = async () => {
        const nextPage = page + 1;
        setLoadingMore(true);
        await loadDocs(nextPage, true);
        setPage(nextPage);
        setLoadingMore(false);
    };

    // Called BEFORE API call — sets guard immediately so poll can't flicker
    const handleAssigned = (documentId) => {
        localAssigned.current[documentId] = Date.now();
        setDocs(prev => prev.map(d =>
            d.documentId === documentId
                ? { ...d, statusId: 2, statusName: 'Assigned' }
                : d
        ));
    };

    // Called if API call fails — reverts optimistic update
    const handleReverted = (documentId, originalStatusId) => {
        delete localAssigned.current[documentId];
        setDocs(prev => prev.map(d =>
            d.documentId === documentId
                ? { ...d, statusId: originalStatusId }
                : d
        ));
    };

    const filtered = docs.filter(d => {
        if (!search) return true;
        const q = search.toLowerCase();
        return (
            d.originalFileName?.toLowerCase().includes(q) ||
            stripMarkup(d.aiSummary).toLowerCase().includes(q) ||
            d.ocrText?.toLowerCase().includes(q)
        );
    });

    return (
        <div className="mt-5 pt-3">
            <h2>Mail Room Queue</h2>
            <p className="text-muted">
                {docs.length} document{docs.length !== 1 ? 's' : ''} loaded
            </p>

            {!loading && docs.length > 0 && <StatusPills docs={docs} />}

            <input
                className="form-control mb-3"
                placeholder="Search file name, summary or OCR text…"
                value={search}
                onChange={e => setSearch(e.target.value)}
            />

            {loading && <p>Loading…</p>}
            {error && <div className="alert alert-danger">{error}</div>}
            {!loading && !error && filtered.length === 0 && (
                <div className="alert alert-info">No documents found.</div>
            )}

            {filtered.map(doc => (
                <DocumentCard
                    key={doc.documentId}
                    doc={doc}
                    onAssign={setAssigningDoc}
                    staffAvailable={staff.length > 0}
                    isAssignedOverride={!!localAssigned.current[doc.documentId]}
                />
            ))}

            {!loading && !error && hasMore && (
                <div className="text-center mb-4">
                    <button
                        className="btn btn-outline-primary"
                        onClick={handleLoadMore}
                        disabled={loadingMore}
                    >
                        {loadingMore ? 'Loading…' : 'Load More'}
                    </button>
                </div>
            )}

            {!loading && !error && !hasMore && docs.length > 0 && (
                <p className="text-center text-muted mb-4">All documents loaded</p>
            )}

            {assigningDoc && (
                <AssignModal
                    doc={assigningDoc}
                    staff={staff}
                    onClose={() => setAssigningDoc(null)}
                    onAssigned={handleAssigned}
                    onReverted={handleReverted}
                />
            )}
        </div>
    );
};

export default AdminQueue;
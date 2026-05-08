// MailPortal.jsx — root layout: sidebar + document list + reading pane

import React, { useEffect, useState, useRef, useCallback } from 'react';
import axios from 'axios';
import MailSidebar from './MailSidebar';
import MailList from './MailList';
import PendingApprovals from './PendingApprovals';
import MailReader from './MailReader';
import AssignModal from './AssignModal';

const PAGE_SIZE = 50;
const POLL_MS = 30000;

const FOLDER_STATUS_MAP = {
    'Inbox': '1,2',
    'Unassigned': '1',
    'Assigned': '2',
    'Actioned': '4',
    'Spam': '7',
    'Promotions': '8',
    'Trash': '9',
};

const MailPortal = () => {
    const [docs, setDocs] = useState([]);
    const [staff, setStaff] = useState([]);
    const [selected, setSelected] = useState(null);
    const [folder, setFolder] = useState('Inbox');
    const [loading, setLoading] = useState(true);
    const [loadingMore, setLoadingMore] = useState(false);
    const [hasMore, setHasMore] = useState(true);
    const [page, setPage] = useState(1);
    const [counts, setCounts] = useState({});
    const [pendingCount, setPendingCount] = useState(0);
    const [assigningDoc, setAssigningDoc] = useState(null);

    const localAssigned = useRef({});
    const pollRef = useRef(null);
    const prevFolderRef = useRef(null);

    // ── Load counts ───────────────────────────────────────────────────────────

    const loadCounts = useCallback(async () => {
        try {
            const [countsRes, pendingRes] = await Promise.all([
                axios.get('/api/admin/counts'),
                axios.get('/api/assignment/pending-count'),
            ]);
            setCounts(countsRes.data);
            setPendingCount(pendingRes.data.count);
        } catch { }
    }, []);

    const loadStaff = useCallback(async () => {
        try {
            const r = await axios.get('/api/assignment/staff');
            setStaff(r.data);
        } catch { }
    }, []);

    // ── Load docs for folder ──────────────────────────────────────────────────

    const loadDocs = useCallback(async (currentFolder, pageNum = 1, append = false) => {
        let data = [];

        if (currentFolder === 'Checks') {
            // Checks uses dedicated endpoint — returns only actual checks
            const r = await axios.get('/api/admin/checks');
            data = r.data;
            setHasMore(false);
        } else {
            const statusIds = FOLDER_STATUS_MAP[currentFolder];
            let url = `/api/admin/queue?page=${pageNum}&pageSize=${PAGE_SIZE}`;
            if (statusIds) url += `&statusIds=${statusIds}`;
            const r = await axios.get(url);
            data = r.data;
            setHasMore(data.length === PAGE_SIZE);
        }

        if (append) {
            setDocs(prev => [...prev, ...data]);
        } else {
            setDocs(data);
        }
    }, []);

    // ── Silent poll ───────────────────────────────────────────────────────────

    const silentRefresh = useCallback(async (currentFolder) => {
        if (currentFolder === 'Checks') return; // checks don't need polling

        const statusIds = FOLDER_STATUS_MAP[currentFolder];
        let url = `/api/admin/queue?page=1&pageSize=${PAGE_SIZE}`;
        if (statusIds) url += `&statusIds=${statusIds}`;

        try {
            const r = await axios.get(url);
            const now = Date.now();

            setDocs(prev => {
                const serverMap = new Map(r.data.map(d => [d.documentId, d]));
                const existingIds = new Set(prev.map(d => d.documentId));
                const newDocs = r.data.filter(d => !existingIds.has(d.documentId));
                const updated = prev.map(d => {
                    const server = serverMap.get(d.documentId);
                    if (!server) return d;
                    const assignedAt = localAssigned.current[d.documentId];
                    const recentlyAssigned = assignedAt && (now - assignedAt < 15000);
                    if (recentlyAssigned && server.statusId !== 2)
                        return { ...server, statusId: 2, statusName: 'Assigned', isViewed: d.isViewed };
                    if (server.statusId === 2)
                        delete localAssigned.current[d.documentId];
                    return server;
                });
                return newDocs.length > 0 ? [...newDocs, ...updated] : updated;
            });

            setSelected(prev => {
                if (!prev) return prev;
                return r.data.find(d => d.documentId === prev.documentId) ?? prev;
            });
        } catch { }

        loadCounts();
    }, [loadCounts]);

    // ── Initial load ──────────────────────────────────────────────────────────

    useEffect(() => {
        const init = async () => {
            setLoading(true);
            await Promise.all([
                loadDocs('Inbox', 1),
                loadStaff(),
                loadCounts(),
            ]);
            setLoading(false);
            prevFolderRef.current = 'Inbox';
        };

        init();

        pollRef.current = setInterval(() => {
            const f = prevFolderRef.current;
            if (f && f !== 'Pending Approvals') silentRefresh(f);
        }, POLL_MS);

        return () => clearInterval(pollRef.current);
    }, []); // eslint-disable-line

    // ── Folder switch ─────────────────────────────────────────────────────────

    useEffect(() => {
        if (prevFolderRef.current === null || prevFolderRef.current === folder) return;

        prevFolderRef.current = folder;

        if (folder === 'Pending Approvals') return;

        setLoading(true);
        setDocs([]);
        setPage(1);
        setHasMore(true);
        setSelected(null);
        loadDocs(folder, 1).finally(() => setLoading(false));
    }, [folder]); // eslint-disable-line

    // ── Folder change handler ─────────────────────────────────────────────────

    const handleFolderChange = (f) => {
        if (f === folder) return;
        setFolder(f);
    };

    // ── Load more ─────────────────────────────────────────────────────────────

    const handleLoadMore = async () => {
        const nextPage = page + 1;
        setLoadingMore(true);
        await loadDocs(folder, nextPage, true);
        setPage(nextPage);
        setLoadingMore(false);
    };

    // ── Handlers ──────────────────────────────────────────────────────────────

    const handleAssigned = (documentId) => {
        localAssigned.current[documentId] = Date.now();
        setDocs(prev => prev.map(d =>
            d.documentId === documentId ? { ...d, statusId: 2, statusName: 'Assigned' } : d
        ));
        setSelected(prev =>
            prev?.documentId === documentId ? { ...prev, statusId: 2, statusName: 'Assigned' } : prev
        );
        loadCounts();
    };

    const handleReverted = (documentId, originalStatusId) => {
        delete localAssigned.current[documentId];
        setDocs(prev => prev.map(d =>
            d.documentId === documentId ? { ...d, statusId: originalStatusId } : d
        ));
    };

    const handleStatusChanged = (documentId, newStatusId, newStatusName) => {
        setDocs(prev => prev.map(d =>
            d.documentId === documentId
                ? { ...d, statusId: newStatusId, statusName: newStatusName } : d
        ));
        setSelected(prev =>
            prev?.documentId === documentId
                ? { ...prev, statusId: newStatusId, statusName: newStatusName } : prev
        );
        loadCounts();
    };

    const handleDeleted = (documentId) => {
        setDocs(prev => prev.filter(d => d.documentId !== documentId));
        setSelected(prev => prev?.documentId === documentId ? null : prev);
        loadCounts();
    };

    const handleSelect = async (doc) => {
        setSelected(doc);
        if (!doc.isViewed) {
            try {
                await axios.post('/api/admin/mark-viewed', { documentId: doc.documentId });
                setDocs(prev => prev.map(d =>
                    d.documentId === doc.documentId ? { ...d, isViewed: true } : d
                ));
                loadCounts();
            } catch { }
        }
    };

    const handleMarkUnread = async (documentId) => {
        try {
            await axios.post('/api/admin/mark-unread', { documentId });
            setDocs(prev => prev.map(d =>
                d.documentId === documentId ? { ...d, isViewed: false } : d
            ));
            loadCounts();
        } catch { }
    };

    const handleMarkAllRead = async () => {
        const unread = docs.filter(d => !d.isViewed);
        await Promise.allSettled(
            unread.map(d => axios.post('/api/admin/mark-viewed', { documentId: d.documentId }))
        );
        setDocs(prev => prev.map(d => ({ ...d, isViewed: true })));
        loadCounts();
    };

    // ── Sidebar counts ────────────────────────────────────────────────────────

    const sidebarCounts = {
        'Inbox': counts.inbox ?? 0,
        'Unassigned': counts.unassigned ?? 0,
        'Assigned': counts.assigned ?? 0,
        'Actioned': counts.actioned ?? 0,
        'Spam': counts.spam ?? 0,
        'Promotions': counts.promotions ?? 0,
        'Trash': counts.trash ?? 0,
        'Pending Approvals': pendingCount,
        'Checks': counts.checks ?? 0,
    };

    const isPendingApprovals = folder === 'Pending Approvals';

    return (
        <div style={styles.root}>
            <MailSidebar
                folder={folder}
                onFolder={handleFolderChange}
                counts={sidebarCounts}
                onMarkAllRead={handleMarkAllRead}
            />

            {isPendingApprovals && (
                <PendingApprovals
                    staff={staff}
                    selected={selected}
                    onSelect={handleSelect}
                    onApproved={loadCounts}
                />
            )}

            {!isPendingApprovals && (
                <MailList
                    docs={docs}
                    selected={selected}
                    loading={loading}
                    loadingMore={loadingMore}
                    hasMore={hasMore}
                    folder={folder}
                    onSelect={handleSelect}
                    onMarkUnread={handleMarkUnread}
                    onLoadMore={handleLoadMore}
                />
            )}

            <MailReader
                doc={selected}
                staff={staff}
                onAssign={setAssigningDoc}
                localAssigned={localAssigned}
                onStatusChanged={handleStatusChanged}
                onDeleted={handleDeleted}
            />

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

const styles = {
    root: {
        display: 'flex',
        height: '100vh',
        fontFamily: "'Segoe UI', -apple-system, sans-serif",
        background: '#f3f2f1',
        overflow: 'hidden',
    },
};

export default MailPortal;
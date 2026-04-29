// MailPortal.jsx — root layout: sidebar + document list + reading pane

import React, { useEffect, useState, useRef, useCallback } from 'react';
import axios from 'axios';
import MailSidebar from './MailSidebar';
import MailList from './MailList';
import MailSearch from './MailSearch';
import SenderSearch from './SenderSearch';
import ChecksSearch from './ChecksSearch';
import MailReader from './MailReader';
import AssignModal from './AssignModal';

const POLL_MS = 5000;

const MailPortal = () => {
    const [docs, setDocs] = useState([]);
    const [staff, setStaff] = useState([]);
    const [selected, setSelected] = useState(null);
    const [folder, setFolder] = useState('Inbox');
    const [loading, setLoading] = useState(true);
    const [assigningDoc, setAssigningDoc] = useState(null);
    const localAssigned = useRef({});
    const pollRef = useRef(null);

    // ── Fetch ──────────────────────────────────────────────────────────────────

    const loadDocs = useCallback(async () => {
        try {
            const r = await axios.get('/api/admin/queue?page=1&pageSize=500');
            setDocs(r.data);
        } catch { /* silent */ }
    }, []);

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
                if (recentlyAssigned && server.statusId !== 2)
                    return { ...server, statusId: 2, statusName: 'Assigned', isViewed: d.isViewed };
                if (server.statusId === 2)
                    delete localAssigned.current[d.documentId];
                return server;
            }));

            // Update reading pane
            setSelected(prev => {
                if (!prev) return prev;
                const updated = map.get(prev.documentId);
                return updated ?? prev;
            });
        } catch { /* silent */ }
    }, []);

    const loadStaff = useCallback(async () => {
        try {
            const r = await axios.get('/api/assignment/staff');
            setStaff(r.data);
        } catch { /* silent */ }
    }, []);

    useEffect(() => {
        Promise.all([loadDocs(), loadStaff()]).finally(() => setLoading(false));
        pollRef.current = setInterval(silentRefresh, POLL_MS);
        return () => clearInterval(pollRef.current);
    }, [loadDocs, loadStaff, silentRefresh]);

    // ── Folder filtering ───────────────────────────────────────────────────────

    const folderStatusMap = {
        'Inbox': [1, 2],
        'Unassigned': [1],
        'Assigned': [2],
        'Actioned': [4],
        'Spam': [7],
        'Promotions': [8],
        'Trash': [9],
    };

    const isChecksFolder = folder === 'Checks';
    const isChecksSearch = folder === 'Search Checks';

    const folderDocs = isChecksFolder
        ? docs.filter(d => d.isCheck)
        : docs.filter(d => (folderStatusMap[folder] ?? [1]).includes(d.statusId));
    const inboxDocs = docs.filter(d => [1, 2].includes(d.statusId));

    const unviewedCount = inboxDocs.filter(d => !d.isViewed).length;

    const counts = {
        'Inbox': unviewedCount,
        'Unassigned': docs.filter(d => d.statusId === 1).length,
        'Assigned': docs.filter(d => d.statusId === 2).length,
        'Actioned': docs.filter(d => d.statusId === 4).length,
        'Spam': docs.filter(d => d.statusId === 7).length,
        'Promotions': docs.filter(d => d.statusId === 8).length,
        'Trash': docs.filter(d => d.statusId === 9).length,
        'Checks': docs.filter(d => d.isCheck).length,
    };

    // ── Handlers ───────────────────────────────────────────────────────────────

    const handleAssigned = (documentId) => {
        localAssigned.current[documentId] = Date.now();
        setDocs(prev => prev.map(d =>
            d.documentId === documentId ? { ...d, statusId: 2, statusName: 'Assigned' } : d
        ));
        setSelected(prev =>
            prev?.documentId === documentId ? { ...prev, statusId: 2, statusName: 'Assigned' } : prev
        );
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
    };

    const handleDeleted = (documentId) => {
        setDocs(prev => prev.filter(d => d.documentId !== documentId));
        setSelected(prev => prev?.documentId === documentId ? null : prev);
    };

    const handleSelect = async (doc) => {
        setSelected(doc);

        if (!doc.isViewed) {
            try {
                await axios.post('/api/admin/mark-viewed', { documentId: doc.documentId });
                setDocs(prev => prev.map(d =>
                    d.documentId === doc.documentId ? { ...d, isViewed: true } : d
                ));
            } catch { /* silent */ }
        }
    };

    const handleMarkUnread = async (documentId) => {
        try {
            await axios.post('/api/admin/mark-unread', { documentId });
            setDocs(prev => prev.map(d =>
                d.documentId === documentId ? { ...d, isViewed: false } : d
            ));
        } catch { /* silent */ }
    };

    const handleMarkAllRead = async () => {
        const unread = inboxDocs.filter(d => !d.isViewed);
        await Promise.allSettled(
            unread.map(d => axios.post('/api/admin/mark-viewed', { documentId: d.documentId }))
        );
        setDocs(prev => prev.map(d =>
            [1, 2].includes(d.statusId) ? { ...d, isViewed: true } : d
        ));
    };

    // ── Render ─────────────────────────────────────────────────────────────────

    const isStaffSearch = folder === 'Search by Staff';
    const isSenderSearch = folder === 'Search by Sender';
    const isSearch = isStaffSearch || isSenderSearch || isChecksSearch;

    return (
        <div style={styles.root}>
            <MailSidebar
                folder={folder}
                onFolder={f => { setFolder(f); setSelected(null); }}
                counts={counts}
                onMarkAllRead={handleMarkAllRead}
            />

            {isStaffSearch && (
                <MailSearch
                    staff={staff}
                    selected={selected}
                    onSelect={handleSelect}
                />
            )}

            {isSenderSearch && (
                <SenderSearch
                    selected={selected}
                    onSelect={handleSelect}
                />
            )}

            {isChecksSearch && (
                <ChecksSearch
                    selected={selected}
                    onSelect={handleSelect}
                />
            )}

            {!isSearch && !isChecksSearch && (
                <MailList
                    docs={folderDocs}
                    selected={selected}
                    loading={loading}
                    folder={folder}
                    onSelect={handleSelect}
                    onMarkUnread={handleMarkUnread}
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
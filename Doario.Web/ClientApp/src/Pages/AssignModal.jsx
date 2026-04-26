// AssignModal.jsx — assign or reassign a document to a staff member
// Optimistic update: marks assigned immediately before API call
// so the poll guard is active and button never flickers back to Assign.

import React, { useState } from 'react';
import axios from 'axios';

const AssignModal = ({ doc, staff, onClose, onAssigned, onReverted }) => {
    const [selectedStaffId, setSelectedStaffId] = useState('');
    const [ccStaffId, setCcStaffId] = useState('');
    const [note, setNote] = useState('');
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');

    const isReassign = doc.statusId === 2;

    const handleSubmit = async () => {
        if (!selectedStaffId) { setError('Please select a staff member.'); return; }
        setSaving(true);
        setError('');

        // Optimistic update BEFORE API call — poll guard is active immediately
        onAssigned(doc.documentId);

        try {
            await axios.post('/api/assignment/assign', {
                documentId: doc.documentId,
                staffId: selectedStaffId,
                ccStaffId: ccStaffId || null,
                note,
            });
            onClose();
        } catch (e) {
            // Revert optimistic update on failure
            onReverted(doc.documentId, doc.statusId);
            setError(e.response?.data?.error ?? 'Assignment failed.');
            setSaving(false);
        }
    };

    return (
        <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-dialog-centered">
                <div className="modal-content">

                    <div className="modal-header">
                        <h5 className="modal-title">
                            {isReassign ? 'Reassign Document' : 'Assign Document'}
                        </h5>
                        <button type="button" className="btn-close" onClick={onClose} />
                    </div>

                    <div className="modal-body">

                        <p className="text-muted small mb-3 font-monospace">{doc.originalFileName}</p>

                        <div className="mb-3">
                            <label className="form-label fw-semibold">Assign to</label>
                            <select
                                className="form-select"
                                value={selectedStaffId}
                                onChange={e => setSelectedStaffId(e.target.value)}
                            >
                                <option value="">Select staff member…</option>
                                {staff.map(s => (
                                    <option key={s.importedStaffId} value={s.importedStaffId}>
                                        {s.firstName} {s.lastName} — {s.email}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div className="mb-3">
                            <label className="form-label fw-semibold">
                                CC <span className="text-muted fw-normal">(optional)</span>
                            </label>
                            <select
                                className="form-select"
                                value={ccStaffId}
                                onChange={e => setCcStaffId(e.target.value)}
                            >
                                <option value="">No CC…</option>
                                {staff
                                    .filter(s => s.importedStaffId !== selectedStaffId)
                                    .map(s => (
                                        <option key={s.importedStaffId} value={s.importedStaffId}>
                                            {s.firstName} {s.lastName} — {s.email}
                                        </option>
                                    ))}
                            </select>
                        </div>

                        <div className="mb-2">
                            <label className="form-label fw-semibold">
                                Note <span className="text-muted fw-normal">(optional)</span>
                            </label>
                            <textarea
                                className="form-control"
                                rows={3}
                                placeholder="Add a note for this staff member…"
                                value={note}
                                onChange={e => setNote(e.target.value)}
                            />
                        </div>

                        {error && <div className="alert alert-danger py-2 small mt-3">{error}</div>}

                    </div>

                    <div className="modal-footer">
                        <button className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
                        <button className="btn btn-dark" onClick={handleSubmit} disabled={saving}>
                            {saving ? 'Saving…' : isReassign ? 'Reassign' : 'Assign'}
                        </button>
                    </div>

                </div>
            </div>
        </div>
    );
};

export default AssignModal;
// DocumentCard.jsx — single document row in the mail room queue
// Status label and colour come from the server (doc.statusName / doc.statusId).
// No hardcoded status maps needed here.

import React, { useState } from 'react';

// Badge colour by statusId — purely visual, not business logic
const badgeClass = (statusId) => ({
    1: 'bg-warning text-dark',
    2: 'bg-info text-dark',
    3: 'bg-success',
    4: 'bg-primary',
    5: 'bg-secondary',
    6: 'bg-danger',
}[statusId] ?? 'bg-secondary');

// Strip HTML and markdown for plain-text operations (truncation preview, search)
const stripMarkup = (html) => {
    if (!html) return '';
    return html
        .replace(/<[^>]*>/g, ' ')
        .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
        .replace(/\*\*/g, '')
        .replace(/\s+/g, ' ')
        .trim();
};

const DocumentCard = ({ doc, onAssign, staffAvailable, isAssignedOverride }) => {
    const [expanded, setExpanded] = useState(false);

    // Use local override for 15s after assigning so button doesn't flicker
    const effectiveStatusId = isAssignedOverride ? 3 : doc.statusId;
    const effectiveStatusName = isAssignedOverride ? 'Assigned' : doc.statusName;
    const isAssigned = effectiveStatusId === 2;

    const summaryPlain = stripMarkup(doc.aiSummary);
    const summaryTruncated = summaryPlain.length > 120;

    return (
        <div className="card mb-3 shadow-sm">
            <div className="card-body">

                {/* Top row — status badges + filename */}
                <div className="d-flex justify-content-between align-items-start gap-2 flex-wrap">
                    <div className="d-flex align-items-center gap-2 flex-wrap">

                        {/* Status from DB */}
                        <span className={`badge ${badgeClass(effectiveStatusId)}`}>
                            {effectiveStatusName ?? 'Unknown'}
                        </span>

                        {/* OCR / AI processing state */}
                        <span className={`badge ${doc.aiSummary ? 'bg-success' :
                                doc.ocrText ? 'bg-info text-dark' :
                                    'bg-warning text-dark'}`}>
                            {doc.aiSummary ? 'Summary Ready' :
                                doc.ocrText ? 'Pending Summary' :
                                    'Pending OCR'}
                        </span>

                        <small className="text-muted">
                            {new Date(doc.uploadedAt + 'Z').toLocaleString()}
                        </small>
                    </div>
                    <small className="text-muted font-monospace">
                        {doc.originalFileName || doc.documentId.substring(0, 8) + '…'}
                    </small>
                </div>

                {/* AI summary — rendered as HTML so <strong> and <br> display correctly */}
                {doc.aiSummary ? (
                    <div
                        className="mt-2 mb-0"
                        style={{ cursor: summaryTruncated ? 'pointer' : 'default', fontSize: '0.875rem' }}
                        onClick={() => summaryTruncated && setExpanded(e => !e)}
                    >
                        {expanded || !summaryTruncated ? (
                            <>
                                <div dangerouslySetInnerHTML={{ __html: doc.aiSummary }} />
                                {summaryTruncated && (
                                    <span className="text-primary small" style={{ cursor: 'pointer' }}>
                                        Show less
                                    </span>
                                )}
                            </>
                        ) : (
                            <div>
                                <span>{summaryPlain.substring(0, 120)}…</span>
                                <span className="text-primary ms-1 small">Show more</span>
                            </div>
                        )}
                    </div>
                ) : doc.ocrText ? (
                    <p className="mt-2 mb-0 text-muted fst-italic small">AI summary pending…</p>
                ) : (
                    <p className="mt-2 mb-0 text-muted fst-italic small">OCR not yet complete</p>
                )}

                {/* Assign / Reassign */}
                <div className="mt-3 d-flex gap-2 align-items-center">
                    <button
                        className={`btn btn-sm ${isAssigned ? 'btn-outline-warning' : 'btn-outline-dark'}`}
                        onClick={() => onAssign(doc)}
                        disabled={!staffAvailable}
                    >
                        {isAssigned ? 'Reassign' : 'Assign'}
                    </button>
                    {!staffAvailable && <small className="text-muted">No staff loaded</small>}
                </div>

            </div>
        </div>
    );
};

export default DocumentCard;
// DocumentCard.jsx — single document row in the mail room queue

import React, { useState } from 'react';

const badgeClass = (statusId) => ({
    1: 'bg-warning text-dark',
    2: 'bg-success',
    3: 'bg-info text-dark',
    4: 'bg-primary',
    5: 'bg-secondary',
    6: 'bg-danger',
}[statusId] ?? 'bg-secondary');

const stripMarkup = (html) => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
};

const DocumentCard = ({ doc, onAssign, staffAvailable, isAssignedOverride }) => {
    const [expanded, setExpanded] = useState(false);

    const effectiveStatusId = isAssignedOverride ? 2 : doc.statusId;
    const effectiveStatusName = isAssignedOverride ? 'Assigned' : doc.statusName;
    const isAssigned = effectiveStatusId === 2;

    const summaryPlain = stripMarkup(doc.aiSummary);
    const summaryTruncated = summaryPlain.length > 120;

    // Button is disabled if:
    // 1. No staff loaded in the dropdown
    // 2. AI summary not ready yet — staff should not receive an email with no summary
    const assignDisabled = !staffAvailable || !doc.aiSummary;

    // Tooltip explains why the button is disabled
    const assignTitle = !doc.aiSummary
        ? 'Waiting for AI summary before assigning'
        : !staffAvailable
            ? 'No staff loaded'
            : '';

    return (
        <div className="card mb-3 shadow-sm">
            <div className="card-body">

                {/* Top row — status badges + filename */}
                <div className="d-flex justify-content-between align-items-start gap-2 flex-wrap">
                    <div className="d-flex align-items-center gap-2 flex-wrap">
                        <span className={`badge ${badgeClass(effectiveStatusId)}`}>
                            {effectiveStatusName ?? 'Unknown'}
                        </span>
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

                {/* Summary */}
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

                {/* Assign / Reassign button */}
                <div className="mt-3 d-flex gap-2 align-items-center">
                    <button
                        className={`btn btn-sm ${isAssigned ? 'btn-outline-warning' : 'btn-outline-dark'}`}
                        onClick={() => onAssign(doc)}
                        disabled={assignDisabled}
                        title={assignTitle}
                    >
                        {isAssigned ? 'Reassign' : 'Assign'}
                    </button>
                    {!doc.aiSummary && (
                        <small className="text-muted">Waiting for summary…</small>
                    )}
                    {doc.aiSummary && !staffAvailable && (
                        <small className="text-muted">No staff loaded</small>
                    )}
                </div>

            </div>
        </div>
    );
};

export default DocumentCard;
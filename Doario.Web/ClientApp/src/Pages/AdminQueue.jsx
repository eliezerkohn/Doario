import React, { useEffect, useState } from 'react';
import axios from 'axios';

const AdminQueue = () => {
    const [docs, setDocs] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [search, setSearch] = useState('');

    useEffect(() => {
        axios.get('/api/admin/queue')
            .then(r => setDocs(r.data))
            .catch(e => setError(e.response?.status === 403 ? 'Access denied — DoarioAdmin role required.' : 'Failed to load queue.'))
            .finally(() => setLoading(false));
    }, []);

    const filtered = docs.filter(d =>
        !search || d.ocrText?.toLowerCase().includes(search.toLowerCase())
    );

    return (
        <div className="mt-4">
            <h2>Mail Room Queue</h2>
            <p className="text-muted">{docs.length} document{docs.length !== 1 ? 's' : ''} in queue</p>

            <input
                className="form-control mb-3"
                placeholder="Search OCR text..."
                value={search}
                onChange={e => setSearch(e.target.value)}
            />

            {loading && <p>Loading...</p>}
            {error && <div className="alert alert-danger">{error}</div>}

            {!loading && !error && filtered.length === 0 && (
                <div className="alert alert-info">No documents found.</div>
            )}

            {filtered.map(doc => (
                <div key={doc.documentId} className="card mb-3">
                    <div className="card-body">
                        <div className="d-flex justify-content-between align-items-start">
                            <div>
                                <span className={`badge me-2 ${doc.aiSummary ? 'bg-success' : doc.ocrText ? 'bg-info' : 'bg-warning text-dark'}`}>
                                    {doc.aiSummary ? 'Summary Ready' : doc.ocrText ? 'Pending Summary' : 'Pending OCR'}
                                </span>
                                <small className="text-muted">
                                    {new Date(doc.uploadedAt + 'Z').toLocaleString()}
                                </small>
                            </div>
                            <small className="text-muted font-monospace">
                                {doc.documentId.substring(0, 8)}...
                            </small>
                        </div>

                        {doc.aiSummary ? (
                            <p className="mt-2 mb-0" style={{ whiteSpace: 'pre-wrap' }}>
                                {doc.aiSummary}
                            </p>
                        ) : doc.ocrText ? (
                            <p className="mt-2 mb-0 text-muted fst-italic">AI summary pending...</p>
                        ) : (
                            <p className="mt-2 mb-0 text-muted fst-italic">OCR not yet complete</p>
                        )}
                    </div>
                </div>
            ))}
        </div>
    );
};

export default AdminQueue;
// ExtractionFieldsSettings.jsx

import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { S, Shared } from './SettingsShared';

export default function ExtractionFieldsSettings() {
    const [fields, setFields] = useState([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [msg, setMsg] = useState(null);
    const [error, setError] = useState(null);

    // New field form
    const [newName, setNewName] = useState('');
    const [newDesc, setNewDesc] = useState('');
    const [adding, setAdding] = useState(false);

    // Edit state — keyed by TenantExtractionFieldId
    const [editing, setEditing] = useState({});

    useEffect(() => { load(); }, []);

    const load = async () => {
        setLoading(true);
        try {
            const res = await axios.get('/api/settings/extraction-fields');
            setFields(res.data);
        } catch {
            setError('Failed to load extraction fields.');
        } finally {
            setLoading(false);
        }
    };

    const handleAdd = async () => {
        if (!newName.trim()) return;
        setAdding(true);
        setMsg(null);
        setError(null);
        try {
            const res = await axios.post('/api/settings/extraction-fields', {
                fieldName: newName.trim(),
                fieldDescription: newDesc.trim(),
            });
            setFields(prev => [res.data, ...prev]);
            setNewName('');
            setNewDesc('');
            setMsg('Field added.');
            setTimeout(() => setMsg(null), 3000);
        } catch {
            setError('Failed to add field.');
        } finally {
            setAdding(false);
        }
    };

    const handleEdit = (field) => {
        setEditing(prev => ({
            ...prev,
            [field.tenantExtractionFieldId]: {
                fieldName: field.fieldName,
                fieldDescription: field.fieldDescription,
            }
        }));
    };

    const handleCancelEdit = (id) => {
        setEditing(prev => { const n = { ...prev }; delete n[id]; return n; });
    };

    const handleSave = async (id) => {
        const e = editing[id];
        setSaving(true);
        setMsg(null);
        setError(null);
        try {
            const res = await axios.put(`/api/settings/extraction-fields/${id}`, {
                fieldName: e.fieldName,
                fieldDescription: e.fieldDescription,
            });
            setFields(prev => prev.map(f =>
                f.tenantExtractionFieldId === id ? res.data : f
            ));
            handleCancelEdit(id);
            setMsg('Field updated.');
            setTimeout(() => setMsg(null), 3000);
        } catch {
            setError('Failed to save field.');
        } finally {
            setSaving(false);
        }
    };

    // Soft delete — sets EndDate to now, stays in DB as history
    const handleDelete = async (id) => {
        if (!window.confirm('Remove this field? It will stop being extracted but remain in history.')) return;
        setMsg(null);
        setError(null);
        try {
            await axios.delete(`/api/settings/extraction-fields/${id}`);
            // Reload to get updated isActive state from server
            await load();
            setMsg('Field removed.');
            setTimeout(() => setMsg(null), 3000);
        } catch {
            setError('Failed to remove field.');
        }
    };

    const handleRestore = async (id) => {
        setMsg(null);
        setError(null);
        try {
            await axios.post(`/api/settings/extraction-fields/${id}/restore`);
            await load();
            setMsg('Field restored.');
            setTimeout(() => setMsg(null), 3000);
        } catch {
            setError('Failed to restore field.');
        }
    };

    if (loading) return <Shared.Loading />;

    const activeFields = fields.filter(f => f.isActive);
    const pastFields = fields.filter(f => !f.isActive);

    return (
        <div>
            <div style={S.sectionTitle}>Extraction Fields</div>
            <div style={S.sectionSub}>
                Define custom fields the AI will always look for in every document.
                Fields found are added to the document summary automatically.
            </div>

            {msg && <div style={{ ...S.msg, ...S.msgSuccess }}>✅ {msg}</div>}
            {error && <div style={{ ...S.msg, ...S.msgError }}>❌ {error}</div>}

            {/* Add new field */}
            <div style={S.card}>
                <div style={S.sectionTitle}>Add Field</div>
                <div style={S.sectionSub}>The AI will extract this value from every document it processes going forward.</div>

                <label style={S.label}>Field Name <span style={{ color: '#e53e3e' }}>*</span></label>
                <input
                    style={S.input}
                    placeholder="e.g. Patient Name, Invoice #, Income"
                    value={newName}
                    onChange={e => setNewName(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && handleAdd()}
                />

                <label style={S.label}>
                    Description{' '}
                    <span style={{ color: '#7a9ab0', fontWeight: 400 }}>(optional — helps the AI find it)</span>
                </label>
                <input
                    style={S.input}
                    placeholder="e.g. The patient's full legal name as it appears on the form"
                    value={newDesc}
                    onChange={e => setNewDesc(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && handleAdd()}
                />

                <button
                    style={{ ...S.btnPrimary, opacity: (!newName.trim() || adding) ? 0.6 : 1 }}
                    onClick={handleAdd}
                    disabled={!newName.trim() || adding}
                >
                    {adding ? 'Adding...' : '+ Add Field'}
                </button>
            </div>

            {/* Active fields */}
            <div style={S.card}>
                <div style={S.sectionTitle}>Active Fields</div>
                <div style={S.sectionSub}>These fields are extracted from every document the AI processes.</div>

                {activeFields.length === 0 && (
                    <div style={{ fontSize: 13, color: '#7a9ab0', padding: '12px 0' }}>
                        No active fields. Add one above.
                    </div>
                )}

                {activeFields.map((field, i) => {
                    const id = field.tenantExtractionFieldId;
                    const isEditing = !!editing[id];
                    const e = editing[id];

                    return (
                        <div key={id}>
                            {i > 0 && <Shared.Divider />}

                            {isEditing ? (
                                <div>
                                    <label style={S.label}>Field Name</label>
                                    <input
                                        style={S.input}
                                        value={e.fieldName}
                                        onChange={ev => setEditing(prev => ({
                                            ...prev,
                                            [id]: { ...prev[id], fieldName: ev.target.value }
                                        }))}
                                    />
                                    <label style={S.label}>Description</label>
                                    <input
                                        style={S.input}
                                        value={e.fieldDescription}
                                        onChange={ev => setEditing(prev => ({
                                            ...prev,
                                            [id]: { ...prev[id], fieldDescription: ev.target.value }
                                        }))}
                                    />
                                    <div style={S.row}>
                                        <button
                                            style={{ ...S.btnPrimary, opacity: saving ? 0.6 : 1 }}
                                            onClick={() => handleSave(id)}
                                            disabled={saving}
                                        >
                                            {saving ? 'Saving...' : 'Save'}
                                        </button>
                                        <button style={S.btnSecondary} onClick={() => handleCancelEdit(id)}>
                                            Cancel
                                        </button>
                                    </div>
                                </div>
                            ) : (
                                <div style={styles.fieldRow}>
                                    <div style={styles.fieldInfo}>
                                        <div style={styles.fieldName}>{field.fieldName}</div>
                                        {field.fieldDescription && (
                                            <div style={styles.fieldDesc}>{field.fieldDescription}</div>
                                        )}
                                        <div style={styles.fieldMeta}>
                                            Active since {new Date(field.startDate).toLocaleDateString([], {
                                                month: 'short', day: 'numeric', year: 'numeric'
                                            })}
                                        </div>
                                    </div>
                                    <div style={styles.fieldActions}>
                                        <button style={S.btnSecondary} onClick={() => handleEdit(field)}>Edit</button>
                                        <button style={S.btnDanger} onClick={() => handleDelete(id)}>Remove</button>
                                    </div>
                                </div>
                            )}
                        </div>
                    );
                })}
            </div>

            {/* Past fields — scrollable history */}
            {pastFields.length > 0 && (
                <div style={S.card}>
                    <div style={S.sectionTitle}>Past Fields</div>
                    <div style={S.sectionSub}>
                        Fields that have been removed. Kept for historical reference — these were extracted from documents at the time they were active.
                    </div>

                    <div style={styles.historyScroll}>
                        {pastFields.map((field, i) => (
                            <div key={field.tenantExtractionFieldId}>
                                {i > 0 && <Shared.Divider />}
                                <div style={styles.fieldRow}>
                                    <div style={styles.fieldInfo}>
                                        <div style={{ ...styles.fieldName, color: '#7a9ab0' }}>
                                            {field.fieldName}
                                        </div>
                                        {field.fieldDescription && (
                                            <div style={styles.fieldDesc}>{field.fieldDescription}</div>
                                        )}
                                        <div style={styles.fieldMeta}>
                                            Active {new Date(field.startDate).toLocaleDateString([], {
                                                month: 'short', day: 'numeric', year: 'numeric'
                                            })} — {new Date(field.endDate).toLocaleDateString([], {
                                                month: 'short', day: 'numeric', year: 'numeric'
                                            })}
                                        </div>
                                    </div>
                                    <div style={styles.fieldActions}>
                                        <button style={S.btnSecondary} onClick={() => handleRestore(field.tenantExtractionFieldId)}>
                                            Restore
                                        </button>
                                        <div style={styles.pastBadge}>Removed</div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}

const styles = {
    fieldRow: {
        display: 'flex', justifyContent: 'space-between',
        alignItems: 'flex-start', gap: 16, padding: '8px 0',
    },
    fieldInfo: { flex: 1, minWidth: 0 },
    fieldName: { fontSize: 14, fontWeight: 700, color: '#1a2e3b', marginBottom: 3 },
    fieldDesc: { fontSize: 12, color: '#4a6478', marginBottom: 4 },
    fieldMeta: { fontSize: 11, color: '#7a9ab0' },
    fieldActions: { display: 'flex', gap: 8, flexShrink: 0, alignItems: 'center' },
    historyScroll: {
        maxHeight: 320,
        overflowY: 'auto',
        paddingRight: 4,
    },
    pastBadge: {
        fontSize: 11, fontWeight: 600,
        color: '#7a9ab0', background: '#f0f4f7',
        padding: '3px 10px', borderRadius: 20,
        flexShrink: 0, alignSelf: 'center',
    },
};
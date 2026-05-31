import { useState, useEffect, useCallback } from "react";
import { useParams } from "react-router-dom";

const STATUS = { LOADING: "loading", READY: "ready", ERROR: "error", DONE: "done" };

export default function VerifyExtractionPage() {
    const { documentId, token } = useParams();
    const [status, setStatus] = useState(STATUS.LOADING);
    const [data, setData] = useState(null);
    const [error, setError] = useState("");
    const [fields, setFields] = useState([]);
    const [saving, setSaving] = useState({});
    const [allDone, setAllDone] = useState(false);
    const [editingId, setEditingId] = useState(null);
    const [editValue, setEditValue] = useState("");

    useEffect(() => {
        fetch(`/api/staff-action/extraction/${documentId}/${token}`)
            .then(r => {
                if (!r.ok) throw new Error("Invalid or expired link.");
                return r.json();
            })
            .then(d => {
                setData(d);
                setFields(d.fields || []);
                setStatus(STATUS.READY);
            })
            .catch(e => {
                setError(e.message);
                setStatus(STATUS.ERROR);
            });
    }, [documentId, token]);

    const confirmField = useCallback(async (fieldId, isConfirmed, correctedValue) => {
        setSaving(s => ({ ...s, [fieldId]: true }));
        try {
            await fetch(`/api/staff-action/extraction/${documentId}/${token}/confirm`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    documentExtractionResultId: fieldId,
                    isConfirmed,
                    correctedValue: correctedValue || ""
                })
            });
            setFields(prev => prev.map(f =>
                f.documentExtractionResultId === fieldId
                    ? { ...f, isConfirmed, correctedValue: correctedValue || f.correctedValue }
                    : f
            ));
            setEditingId(null);
        } finally {
            setSaving(s => ({ ...s, [fieldId]: false }));
        }
    }, [documentId, token]);

    useEffect(() => {
        if (fields.length > 0 && fields.every(f => f.isConfirmed !== null && f.isConfirmed !== undefined)) {
            setAllDone(true);
        }
    }, [fields]);

    const pendingCount = fields.filter(f => f.isConfirmed === null || f.isConfirmed === undefined).length;
    const confirmedCount = fields.filter(f => f.isConfirmed === true).length;
    const correctedCount = fields.filter(f => f.isConfirmed === false).length;

    if (status === STATUS.LOADING) return (
        <div style={styles.fullCenter}>
            <div style={styles.spinner} />
            <p style={styles.loadingText}>Loading document…</p>
        </div>
    );

    if (status === STATUS.ERROR) return (
        <div style={styles.fullCenter}>
            <div style={styles.errorBox}>
                <div style={{ fontSize: 40, marginBottom: 12 }}>⚠️</div>
                <h2 style={{ color: "#dc2626", margin: "0 0 8px" }}>Access Denied</h2>
                <p style={{ color: "#6b7280", margin: 0 }}>{error}</p>
            </div>
        </div>
    );

    return (
        <div style={styles.root}>
            {/* Header */}
            <header style={styles.header}>
                <div style={styles.headerLeft}>
                    <div style={styles.logo}>
                        Do<span style={{ color: "#34d399" }}>a</span>rio
                    </div>
                    <div style={styles.headerDivider} />
                    <div>
                        <div style={styles.headerTitle}>Verify Extraction</div>
                        <div style={styles.headerSub}>{data?.fileName}</div>
                    </div>
                </div>
                <div style={styles.headerStats}>
                    {confirmedCount > 0 && (
                        <span style={{ ...styles.badge, background: "#dcfce7", color: "#16a34a" }}>
                            ✓ {confirmedCount} confirmed
                        </span>
                    )}
                    {correctedCount > 0 && (
                        <span style={{ ...styles.badge, background: "#fef3c7", color: "#d97706" }}>
                            ✏ {correctedCount} corrected
                        </span>
                    )}
                    {pendingCount > 0 && (
                        <span style={{ ...styles.badge, background: "#f1f5f9", color: "#64748b" }}>
                            {pendingCount} pending
                        </span>
                    )}
                </div>
            </header>

            {allDone && (
                <div style={styles.doneBanner}>
                    <span style={{ fontSize: 18, marginRight: 8 }}>🎉</span>
                    All fields verified — you can close this window or continue reviewing.
                </div>
            )}

            {/* Body */}
            <div style={styles.body}>
                {/* Left — Fields */}
                <aside style={styles.sidebar}>
                    <div style={styles.sidebarHeader}>
                        <span style={styles.sidebarTitle}>Extracted Fields</span>
                        <span style={styles.sidebarCount}>{fields.length} fields</span>
                    </div>

                    {fields.length === 0 ? (
                        <div style={styles.emptyFields}>
                            <div style={{ fontSize: 32, marginBottom: 8 }}>📋</div>
                            <p style={{ color: "#9ca3af", fontSize: 13, margin: 0, textAlign: "center" }}>
                                No extraction fields found for this document.
                            </p>
                        </div>
                    ) : (
                        <div style={styles.fieldList}>
                            {fields.map(field => {
                                const isEditing = editingId === field.documentExtractionResultId;
                                const isSaving = saving[field.documentExtractionResultId];
                                const displayValue = field.isConfirmed === false && field.correctedValue
                                    ? field.correctedValue
                                    : field.fieldValue;

                                return (
                                    <div
                                        key={field.documentExtractionResultId}
                                        style={{
                                            ...styles.fieldCard,
                                            ...(field.isConfirmed === true ? styles.fieldConfirmed : {}),
                                            ...(field.isConfirmed === false ? styles.fieldCorrected : {}),
                                        }}
                                    >
                                        <div style={styles.fieldHeader}>
                                            <span style={styles.fieldName}>{field.fieldName}</span>
                                            {field.isConfirmed === true && (
                                                <span style={styles.confirmedBadge}>✓ Confirmed</span>
                                            )}
                                            {field.isConfirmed === false && (
                                                <span style={styles.correctedBadge}>✏ Corrected</span>
                                            )}
                                        </div>

                                        {isEditing ? (
                                            <div style={{ marginTop: 8 }}>
                                                <input
                                                    autoFocus
                                                    value={editValue}
                                                    onChange={e => setEditValue(e.target.value)}
                                                    style={styles.editInput}
                                                    onKeyDown={e => {
                                                        if (e.key === "Enter") confirmField(field.documentExtractionResultId, false, editValue);
                                                        if (e.key === "Escape") setEditingId(null);
                                                    }}
                                                />
                                                <div style={styles.editActions}>
                                                    <button
                                                        style={styles.btnSave}
                                                        disabled={isSaving}
                                                        onClick={() => confirmField(field.documentExtractionResultId, false, editValue)}
                                                    >
                                                        {isSaving ? "Saving…" : "Save Correction"}
                                                    </button>
                                                    <button
                                                        style={styles.btnCancel}
                                                        onClick={() => setEditingId(null)}
                                                    >
                                                        Cancel
                                                    </button>
                                                </div>
                                            </div>
                                        ) : (
                                            <>
                                                <div style={styles.fieldValue}>{displayValue || "—"}</div>
                                                {field.isConfirmed === null || field.isConfirmed === undefined ? (
                                                    <div style={styles.fieldActions}>
                                                        <button
                                                            style={styles.btnConfirm}
                                                            disabled={isSaving}
                                                            onClick={() => confirmField(field.documentExtractionResultId, true, "")}
                                                        >
                                                            {isSaving ? "…" : "✓ Correct"}
                                                        </button>
                                                        <button
                                                            style={styles.btnEdit}
                                                            onClick={() => {
                                                                setEditingId(field.documentExtractionResultId);
                                                                setEditValue(field.fieldValue || "");
                                                            }}
                                                        >
                                                            ✏ Edit
                                                        </button>
                                                    </div>
                                                ) : (
                                                    <button
                                                        style={styles.btnRedo}
                                                        onClick={() => {
                                                            setEditingId(field.documentExtractionResultId);
                                                            setEditValue(displayValue || "");
                                                        }}
                                                    >
                                                        Re-review
                                                    </button>
                                                )}
                                            </>
                                        )}
                                    </div>
                                );
                            })}
                        </div>
                    )}

                    {/* AI Summary */}
                    {data?.aiSummary && (
                        <div style={styles.summaryBox}>
                            <div style={styles.summaryTitle}>AI Summary</div>
                            <div
                                style={styles.summaryText}
                                dangerouslySetInnerHTML={{ __html: data.aiSummary }}
                            />
                        </div>
                    )}
                </aside>

                {/* Right — PDF Viewer via backend proxy */}
                <main style={styles.viewer}>
                    <iframe
                        src={`/api/staff-action/pdf/${documentId}/${token}`}
                        style={styles.iframe}
                        title="Document Preview"
                    />
                    {data?.sharePointUrl && (
                        <a
                            href={data.sharePointUrl}
                            target="_blank"
                            rel="noreferrer"
                            style={styles.spOverlay}
                        >
                            ↗ Open in SharePoint
                        </a>
                    )}
                </main>
            </div>
        </div>
    );
}

// ── Styles ────────────────────────────────────────────────────────────────────

const styles = {
    root: {
        fontFamily: "'Segoe UI', system-ui, sans-serif",
        background: "#f8fafc",
        minHeight: "100vh",
        display: "flex",
        flexDirection: "column",
    },
    header: {
        background: "#0f2d4a",
        padding: "14px 24px",
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        boxShadow: "0 2px 8px rgba(0,0,0,0.2)",
        flexShrink: 0,
    },
    headerLeft: {
        display: "flex",
        alignItems: "center",
        gap: 16,
    },
    logo: {
        fontSize: 20,
        fontWeight: 800,
        color: "#fff",
        letterSpacing: "-0.5px",
    },
    headerDivider: {
        width: 1,
        height: 28,
        background: "rgba(255,255,255,0.2)",
    },
    headerTitle: {
        color: "#fff",
        fontWeight: 600,
        fontSize: 15,
        lineHeight: 1.2,
    },
    headerSub: {
        color: "rgba(255,255,255,0.5)",
        fontSize: 12,
        marginTop: 2,
        maxWidth: 300,
        overflow: "hidden",
        textOverflow: "ellipsis",
        whiteSpace: "nowrap",
    },
    headerStats: {
        display: "flex",
        gap: 8,
        alignItems: "center",
    },
    badge: {
        padding: "4px 10px",
        borderRadius: 20,
        fontSize: 12,
        fontWeight: 600,
    },
    doneBanner: {
        background: "#dcfce7",
        borderBottom: "1px solid #bbf7d0",
        color: "#15803d",
        padding: "10px 24px",
        fontSize: 14,
        fontWeight: 500,
        display: "flex",
        alignItems: "center",
        flexShrink: 0,
    },
    body: {
        display: "flex",
        flex: 1,
        overflow: "hidden",
        height: "calc(100vh - 56px)",
    },
    sidebar: {
        width: 360,
        flexShrink: 0,
        background: "#fff",
        borderRight: "1px solid #e2e8f0",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden",
    },
    sidebarHeader: {
        padding: "16px 20px",
        borderBottom: "1px solid #e2e8f0",
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        flexShrink: 0,
    },
    sidebarTitle: {
        fontWeight: 700,
        fontSize: 14,
        color: "#0f172a",
    },
    sidebarCount: {
        fontSize: 12,
        color: "#94a3b8",
        fontWeight: 500,
    },
    fieldList: {
        overflowY: "auto",
        flex: 1,
        padding: "12px 16px",
        display: "flex",
        flexDirection: "column",
        gap: 10,
    },
    emptyFields: {
        flex: 1,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        padding: 32,
    },
    fieldCard: {
        background: "#f8fafc",
        border: "1px solid #e2e8f0",
        borderRadius: 10,
        padding: "12px 14px",
        transition: "border-color 0.15s",
    },
    fieldConfirmed: {
        background: "#f0fdf4",
        border: "1px solid #bbf7d0",
    },
    fieldCorrected: {
        background: "#fffbeb",
        border: "1px solid #fde68a",
    },
    fieldHeader: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: 4,
    },
    fieldName: {
        fontSize: 11,
        fontWeight: 700,
        color: "#64748b",
        textTransform: "uppercase",
        letterSpacing: "0.05em",
    },
    confirmedBadge: {
        fontSize: 11,
        color: "#16a34a",
        fontWeight: 600,
    },
    correctedBadge: {
        fontSize: 11,
        color: "#d97706",
        fontWeight: 600,
    },
    fieldValue: {
        fontSize: 15,
        color: "#0f172a",
        fontWeight: 500,
        marginBottom: 10,
        wordBreak: "break-word",
    },
    fieldActions: {
        display: "flex",
        gap: 8,
    },
    btnConfirm: {
        flex: 1,
        padding: "7px 0",
        background: "#16a34a",
        color: "#fff",
        border: "none",
        borderRadius: 6,
        fontSize: 13,
        fontWeight: 600,
        cursor: "pointer",
    },
    btnEdit: {
        flex: 1,
        padding: "7px 0",
        background: "#f1f5f9",
        color: "#475569",
        border: "1px solid #e2e8f0",
        borderRadius: 6,
        fontSize: 13,
        fontWeight: 600,
        cursor: "pointer",
    },
    btnRedo: {
        padding: "5px 12px",
        background: "none",
        color: "#94a3b8",
        border: "1px solid #e2e8f0",
        borderRadius: 6,
        fontSize: 12,
        cursor: "pointer",
    },
    editInput: {
        width: "100%",
        padding: "8px 10px",
        border: "2px solid #3b82f6",
        borderRadius: 6,
        fontSize: 14,
        fontFamily: "inherit",
        outline: "none",
        boxSizing: "border-box",
    },
    editActions: {
        display: "flex",
        gap: 8,
        marginTop: 8,
    },
    btnSave: {
        flex: 1,
        padding: "7px 0",
        background: "#2563eb",
        color: "#fff",
        border: "none",
        borderRadius: 6,
        fontSize: 13,
        fontWeight: 600,
        cursor: "pointer",
    },
    btnCancel: {
        padding: "7px 14px",
        background: "#f1f5f9",
        color: "#475569",
        border: "1px solid #e2e8f0",
        borderRadius: 6,
        fontSize: 13,
        cursor: "pointer",
    },
    summaryBox: {
        margin: "0 16px 16px",
        padding: "12px 14px",
        background: "#f0f9ff",
        border: "1px solid #bae6fd",
        borderRadius: 10,
        flexShrink: 0,
    },
    summaryTitle: {
        fontSize: 11,
        fontWeight: 700,
        color: "#0369a1",
        textTransform: "uppercase",
        letterSpacing: "0.05em",
        marginBottom: 6,
    },
    summaryText: {
        fontSize: 13,
        color: "#0c4a6e",
        lineHeight: 1.6,
    },
    viewer: {
        flex: 1,
        position: "relative",
        background: "#1e293b",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        overflow: "hidden",
    },
    iframe: {
        width: "100%",
        height: "100%",
        border: "none",
    },
    spOverlay: {
        position: "absolute",
        bottom: 16,
        right: 16,
        background: "rgba(15,45,74,0.9)",
        color: "#fff",
        padding: "8px 14px",
        borderRadius: 8,
        fontSize: 13,
        textDecoration: "none",
        fontWeight: 500,
        backdropFilter: "blur(4px)",
    },
    fullCenter: {
        minHeight: "100vh",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        background: "#f8fafc",
    },
    spinner: {
        width: 36,
        height: 36,
        border: "3px solid #e2e8f0",
        borderTop: "3px solid #0f2d4a",
        borderRadius: "50%",
        animation: "spin 0.8s linear infinite",
    },
    loadingText: {
        color: "#94a3b8",
        marginTop: 16,
        fontSize: 14,
    },
    errorBox: {
        background: "#fff",
        borderRadius: 12,
        padding: "40px 48px",
        textAlign: "center",
        maxWidth: 400,
        boxShadow: "0 4px 24px rgba(0,0,0,0.08)",
    },
};

const styleTag = document.createElement("style");
styleTag.textContent = `@keyframes spin { to { transform: rotate(360deg); } }`;
document.head.appendChild(styleTag);
import { useState } from "react";
import axios from "axios";

export default function SyncStaffButton() {
    const [loading, setLoading] = useState(false);
    const [result, setResult] = useState(null);
    const [error, setError] = useState(null);

    const handleSync = async () => {
        setLoading(true);
        setResult(null);
        setError(null);

        try {
            const res = await axios.post("/api/admin/sync-staff");
            setResult(res.data.message);
        } catch (err) {
            setError(err.response?.data?.error || "Sync failed. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{ marginBottom: "16px" }}>
            <button
                onClick={handleSync}
                disabled={loading}
                style={{
                    padding: "8px 18px",
                    backgroundColor: loading ? "#94a3b8" : "#0f172a",
                    color: "#fff",
                    border: "none",
                    borderRadius: "6px",
                    cursor: loading ? "not-allowed" : "pointer",
                    fontSize: "14px",
                    fontWeight: 500,
                    display: "flex",
                    alignItems: "center",
                    gap: "8px"
                }}
            >
                {loading ? (
                    <>
                        <span style={{ fontSize: "12px" }}>⏳</span> Syncing...
                    </>
                ) : (
                    <>
                        <span>🔄</span> Sync Staff from Microsoft 365
                    </>
                )}
            </button>

            {result && (
                <div style={{
                    marginTop: "10px",
                    padding: "10px 14px",
                    backgroundColor: "#f0fdf4",
                    border: "1px solid #86efac",
                    borderRadius: "6px",
                    color: "#166534",
                    fontSize: "13px"
                }}>
                    ✓ {result}
                </div>
            )}

            {error && (
                <div style={{
                    marginTop: "10px",
                    padding: "10px 14px",
                    backgroundColor: "#fef2f2",
                    border: "1px solid #fca5a5",
                    borderRadius: "6px",
                    color: "#991b1b",
                    fontSize: "13px"
                }}>
                    ✗ {error}
                </div>
            )}
        </div>
    );
}
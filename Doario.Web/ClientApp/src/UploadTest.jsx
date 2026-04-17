import { useState } from "react";
import axios from "axios";

export default function UploadTest() {
    const [file, setFile] = useState(null);
    const [result, setResult] = useState(null);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(false);

    const handleUpload = async () => {
        if (!file) return;

        const form = new FormData();
        form.append("file", file);

        setLoading(true);
        setError(null);
        setResult(null);

        try {
            const response = await axios.post("/api/upload", form);
            setResult(response.data);
        } catch (err) {
            setError(err.response?.data || "Upload failed");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{ padding: "40px", maxWidth: "600px", margin: "0 auto" }}>
            <h2>Doario — Upload Test</h2>
            <p>Select a scanned document to upload to SharePoint.</p>

            <input
                type="file"
                accept=".pdf,.jpg,.jpeg,.png,.tiff"
                onChange={e => setFile(e.target.files[0])}
                style={{ display: "block", marginBottom: "16px" }}
            />

            <button
                onClick={handleUpload}
                disabled={!file || loading}
                style={{ padding: "8px 24px", cursor: "pointer" }}
            >
                {loading ? "Uploading..." : "Upload to SharePoint"}
            </button>

            {result && (
                <div style={{
                    marginTop: "24px", padding: "16px",
                    background: "#f0fff0", border: "1px solid #4caf50"
                }}>
                    <p><strong>Upload successful</strong></p>
                    <p>Document ID: {result.documentId}</p>
                    <p>
                        <a href={result.sharePointUrl} target="_blank" rel="noreferrer">
                            View in SharePoint
                        </a>
                    </p>
                </div>
            )}

            {error && (
                <div style={{
                    marginTop: "24px", padding: "16px",
                    background: "#fff0f0", border: "1px solid #f44336"
                }}>
                    <p><strong>Error:</strong> {String(error)}</p>
                </div>
            )}
        </div>
    );
}

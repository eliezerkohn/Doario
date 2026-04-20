import { useState } from "react";
import axios from "axios";

export default function UploadTest() {
    const [file, setFile] = useState(null);
    const [preview, setPreview] = useState(null);
    const [result, setResult] = useState(null);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(false);

    const handleUpload = async (selectedFile) => {
        const fileToUpload = selectedFile || file;
        if (!fileToUpload) return;

        const form = new FormData();
        form.append("file", fileToUpload);

        setLoading(true);
        setError(null);
        setResult(null);

        try {
            const response = await axios.post("/api/upload", form);
            setResult(response.data);
            setPreview(null);
        } catch (err) {
            setError(err.response?.data || "Upload failed");
        } finally {
            setLoading(false);
        }
    };

    const handleFileChange = (e) => {
        const selected = e.target.files[0];
        if (selected) {
            setFile(selected);
            setPreview(null);
            setResult(null);
            setError(null);
        }
    };

    const handleCameraCapture = (e) => {
        const selected = e.target.files[0];
        if (selected) {
            setFile(selected);
            setPreview(URL.createObjectURL(selected));
            setResult(null);
            setError(null);
        }
    };

    return (
        <div style={{ padding: "40px", maxWidth: "600px", margin: "0 auto" }}>
            <h2>Doario — Upload Document</h2>
            <p>Select a scanned document or take a photo to upload.</p>

            {/* Standard file picker */}
            <input
                id="file-picker"
                type="file"
                accept=".pdf,.jpg,.jpeg,.png,.tiff"
                onChange={handleFileChange}
                style={{ display: "none" }}
            />

            {/* Camera capture — opens camera on mobile */}
            <input
                id="camera-capture"
                type="file"
                accept="image/*"
                capture="environment"
                onChange={handleCameraCapture}
                style={{ display: "none" }}
            />

            <div style={{ display: "flex", gap: "12px", marginBottom: "16px" }}>
                <button
                    className="btn btn-outline-secondary"
                    onClick={() => document.getElementById('file-picker').click()}
                >
                    📁 Choose File
                </button>

                <button
                    className="btn btn-outline-primary"
                    onClick={() => document.getElementById('camera-capture').click()}
                >
                    📷 Scan with Camera
                </button>
            </div>

            {preview && (
                <div style={{ marginBottom: "16px" }}>
                    <img
                        src={preview}
                        alt="Preview"
                        style={{ maxWidth: "100%", maxHeight: "300px", borderRadius: "8px", border: "1px solid #ddd" }}
                    />
                    <p className="text-muted mt-2">
                        Selected: <strong>{file.name}</strong>
                    </p>
                </div>
            )}

            {!preview && file && (
                <p className="text-muted" style={{ marginBottom: "16px" }}>
                    Selected: <strong>{file.name}</strong>
                </p>
            )}

            <button
                className="btn btn-primary"
                onClick={() => handleUpload()}
                disabled={!file || loading}
            >
                {loading ? "Uploading..." : "Upload to SharePoint"}
            </button>

            {result && (
                <div style={{
                    marginTop: "24px", padding: "16px",
                    background: "#f0fff0", border: "1px solid #4caf50",
                    borderRadius: "8px"
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
                    background: "#fff0f0", border: "1px solid #f44336",
                    borderRadius: "8px"
                }}>
                    <p><strong>Error:</strong> {String(error)}</p>
                </div>
            )}
        </div>
    );
}
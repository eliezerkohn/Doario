import { useState } from "react";
import { useNavigate } from "react-router-dom";

export default function LoginPage() {
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();

    const handleLogin = async (e) => {
        e.preventDefault();
        setError("");
        setLoading(true);
        try {
            const res = await fetch("/api/demo-auth/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password }),
            });
            if (res.ok) {
                navigate("/admin/queue");
            } else {
                setError("Invalid username or password.");
            }
        } catch {
            setError("Something went wrong. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={S.root}>
            <style>{`
                @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@700;800&family=DM+Sans:wght@300;400;500;600&display=swap');
                * { box-sizing: border-box; margin: 0; padding: 0; }
                .btn-login:hover { background: #2bc48a !important; transform: translateY(-1px); }
                .btn-login { transition: all 0.2s ease !important; }
                input:focus { border-color: #34d399 !important; outline: none; }
            `}</style>

            {/* Background */}
            <div style={S.glow1} />
            <div style={S.glow2} />

            {/* Card */}
            <div style={S.card}>
                <a href="/" style={S.logo}>Do<span style={{ color: "#34d399" }}>a</span>rio</a>
                <h1 style={S.title}>Welcome back</h1>
                <p style={S.sub}>Sign in to access the demo portal</p>

                <form onSubmit={handleLogin} style={S.form}>
                    <div style={S.field}>
                        <label style={S.label}>Username</label>
                        <input
                            type="text"
                            value={username}
                            onChange={e => setUsername(e.target.value)}
                            placeholder="Enter your username"
                            style={S.input}
                            autoFocus
                            required
                        />
                    </div>
                    <div style={S.field}>
                        <label style={S.label}>Password</label>
                        <input
                            type="password"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            placeholder="Enter your password"
                            style={S.input}
                            required
                        />
                    </div>

                    {error && <div style={S.error}>{error}</div>}

                    <button
                        type="submit"
                        style={S.btn}
                        className="btn-login"
                        disabled={loading}
                    >
                        {loading ? "Signing in…" : "Sign in →"}
                    </button>
                </form>

                <a href="/" style={S.back}>← Back to homepage</a>
            </div>
        </div>
    );
}

const S = {
    root: {
        fontFamily: "'DM Sans', sans-serif",
        background: "#0a1628",
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 24,
        position: "relative",
        overflow: "hidden",
    },
    glow1: {
        position: "absolute",
        top: "20%",
        left: "10%",
        width: 500,
        height: 500,
        background: "radial-gradient(circle, rgba(52,211,153,0.08) 0%, transparent 70%)",
        pointerEvents: "none",
    },
    glow2: {
        position: "absolute",
        bottom: "10%",
        right: "5%",
        width: 400,
        height: 400,
        background: "radial-gradient(circle, rgba(99,102,241,0.07) 0%, transparent 70%)",
        pointerEvents: "none",
    },
    card: {
        background: "rgba(255,255,255,0.03)",
        border: "1px solid rgba(255,255,255,0.08)",
        borderRadius: 20,
        padding: "48px 44px",
        width: "100%",
        maxWidth: 420,
        backdropFilter: "blur(20px)",
        boxShadow: "0 32px 80px rgba(0,0,0,0.4)",
        position: "relative",
        zIndex: 1,
    },
    logo: {
        fontFamily: "'Playfair Display', serif",
        fontSize: 22,
        fontWeight: 800,
        color: "#fff",
        letterSpacing: "-0.5px",
        textDecoration: "none",
        display: "block",
        marginBottom: 32,
    },
    title: {
        fontFamily: "'Playfair Display', serif",
        fontSize: 28,
        fontWeight: 800,
        color: "#fff",
        marginBottom: 8,
        letterSpacing: "-0.5px",
    },
    sub: {
        fontSize: 14,
        color: "rgba(255,255,255,0.4)",
        marginBottom: 36,
        fontWeight: 300,
    },
    form: {
        display: "flex",
        flexDirection: "column",
        gap: 20,
    },
    field: {
        display: "flex",
        flexDirection: "column",
        gap: 8,
    },
    label: {
        fontSize: 12,
        fontWeight: 600,
        color: "rgba(255,255,255,0.5)",
        textTransform: "uppercase",
        letterSpacing: "1px",
    },
    input: {
        padding: "12px 16px",
        background: "rgba(255,255,255,0.05)",
        border: "1px solid rgba(255,255,255,0.1)",
        borderRadius: 10,
        fontSize: 14,
        color: "#fff",
        fontFamily: "inherit",
        transition: "border-color 0.15s ease",
    },
    error: {
        padding: "10px 14px",
        background: "rgba(239,68,68,0.1)",
        border: "1px solid rgba(239,68,68,0.25)",
        borderRadius: 8,
        fontSize: 13,
        color: "#f87171",
    },
    btn: {
        padding: "13px",
        background: "#34d399",
        color: "#0a1628",
        border: "none",
        borderRadius: 10,
        fontSize: 15,
        fontWeight: 700,
        cursor: "pointer",
        fontFamily: "inherit",
        marginTop: 4,
    },
    back: {
        display: "block",
        textAlign: "center",
        marginTop: 24,
        fontSize: 13,
        color: "rgba(255,255,255,0.3)",
        textDecoration: "none",
    },
};
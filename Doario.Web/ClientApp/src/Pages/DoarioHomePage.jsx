import { useState, useEffect } from "react";

export default function DoarioHomePage() {
    const [scrolled, setScrolled] = useState(false);
    const [visible, setVisible] = useState({});
    const [menuOpen, setMenuOpen] = useState(false);

    useEffect(() => {
        const onScroll = () => setScrolled(window.scrollY > 40);
        window.addEventListener("scroll", onScroll);
        return () => window.removeEventListener("scroll", onScroll);
    }, []);

    useEffect(() => {
        const obs = new IntersectionObserver(
            (entries) => entries.forEach(e => {
                if (e.isIntersecting) setVisible(v => ({ ...v, [e.target.dataset.id]: true }));
            }),
            { threshold: 0.1 }
        );
        document.querySelectorAll("[data-id]").forEach(el => obs.observe(el));
        return () => obs.disconnect();
    }, []);

    const features = [
        { icon: "📬", title: "Instant Digitisation", desc: "Scan physical mail directly from your mailroom scanner. Doario processes pages in seconds — no manual data entry." },
        { icon: "🧠", title: "AI-Powered Reading", desc: "Azure AI reads every document, extracts key fields, classifies mail type, and writes a structured summary automatically." },
        { icon: "📧", title: "Delivered to Outlook", desc: "Staff receive mail items directly in their inbox with action buttons — Mark as Actioned, Add Note, Forward, Verify." },
        { icon: "🔍", title: "Verify Extractions", desc: "Staff can review and confirm AI-extracted fields against the original document with one click — no login needed." },
        { icon: "📁", title: "SharePoint Archive", desc: "Every scanned document is uploaded to your organisation's SharePoint automatically, organised by date." },
        { icon: "⚡", title: "Zero Manual Work", desc: "From scanner to staff inbox in under 60 seconds. No printing, no sorting, no filing — it just works." },
    ];

    const steps = [
        { num: "01", title: "Scan", desc: "Place mail in the scanner. Click Scan Now in Doario." },
        { num: "02", title: "Process", desc: "AI reads, classifies, and extracts data from every page." },
        { num: "03", title: "Deliver", desc: "Staff receive it in Outlook with all extracted info." },
        { num: "04", title: "Action", desc: "Staff confirm, note, or forward — directly from email." },
    ];

    return (
        <div style={S.root}>
            <style>{`
                @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@700;800;900&family=DM+Sans:wght@300;400;500;600&display=swap');
                * { box-sizing: border-box; margin: 0; padding: 0; }
                html { scroll-behavior: smooth; }
                [data-id] { opacity: 0; transform: translateY(24px); transition: opacity 0.6s ease, transform 0.6s ease; }
                [data-id].visible { opacity: 1; transform: translateY(0); }
                .stagger-1 { transition-delay: 0.05s !important; }
                .stagger-2 { transition-delay: 0.1s !important; }
                .stagger-3 { transition-delay: 0.15s !important; }
                .stagger-4 { transition-delay: 0.2s !important; }
                .stagger-5 { transition-delay: 0.25s !important; }
                .stagger-6 { transition-delay: 0.3s !important; }
                .btn-demo:hover { background: #2bc48a !important; transform: translateY(-2px); }
                .btn-demo { transition: all 0.2s ease !important; }
                .feature-card:hover { border-color: rgba(52,211,153,0.4) !important; transform: translateY(-3px); }
                .feature-card { transition: all 0.2s ease !important; }
                .nav-link:hover { color: #34d399 !important; }
                @media (max-width: 768px) {
                    .hero-inner { flex-direction: column !important; padding: 100px 20px 60px !important; gap: 48px !important; }
                    .hero-title { font-size: 44px !important; }
                    .hero-sub { font-size: 15px !important; }
                    .floating-card { width: 100% !important; max-width: 340px !important; align-self: center !important; }
                    .feature-grid { grid-template-columns: 1fr !important; }
                    .steps-row { grid-template-columns: 1fr 1fr !important; }
                    .section-title { font-size: 28px !important; }
                    .cta-title { font-size: 32px !important; }
                    .hero-stats { gap: 24px !important; }
                    .nav-links-desktop { display: none !important; }
                    .hamburger { display: flex !important; }
                }
                @media (max-width: 480px) {
                    .steps-row { grid-template-columns: 1fr !important; }
                    .hero-title { font-size: 36px !important; }
                    .hero-ctas { flex-direction: column !important; }
                    .hero-ctas a { text-align: center !important; }
                }
            `}</style>

            {/* ── Nav ── */}
            <nav style={{ ...S.nav, background: scrolled ? "rgba(10,22,40,0.97)" : "transparent", backdropFilter: scrolled ? "blur(12px)" : "none", borderBottom: scrolled ? "1px solid rgba(255,255,255,0.06)" : "none" }}>
                <div style={S.navInner}>
                    <div style={S.logo}>Do<span style={{ color: "#34d399" }}>a</span>rio</div>
                    <div className="nav-links-desktop" style={S.navLinks}>
                        <a href="#features" style={S.navLink} className="nav-link">Features</a>
                        <a href="#how" style={S.navLink} className="nav-link">How it works</a>
                        <a href="/login" style={S.navCta} className="btn-demo">See it in action →</a>
                    </div>
                    {/* Hamburger */}
                    <button
                        className="hamburger"
                        style={{ display: "none", flexDirection: "column", gap: 5, background: "none", border: "none", cursor: "pointer", padding: 8 }}
                        onClick={() => setMenuOpen(!menuOpen)}
                    >
                        <span style={{ width: 22, height: 2, background: "#fff", borderRadius: 2, transition: "all 0.2s", transform: menuOpen ? "rotate(45deg) translateY(7px)" : "none" }} />
                        <span style={{ width: 22, height: 2, background: "#fff", borderRadius: 2, opacity: menuOpen ? 0 : 1 }} />
                        <span style={{ width: 22, height: 2, background: "#fff", borderRadius: 2, transition: "all 0.2s", transform: menuOpen ? "rotate(-45deg) translateY(-7px)" : "none" }} />
                    </button>
                </div>
                {/* Mobile menu */}
                {menuOpen && (
                    <div style={{ background: "rgba(10,22,40,0.98)", padding: "16px 24px 24px", borderTop: "1px solid rgba(255,255,255,0.06)", display: "flex", flexDirection: "column", gap: 16 }}>
                        <a href="#features" style={{ color: "rgba(255,255,255,0.7)", textDecoration: "none", fontSize: 15, fontWeight: 500 }} onClick={() => setMenuOpen(false)}>Features</a>
                        <a href="#how" style={{ color: "rgba(255,255,255,0.7)", textDecoration: "none", fontSize: 15, fontWeight: 500 }} onClick={() => setMenuOpen(false)}>How it works</a>
                        <a href="/login" style={{ ...S.navCta, textAlign: "center" }} className="btn-demo">See it in action →</a>
                    </div>
                )}
            </nav>

            {/* ── Hero ── */}
            <section style={S.heroSection}>
                <div style={S.heroGlow} />
                <div style={S.heroGlow2} />
                <div className="hero-inner" style={S.heroInner}>
                    <div style={S.heroContent}>
                        <div style={S.heroEyebrow}>AI Mail Digitisation</div>
                        <h1 className="hero-title" style={S.heroTitle}>
                            Your mailroom,<br />
                            <span style={{ color: "#34d399" }}>digitised.</span>
                        </h1>
                        <p className="hero-sub" style={S.heroSub}>
                            Doario scans physical mail, reads it with AI, extracts key data,
                            and delivers it straight to your team's Outlook inbox — in under 60 seconds.
                        </p>
                        <div className="hero-ctas" style={S.heroCtas}>
                            <a href="/login" style={S.heroBtnPrimary} className="btn-demo">See it in action →</a>
                            <a href="#how" style={S.heroBtnSecondary}>How it works</a>
                        </div>
                        <div className="hero-stats" style={S.heroStats}>
                            {[["< 60s", "Scan to inbox"], ["100%", "Paperless"], ["AI", "Data extraction"]].map(([val, label]) => (
                                <div key={label} style={S.heroStat}>
                                    <div style={S.heroStatVal}>{val}</div>
                                    <div style={S.heroStatLabel}>{label}</div>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Floating card */}
                    <div className="floating-card" style={S.floatingCard}>
                        <div style={S.cardHeader}>
                            <div style={S.cardDot} />
                            <div style={{ ...S.cardDot, background: "#fbbf24" }} />
                            <div style={{ ...S.cardDot, background: "#34d399" }} />
                            <span style={S.cardTitle}>New Mail Item</span>
                        </div>
                        <div style={S.cardBody}>
                            <div style={S.cardRow}><span style={S.cardLabel}>From</span><span style={S.cardVal}>Metro City Housing</span></div>
                            <div style={S.cardRow}><span style={S.cardLabel}>Type</span><span style={{ ...S.cardVal, color: "#34d399" }}>Housing Application</span></div>
                            <div style={S.cardRow}><span style={S.cardLabel}>Applicant</span><span style={S.cardVal}>Maria L. Rodriguez</span></div>
                            <div style={S.cardRow}><span style={S.cardLabel}>DOB</span><span style={S.cardVal}>06/18/1986</span></div>
                            <div style={S.cardRow}><span style={S.cardLabel}>Action</span><span style={{ ...S.cardVal, color: "#fbbf24" }}>Review required</span></div>
                        </div>
                        <div style={S.cardActions}>
                            <div style={S.cardBtn}>✅ Action</div>
                            <div style={{ ...S.cardBtn, background: "rgba(52,211,153,0.12)", color: "#34d399" }}>🔍 Verify</div>
                        </div>
                    </div>
                </div>
            </section>

            {/* ── Features ── */}
            <section id="features" style={S.section}>
                <div style={S.sectionInner}>
                    <div data-id="feat-head" className={visible["feat-head"] ? "visible" : ""} style={S.sectionHead}>
                        <div style={S.eyebrow}>What Doario does</div>
                        <h2 className="section-title" style={S.sectionTitle}>Everything your mailroom needs</h2>
                    </div>
                    <div className="feature-grid" style={S.featureGrid}>
                        {features.map((f, i) => (
                            <div
                                key={f.title}
                                data-id={`feat-${i}`}
                                className={`feature-card stagger-${i + 1} ${visible[`feat-${i}`] ? "visible" : ""}`}
                                style={S.featureCard}
                            >
                                <div style={S.featureIcon}>{f.icon}</div>
                                <div style={S.featureTitle}>{f.title}</div>
                                <div style={S.featureDesc}>{f.desc}</div>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            {/* ── How it works ── */}
            <section id="how" style={{ ...S.section, background: "rgba(255,255,255,0.02)" }}>
                <div style={S.sectionInner}>
                    <div data-id="how-head" className={visible["how-head"] ? "visible" : ""} style={S.sectionHead}>
                        <div style={S.eyebrow}>The process</div>
                        <h2 className="section-title" style={S.sectionTitle}>From scanner to inbox in 4 steps</h2>
                    </div>
                    <div className="steps-row" style={S.stepsRow}>
                        {steps.map((s, i) => (
                            <div
                                key={s.num}
                                data-id={`step-${i}`}
                                className={`stagger-${i + 1} ${visible[`step-${i}`] ? "visible" : ""}`}
                                style={S.stepCard}
                            >
                                <div style={S.stepNum}>{s.num}</div>
                                <div style={S.stepTitle}>{s.title}</div>
                                <div style={S.stepDesc}>{s.desc}</div>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            {/* ── CTA ── */}
            <section style={S.ctaSection}>
                <div style={S.ctaGlow} />
                <div data-id="cta" className={visible["cta"] ? "visible" : ""} style={S.ctaInner}>
                    <h2 className="cta-title" style={S.ctaTitle}>Ready to see it live?</h2>
                    <p style={S.ctaSub}>Watch Doario digitise, read, and deliver a real mail item — end to end.</p>
                    <a href="/login" style={S.heroBtnPrimary} className="btn-demo">Open the demo →</a>
                </div>
            </section>

            {/* ── Footer ── */}
            <footer style={S.footer}>
                <div style={S.footerInner}>
                    <div style={S.logo}>Do<span style={{ color: "#34d399" }}>a</span>rio</div>
                    <div style={S.footerSub}>AI-powered mail digitisation for modern offices.</div>
                </div>
            </footer>
        </div>
    );
}

const S = {
    root: { fontFamily: "'DM Sans', sans-serif", background: "#0a1628", color: "#e8eef4", minHeight: "100vh", overflowX: "hidden" },
    nav: { position: "fixed", top: 0, left: 0, right: 0, zIndex: 100, transition: "all 0.3s ease" },
    navInner: { maxWidth: 1100, margin: "0 auto", display: "flex", alignItems: "center", justifyContent: "space-between", height: 64, padding: "0 24px" },
    logo: { fontFamily: "'Playfair Display', serif", fontSize: 22, fontWeight: 800, color: "#fff", letterSpacing: "-0.5px" },
    navLinks: { display: "flex", alignItems: "center", gap: 32 },
    navLink: { fontSize: 14, color: "rgba(255,255,255,0.6)", textDecoration: "none", fontWeight: 500 },
    navCta: { padding: "9px 22px", background: "#34d399", color: "#0a1628", borderRadius: 8, fontSize: 14, fontWeight: 700, textDecoration: "none", display: "inline-block" },
    heroSection: { position: "relative", overflow: "hidden" },
    heroGlow: { position: "absolute", top: "20%", left: "-10%", width: 500, height: 500, background: "radial-gradient(circle, rgba(52,211,153,0.1) 0%, transparent 70%)", pointerEvents: "none" },
    heroGlow2: { position: "absolute", top: "40%", right: "-5%", width: 400, height: 400, background: "radial-gradient(circle, rgba(99,102,241,0.08) 0%, transparent 70%)", pointerEvents: "none" },
    heroInner: { maxWidth: 1200, margin: "0 auto", padding: "120px 48px 80px", display: "flex", alignItems: "center", justifyContent: "space-between", gap: 48, position: "relative", zIndex: 1 },
    heroContent: { flex: 1, maxWidth: 560 },
    heroEyebrow: { fontSize: 11, fontWeight: 600, color: "#34d399", textTransform: "uppercase", letterSpacing: "2px", marginBottom: 20 },
    heroTitle: { fontFamily: "'Playfair Display', serif", fontSize: 64, fontWeight: 900, color: "#fff", lineHeight: 1.1, marginBottom: 24, letterSpacing: "-1px" },
    heroSub: { fontSize: 16, color: "rgba(255,255,255,0.55)", lineHeight: 1.7, marginBottom: 40, fontWeight: 300 },
    heroCtas: { display: "flex", gap: 14, alignItems: "center", marginBottom: 48, flexWrap: "wrap" },
    heroBtnPrimary: { padding: "13px 28px", background: "#34d399", color: "#0a1628", borderRadius: 10, fontSize: 15, fontWeight: 700, textDecoration: "none", display: "inline-block" },
    heroBtnSecondary: { padding: "13px 24px", background: "transparent", color: "rgba(255,255,255,0.6)", border: "1px solid rgba(255,255,255,0.15)", borderRadius: 10, fontSize: 15, fontWeight: 500, textDecoration: "none", display: "inline-block" },
    heroStats: { display: "flex", gap: 36 },
    heroStat: { display: "flex", flexDirection: "column", gap: 4 },
    heroStatVal: { fontFamily: "'Playfair Display', serif", fontSize: 26, fontWeight: 800, color: "#fff" },
    heroStatLabel: { fontSize: 11, color: "rgba(255,255,255,0.4)", fontWeight: 500, textTransform: "uppercase", letterSpacing: "1px" },
    floatingCard: { width: 290, background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.1)", borderRadius: 16, overflow: "hidden", backdropFilter: "blur(20px)", boxShadow: "0 24px 60px rgba(0,0,0,0.4)", flexShrink: 0 },
    cardHeader: { display: "flex", alignItems: "center", gap: 6, padding: "12px 16px", borderBottom: "1px solid rgba(255,255,255,0.06)", background: "rgba(255,255,255,0.03)" },
    cardDot: { width: 10, height: 10, borderRadius: "50%", background: "#ef4444" },
    cardTitle: { fontSize: 10, color: "rgba(255,255,255,0.4)", marginLeft: 6, fontWeight: 600, textTransform: "uppercase", letterSpacing: "1px" },
    cardBody: { padding: "14px 16px", display: "flex", flexDirection: "column", gap: 9 },
    cardRow: { display: "flex", justifyContent: "space-between", alignItems: "center" },
    cardLabel: { fontSize: 10, color: "rgba(255,255,255,0.35)", fontWeight: 600, textTransform: "uppercase", letterSpacing: "0.8px" },
    cardVal: { fontSize: 12, color: "rgba(255,255,255,0.85)", fontWeight: 500, textAlign: "right", maxWidth: 150 },
    cardActions: { display: "flex", gap: 8, padding: "0 16px 14px" },
    cardBtn: { flex: 1, padding: "7px 0", background: "rgba(255,255,255,0.06)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 8, fontSize: 11, fontWeight: 600, color: "rgba(255,255,255,0.7)", textAlign: "center", cursor: "pointer" },
    section: { padding: "80px 24px" },
    sectionInner: { maxWidth: 1100, margin: "0 auto" },
    sectionHead: { textAlign: "center", marginBottom: 48 },
    eyebrow: { fontSize: 11, fontWeight: 600, color: "#34d399", textTransform: "uppercase", letterSpacing: "2px", marginBottom: 14 },
    sectionTitle: { fontFamily: "'Playfair Display', serif", fontSize: 36, fontWeight: 800, color: "#fff", letterSpacing: "-0.5px" },
    featureGrid: { display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 20 },
    featureCard: { padding: "24px 20px", background: "rgba(255,255,255,0.02)", border: "1px solid rgba(255,255,255,0.07)", borderRadius: 14, cursor: "default" },
    featureIcon: { fontSize: 26, marginBottom: 14 },
    featureTitle: { fontSize: 15, fontWeight: 700, color: "#fff", marginBottom: 8 },
    featureDesc: { fontSize: 13, color: "rgba(255,255,255,0.45)", lineHeight: 1.7, fontWeight: 300 },
    stepsRow: { display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 20 },
    stepCard: { padding: "24px 20px", borderTop: "2px solid rgba(52,211,153,0.3)" },
    stepNum: { fontFamily: "'Playfair Display', serif", fontSize: 40, fontWeight: 900, color: "rgba(52,211,153,0.2)", lineHeight: 1, marginBottom: 12 },
    stepTitle: { fontSize: 18, fontWeight: 700, color: "#fff", marginBottom: 8 },
    stepDesc: { fontSize: 13, color: "rgba(255,255,255,0.45)", lineHeight: 1.6, fontWeight: 300 },
    ctaSection: { padding: "100px 24px", textAlign: "center", position: "relative", overflow: "hidden" },
    ctaGlow: { position: "absolute", top: "50%", left: "50%", transform: "translate(-50%, -50%)", width: 600, height: 400, background: "radial-gradient(ellipse, rgba(52,211,153,0.07) 0%, transparent 70%)", pointerEvents: "none" },
    ctaInner: { position: "relative", maxWidth: 560, margin: "0 auto" },
    ctaTitle: { fontFamily: "'Playfair Display', serif", fontSize: 44, fontWeight: 900, color: "#fff", marginBottom: 16, letterSpacing: "-0.5px" },
    ctaSub: { fontSize: 16, color: "rgba(255,255,255,0.45)", marginBottom: 36, lineHeight: 1.6, fontWeight: 300 },
    footer: { borderTop: "1px solid rgba(255,255,255,0.06)", padding: "32px 24px" },
    footerInner: { maxWidth: 1100, margin: "0 auto", display: "flex", flexDirection: "column", gap: 6 },
    footerSub: { fontSize: 13, color: "rgba(255,255,255,0.25)" },
};
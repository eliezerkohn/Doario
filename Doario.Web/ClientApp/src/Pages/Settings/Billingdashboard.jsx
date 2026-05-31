// BillingDashboard.jsx — Billing period summary and promo code redemption

import React, { useEffect, useState } from 'react';

export default function BillingDashboard() {
    const [summary, setSummary] = useState(null);
    const [loading, setLoading] = useState(true);
    const [promoCode, setPromoCode] = useState('');
    const [promoApplying, setPromoApplying] = useState(false);
    const [promoResult, setPromoResult] = useState(null);
    const [portalLoading, setPortalLoading] = useState(false);

    useEffect(() => { loadAll(); }, []);

    async function loadAll() {
        setLoading(true);
        try {
            const sumRes = await fetch('/api/billing/summary');
            if (sumRes.ok) setSummary(await sumRes.json());
        } catch { }
        setLoading(false);
    }

    async function applyPromo() {
        if (!promoCode.trim()) return;
        setPromoApplying(true);
        setPromoResult(null);
        try {
            const res = await fetch('/api/billing/apply-promo', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ code: promoCode.trim() })
            });
            const data = await res.json();
            if (res.ok) {
                setPromoResult({ success: true, message: data.message });
                setPromoCode('');
                await loadAll();
            } else {
                setPromoResult({ success: false, message: data.error });
            }
        } catch {
            setPromoResult({ success: false, message: 'Failed to apply promo code.' });
        }
        setPromoApplying(false);
    }

    async function openBillingPortal() {
        setPortalLoading(true);
        try {
            const res = await fetch('/api/billing/customer-portal', { method: 'POST' });
            const data = await res.json();
            if (res.ok && data.url) {
                window.location.href = data.url;
            } else {
                alert(data.error || 'Failed to open billing portal.');
            }
        } catch {
            alert('Failed to open billing portal.');
        }
        setPortalLoading(false);
    }

    if (loading) return <div style={S.loading}>Loading billing data...</div>;

    if (!summary?.hasSubscription) {
        return (
            <div style={S.empty}>
                <div style={S.emptyIcon}>📊</div>
                <div style={S.emptyTitle}>No active subscription</div>
                <div style={S.emptySub}>Set up a subscription to see billing information here.</div>
            </div>
        );
    }

    const periodLabel = summary
        ? new Date(summary.periodStart).toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
        : '';

    const includedTotal = summary.includedDocuments + summary.promoFreeDocCount;
    const overDocs = Math.max(0, summary.totalDocsThisPeriod - includedTotal);
    const overageCharge = overDocs * summary.effectiveDocPrice;

    const hasBaseDiscount = summary.negotiatedDiscountPercent > 0 || summary.baseDiscountPercent > 0;
    const effectiveBase = summary.effectiveMonthlyPrice ?? summary.monthlyPrice;

    return (
        <div style={S.page}>

            {/* Header */}
            <div style={S.sectionTitle}>Billing Overview</div>
            <div style={S.sectionSub}>{periodLabel}</div>

            {/* Summary cards */}
            <div style={S.cards}>
                <StatCard label="Plan" value={summary.planName} />
                <StatCard label="Docs This Month" value={summary.totalDocsThisPeriod.toLocaleString()} />
                <StatCard label="Included" value={includedTotal.toLocaleString()} />
                <StatCard label="Over Limit" value={overDocs.toLocaleString()} accent={overDocs > 0} />
            </div>

            {/* Pricing breakdown */}
            <div style={S.card}>
                <div style={S.cardTitle}>Estimated Charge</div>

                {/* Monthly base */}
                <div style={S.row}>
                    <span style={S.rowLabel}>Monthly base ({summary.planName})</span>
                    <span style={S.rowValue}>
                        {hasBaseDiscount ? (
                            <>
                                <span style={{ textDecoration: 'line-through', color: '#7a9ab0', marginRight: 6 }}>
                                    {fmt(summary.monthlyPrice)}
                                </span>
                                <span style={{ color: '#0d9488' }}>{fmt(effectiveBase)}</span>
                            </>
                        ) : fmt(summary.monthlyPrice)}
                    </span>
                </div>

                {/* Negotiated discount */}
                {summary.negotiatedDiscountPercent > 0 && (
                    <div style={S.row}>
                        <span style={{ ...S.rowLabel, color: '#0d9488' }}>Negotiated discount</span>
                        <span style={{ ...S.rowValue, color: '#0d9488' }}>-{summary.negotiatedDiscountPercent}%</span>
                    </div>
                )}

                {/* Promo base discount */}
                {summary.baseDiscountPercent > 0 && (
                    <div style={S.row}>
                        <span style={{ ...S.rowLabel, color: '#0d9488' }}>
                            Promo discount
                            {summary.activePromoCode && (
                                <span style={{ ...S.promoBadge, marginLeft: 8 }}>{summary.activePromoCode}</span>
                            )}
                        </span>
                        <span style={{ ...S.rowValue, color: '#0d9488' }}>-{summary.baseDiscountPercent}%</span>
                    </div>
                )}

                <div style={S.row}>
                    <span style={S.rowLabel}>Included documents</span>
                    <span style={S.rowValue}>{summary.includedDocuments.toLocaleString()}</span>
                </div>

                {summary.promoFreeDocCount > 0 && (
                    <div style={S.row}>
                        <span style={{ ...S.rowLabel, color: '#0d9488' }}>
                            Promo bonus docs
                            {summary.activePromoCode && (
                                <span style={{ ...S.promoBadge, marginLeft: 8 }}>{summary.activePromoCode}</span>
                            )}
                        </span>
                        <span style={{ ...S.rowValue, color: '#0d9488' }}>
                            +{summary.promoFreeDocCount.toLocaleString()}
                        </span>
                    </div>
                )}

                <div style={S.row}>
                    <span style={S.rowLabel}>Total included this month</span>
                    <span style={S.rowValue}>{includedTotal.toLocaleString()}</span>
                </div>

                <div style={S.divider} />

                <div style={S.row}>
                    <span style={S.rowLabel}>Documents processed</span>
                    <span style={S.rowValue}>{summary.totalDocsThisPeriod.toLocaleString()}</span>
                </div>

                <div style={S.row}>
                    <span style={{ ...S.rowLabel, color: overDocs > 0 ? '#e53e3e' : '#4a6478' }}>
                        Over limit
                    </span>
                    <span style={{ ...S.rowValue, color: overDocs > 0 ? '#e53e3e' : '#0d9488' }}>
                        {overDocs > 0 ? `${overDocs.toLocaleString()} docs` : 'Within limit ✓'}
                    </span>
                </div>

                {overDocs > 0 && (
                    <div style={S.row}>
                        <span style={S.rowLabel}>
                            Overage ({overDocs.toLocaleString()} × {fmt(summary.effectiveDocPrice)})
                            {summary.activePromoCode && summary.effectiveDocPrice < (summary.effectiveDocPrice + 0.01) && (
                                <span style={{ ...S.promoBadge, marginLeft: 8 }}>{summary.activePromoCode}</span>
                            )}
                        </span>
                        <span style={{ ...S.rowValue, color: '#e53e3e' }}>{fmt(overageCharge)}</span>
                    </div>
                )}

                <div style={S.divider} />

                <div style={{ ...S.row, fontWeight: 700 }}>
                    <span style={S.rowLabel}>Estimated total</span>
                    <span style={{ ...S.rowValue, fontSize: 18, color: '#0f2d4a' }}>
                        {fmt(summary.estimatedCharge)}
                    </span>
                </div>
            </div>

            {/* Payment method management */}
            <div style={S.card}>
                <div style={S.cardTitle}>Payment Method</div>
                <div style={{ fontSize: 12, color: '#7a9ab0', marginBottom: 14 }}>
                    Manage your payment method, view invoices, and update billing details.
                </div>
                <button
                    style={{
                        ...S.portalBtn,
                        opacity: portalLoading ? 0.6 : 1,
                        cursor: portalLoading ? 'not-allowed' : 'pointer',
                    }}
                    onClick={openBillingPortal}
                    disabled={portalLoading}
                >
                    {portalLoading ? 'Opening...' : '💳 Manage Payment Method & Invoices'}
                </button>
            </div>

            {/* Active promo display */}
            {summary.activePromoCode && (
                <div style={S.activePromoCard}>
                    <span style={S.promoBadge}>{summary.activePromoCode}</span>
                    <span style={S.activePromoText}>
                        {summary.activePromoDescription || 'Promo applied to your account'}
                    </span>
                </div>
            )}

            {/* Promo code input */}
            <div style={S.card}>
                <div style={S.cardTitle}>Have a Promo Code?</div>
                <div style={S.promoInputRow}>
                    <input
                        style={S.promoInput}
                        placeholder="Enter promo code e.g. WELCOME50"
                        value={promoCode}
                        onChange={e => setPromoCode(e.target.value.toUpperCase())}
                        onKeyDown={e => e.key === 'Enter' && applyPromo()}
                    />
                    <button
                        style={{
                            ...S.applyBtn,
                            opacity: promoApplying || !promoCode.trim() ? 0.6 : 1,
                            cursor: promoApplying || !promoCode.trim() ? 'not-allowed' : 'pointer'
                        }}
                        onClick={applyPromo}
                        disabled={promoApplying || !promoCode.trim()}
                    >
                        {promoApplying ? 'Applying...' : 'Apply'}
                    </button>
                </div>
                {promoResult && (
                    <div style={{
                        ...S.promoMsg,
                        background: promoResult.success ? '#d1fae5' : '#fee2e2',
                        color: promoResult.success ? '#065f46' : '#991b1b',
                        border: `1px solid ${promoResult.success ? '#6ee7b7' : '#fca5a5'}`
                    }}>
                        {promoResult.success ? '✅' : '❌'} {promoResult.message}
                    </div>
                )}
            </div>

        </div>
    );
}

function StatCard({ label, value, accent }) {
    return (
        <div style={{ ...S.statCard, borderColor: accent ? '#e53e3e' : '#e2eaef' }}>
            <div style={{ ...S.statValue, color: accent ? '#e53e3e' : '#0f2d4a' }}>{value}</div>
            <div style={S.statLabel}>{label}</div>
        </div>
    );
}

function fmt(amount) {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount ?? 0);
}

const S = {
    page: { display: 'flex', flexDirection: 'column', gap: 20 },
    loading: { color: '#7a9ab0', fontSize: 14, padding: 20 },
    sectionTitle: { fontSize: 16, fontWeight: 800, color: '#0f2d4a' },
    sectionSub: { fontSize: 12, color: '#7a9ab0', marginTop: -16 },
    cards: { display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 },
    statCard: { background: '#fff', border: '1.5px solid', borderRadius: 10, padding: '16px 18px', display: 'flex', flexDirection: 'column', gap: 4 },
    statValue: { fontSize: 22, fontWeight: 800 },
    statLabel: { fontSize: 11, color: '#7a9ab0', fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 },
    card: { background: '#fff', border: '1px solid #e2eaef', borderRadius: 10, padding: '20px 24px', display: 'flex', flexDirection: 'column', gap: 10 },
    cardTitle: { fontSize: 13, fontWeight: 700, color: '#0f2d4a', marginBottom: 4 },
    row: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: 13 },
    rowLabel: { color: '#4a6478' },
    rowValue: { color: '#0f2d4a', fontWeight: 600, display: 'flex', alignItems: 'center', gap: 8 },
    divider: { borderTop: '1px solid #e2eaef', margin: '4px 0' },
    promoBadge: { background: '#d1fae5', color: '#065f46', fontSize: 11, fontWeight: 700, padding: '2px 8px', borderRadius: 20 },
    activePromoCard: { background: '#f0fdf4', border: '1px solid #6ee7b7', borderRadius: 10, padding: '12px 18px', display: 'flex', alignItems: 'center', gap: 12 },
    activePromoText: { fontSize: 13, color: '#065f46' },
    promoInputRow: { display: 'flex', gap: 10 },
    promoInput: { flex: 1, border: '1px solid #d1dde6', borderRadius: 7, padding: '9px 12px', fontSize: 13, color: '#1a2e3b', fontFamily: 'inherit', outline: 'none', letterSpacing: 1 },
    applyBtn: { fontSize: 13, fontWeight: 700, color: '#fff', background: '#0d9488', border: 'none', borderRadius: 7, padding: '9px 20px', fontFamily: 'inherit', transition: 'opacity 0.15s' },
    portalBtn: { fontSize: 13, fontWeight: 600, color: '#0f2d4a', background: '#f7f9fb', border: '1px solid #d1dde6', borderRadius: 8, padding: '10px 20px', fontFamily: 'inherit', transition: 'opacity 0.15s', alignSelf: 'flex-start' },
    promoMsg: { fontSize: 12, fontWeight: 600, padding: '10px 14px', borderRadius: 8, marginTop: 4 },
    empty: { display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10, padding: '60px 0', color: '#7a9ab0' },
    emptyIcon: { fontSize: 36 },
    emptyTitle: { fontSize: 15, fontWeight: 700, color: '#0f2d4a' },
    emptySub: { fontSize: 13 },
};
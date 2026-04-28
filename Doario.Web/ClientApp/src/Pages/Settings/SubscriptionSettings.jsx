// SubscriptionSettings.jsx

import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { S, Shared } from './SettingsShared';

export default function SubscriptionSettings() {

    const [plans, setPlans] = useState([]);
    const [subscription, setSubscription] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => { load(); }, []);

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            const [plansRes, subRes] = await Promise.all([
                axios.get('/api/settings/plans'),
                axios.get('/api/settings/subscription'),
            ]);
            setPlans(plansRes.data);
            setSubscription(subRes.data);
        } catch {
            setError('Failed to load subscription details.');
        } finally {
            setLoading(false);
        }
    };

    // Usage percentage — cap at 100 so bar never overflows
    const usagePct = subscription
        ? Math.min((subscription.documentsUsed / subscription.includedDocuments) * 100, 100)
        : 0;

    const usageColor = usagePct >= 90
        ? '#e53e3e'
        : usagePct >= 70
            ? '#d97706'
            : '#0d9488';

    if (loading) return <Shared.Loading />;
    if (error) return <div style={{ ...S.msg, ...S.msgError }}>❌ {error}</div>;

    return (
        <div>
            <div style={S.sectionTitle}>Subscription</div>
            <div style={S.sectionSub}>Your current plan and usage</div>

            {/* ── Current plan + usage ── */}
            {subscription ? (
                <div style={S.card}>
                    <div style={cardTitle}>Current Plan</div>

                    <div style={currentPlanRow}>
                        <div>
                            <div style={planName}>{subscription.planName}</div>
                            <div style={planPrice}>
                                ${subscription.monthlyPrice.toFixed(2)} / month
                            </div>
                            {subscription.discountPercent > 0 && (
                                <div style={discountBadge}>
                                    {subscription.discountPercent}% discount applied
                                </div>
                            )}
                        </div>
                        <span style={activeBadge}>Active</span>
                    </div>

                    <Shared.Divider />

                    {/* Usage bar */}
                    <div style={usageLabel}>Documents used this month</div>
                    <div style={usageRow}>
                        <div style={usageBar}>
                            <div style={{
                                ...usageFill,
                                width: `${usagePct}%`,
                                background: usageColor,
                            }} />
                        </div>
                        <div style={{ ...usageCount, color: usageColor }}>
                            {subscription.documentsUsed ?? 0} / {subscription.includedDocuments}
                        </div>
                    </div>
                    <div style={usageExtra}>
                        Extra documents billed at ${subscription.extraDocumentPrice.toFixed(2)} each
                    </div>

                    <Shared.Divider />

                    {/* Billing coming soon */}
                    <div style={billingNote}>
                        <div style={billingIcon}>💳</div>
                        <div>
                            <div style={billingTitle}>Billing management coming soon</div>
                            <div style={billingSub}>
                                You will be able to manage your subscription,
                                view invoices, and update payment details here on Day 18.
                            </div>
                        </div>
                    </div>
                </div>
            ) : (
                <div style={S.card}>
                    <div style={{ fontSize: 13, color: '#7a9ab0' }}>
                        No active subscription found.
                    </div>
                </div>
            )}

            {/* ── Plan comparison ── */}
            {plans.length > 0 && (
                <div style={S.card}>
                    <div style={cardTitle}>Available Plans</div>
                    <div style={{ ...cardSub, marginBottom: 20 }}>
                        Plan changes available when billing is enabled.
                    </div>

                    <div style={plansRow}>
                        {plans.map(plan => {
                            const isCurrent = subscription?.planName === plan.name;
                            const color = isCurrent ? '#0d9488' : '#6b8499';
                            return (
                                <div
                                    key={plan.subscriptionPlanId}
                                    style={{
                                        ...planCard,
                                        borderColor: isCurrent ? '#0d9488' : '#e2eaef',
                                        background: isCurrent
                                            ? 'rgba(13,148,136,0.04)'
                                            : '#fff',
                                    }}
                                >
                                    {isCurrent && (
                                        <div style={currentTag}>Current</div>
                                    )}
                                    <div style={{ ...planCardName, color }}>
                                        {plan.name}
                                    </div>
                                    <div style={planCardPrice}>
                                        ${plan.monthlyPrice.toFixed(2)}
                                        <span style={planCardPer}>/mo</span>
                                    </div>
                                    <div style={planCardDocs}>
                                        {plan.includedDocuments} documents
                                    </div>
                                    <div style={planCardExtra}>
                                        +${plan.extraDocumentPrice.toFixed(2)} per extra
                                    </div>
                                    {plan.description && (
                                        <div style={planCardDesc}>
                                            {plan.description}
                                        </div>
                                    )}
                                    <button
                                        style={{
                                            ...S.btnSecondary,
                                            width: '100%',
                                            marginTop: 16,
                                            opacity: 0.4,
                                            cursor: 'not-allowed',
                                            fontSize: 12,
                                            padding: '7px 0',
                                        }}
                                        disabled
                                    >
                                        {isCurrent ? 'Current Plan' : 'Switch Plan'}
                                    </button>
                                </div>
                            );
                        })}
                    </div>

                    <div style={enterpriseNote}>
                        Need more than 600 documents?{' '}
                        <span style={{ color: '#0d9488', fontWeight: 600 }}>
                            Contact us for Enterprise pricing
                        </span>
                    </div>
                </div>
            )}
        </div>
    );
}

// ── Local styles ─────────────────────────────────────────────────
const cardTitle = {
    fontSize: 14, fontWeight: 700, color: '#0f2d4a', marginBottom: 4,
};
const cardSub = {
    fontSize: 12, color: '#7a9ab0', lineHeight: 1.6,
};
const currentPlanRow = {
    display: 'flex', alignItems: 'flex-start',
    justifyContent: 'space-between', marginBottom: 16,
};
const planName = {
    fontSize: 18, fontWeight: 800, color: '#0f2d4a', marginBottom: 2,
};
const planPrice = {
    fontSize: 13, color: '#7a9ab0',
};
const discountBadge = {
    display: 'inline-block', marginTop: 6,
    fontSize: 11, fontWeight: 600,
    background: 'rgba(13,148,136,0.1)', color: '#0d9488',
    padding: '3px 10px', borderRadius: 20,
};
const activeBadge = {
    fontSize: 11, fontWeight: 700,
    background: 'rgba(13,148,136,0.1)', color: '#0d9488',
    padding: '4px 12px', borderRadius: 20,
};
const usageLabel = {
    fontSize: 11, fontWeight: 600, color: '#4a6478',
    textTransform: 'uppercase', letterSpacing: '1px', marginBottom: 8,
};
const usageRow = {
    display: 'flex', alignItems: 'center', gap: 12, marginBottom: 6,
};
const usageBar = {
    flex: 1, height: 8, background: '#e2eaef',
    borderRadius: 20, overflow: 'hidden',
};
const usageFill = {
    height: '100%', borderRadius: 20, transition: 'width 0.4s ease',
};
const usageCount = {
    fontSize: 12, fontWeight: 700,
    minWidth: 70, textAlign: 'right',
};
const usageExtra = {
    fontSize: 11, color: '#7a9ab0',
};
const billingNote = {
    display: 'flex', alignItems: 'flex-start', gap: 14,
};
const billingIcon = {
    fontSize: 24, flexShrink: 0, marginTop: 2,
};
const billingTitle = {
    fontSize: 13, fontWeight: 700, color: '#0f2d4a', marginBottom: 4,
};
const billingSub = {
    fontSize: 12, color: '#7a9ab0', lineHeight: 1.6,
};
const plansRow = {
    display: 'flex', gap: 12,
};
const planCard = {
    flex: 1, border: '2px solid', borderRadius: 10,
    padding: '16px 14px', position: 'relative', textAlign: 'center',
};
const currentTag = {
    position: 'absolute', top: -10, left: '50%',
    transform: 'translateX(-50%)',
    fontSize: 9, fontWeight: 700, color: '#fff',
    background: '#0d9488',
    padding: '2px 10px', borderRadius: 20,
    textTransform: 'uppercase', letterSpacing: '1px',
};
const planCardName = {
    fontSize: 13, fontWeight: 800,
    textTransform: 'uppercase', letterSpacing: '1px', marginBottom: 8,
};
const planCardPrice = {
    fontSize: 22, fontWeight: 800, color: '#0f2d4a', marginBottom: 4,
};
const planCardPer = {
    fontSize: 12, fontWeight: 400, color: '#7a9ab0',
};
const planCardDocs = {
    fontSize: 12, color: '#4a6478', fontWeight: 600, marginBottom: 2,
};
const planCardExtra = {
    fontSize: 11, color: '#7a9ab0', marginBottom: 4,
};
const planCardDesc = {
    fontSize: 11, color: '#7a9ab0',
    lineHeight: 1.5, marginTop: 6,
};
const enterpriseNote = {
    fontSize: 12, color: '#7a9ab0',
    textAlign: 'center', marginTop: 16,
};
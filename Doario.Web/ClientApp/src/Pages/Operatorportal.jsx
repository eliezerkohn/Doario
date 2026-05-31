// OperatorPortal.jsx — Doario operator dashboard

import React, { useEffect, useState } from 'react';
import axios from 'axios';

const TABS = ['Tenants', 'Plans', 'Promos'];

export default function OperatorPortal() {
    const [tab, setTab] = useState('Tenants');
    const [tenants, setTenants] = useState([]);
    const [stats, setStats] = useState(null);
    const [plans, setPlans] = useState([]);
    const [promos, setPromos] = useState([]);
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState('');

    // Tenant detail
    const [selectedTenant, setSelectedTenant] = useState(null);
    const [tenantDetail, setTenantDetail] = useState(null);
    const [detailLoading, setDetailLoading] = useState(false);
    const [assigningPlan, setAssigningPlan] = useState(false);
    const [assignMsg, setAssignMsg] = useState(null);
    const [discountInput, setDiscountInput] = useState('');
    const [savingDiscount, setSavingDiscount] = useState(false);
    const [discountMsg, setDiscountMsg] = useState(null);
    const [togglingHipaa, setTogglingHipaa] = useState(false);

    // Create tenant
    const [showCreate, setShowCreate] = useState(false);
    const [tenantForm, setTenantForm] = useState({ name: '', domain: '', mailboxAddress: '' });
    const [creating, setCreating] = useState(false);
    const [createMsg, setCreateMsg] = useState(null);

    // Create plan
    const [showCreatePlan, setShowCreatePlan] = useState(false);
    const [planForm, setPlanForm] = useState({ name: '', description: '', monthlyPrice: '', includedDocuments: '', extraDocumentPrice: '', isPublic: true, sortOrder: 100 });
    const [creatingPlan, setCreatingPlan] = useState(false);
    const [planMsg, setPlanMsg] = useState(null);
    const [syncingPlan, setSyncingPlan] = useState(null);

    // Create promo
    const [showCreatePromo, setShowCreatePromo] = useState(false);
    const [promoForm, setPromoForm] = useState({ code: '', description: '', baseDiscountPercent: 0, discountPercent: 0, flatDiscountPerDoc: 0, freeDocCount: 0, maxRedemptions: 0 });
    const [creatingPromo, setCreatingPromo] = useState(false);
    const [promoMsg, setPromoMsg] = useState(null);

    // Assign promo
    const [assigningPromo, setAssigningPromo] = useState(null);
    const [selectedTenantIds, setSelectedTenantIds] = useState([]);
    const [sendEmail, setSendEmail] = useState(true);
    const [assignPromoMsg, setAssignPromoMsg] = useState(null);
    const [assigningPromoLoading, setAssigningPromoLoading] = useState(false);

    useEffect(() => { loadAll(); }, []);

    async function loadAll() {
        setLoading(true);
        try {
            const [tenantsRes, statsRes, plansRes, promosRes] = await Promise.all([
                axios.get('/api/operator/tenants'),
                axios.get('/api/operator/stats'),
                axios.get('/api/operator/plans'),
                axios.get('/api/operator/promos'),
            ]);
            setTenants(tenantsRes.data);
            setStats(statsRes.data);
            setPlans(plansRes.data);
            setPromos(promosRes.data);
        } catch { }
        setLoading(false);
    }

    async function loadTenantDetail(tenant) {
        setSelectedTenant(tenant);
        setDetailLoading(true);
        setAssignMsg(null);
        setDiscountMsg(null);
        try {
            const res = await axios.get(`/api/operator/tenants/${tenant.tenantId}`);
            setTenantDetail(res.data);
            setDiscountInput(res.data.subscription?.discountPercent?.toString() ?? '0');
        } catch { }
        setDetailLoading(false);
    }

    async function assignPlan(planId) {
        if (!selectedTenant) return;
        setAssigningPlan(true);
        setAssignMsg(null);
        try {
            const res = await axios.post(`/api/operator/tenants/${selectedTenant.tenantId}/assign-plan`, { subscriptionPlanId: planId });
            setAssignMsg({ success: true, message: res.data.message });
            await loadTenantDetail(selectedTenant);
            await loadAll();
        } catch (e) {
            setAssignMsg({ success: false, message: e.response?.data?.error || 'Failed to assign plan.' });
        }
        setAssigningPlan(false);
    }

    async function saveDiscount() {
        setSavingDiscount(true);
        setDiscountMsg(null);
        try {
            const res = await axios.put(`/api/operator/tenants/${selectedTenant.tenantId}/negotiated-discount`, {
                discountPercent: parseFloat(discountInput) || 0
            });
            setDiscountMsg({ success: true, message: res.data.message });
            await loadTenantDetail(selectedTenant);
        } catch (e) {
            setDiscountMsg({ success: false, message: e.response?.data?.error || 'Failed to save discount.' });
        }
        setSavingDiscount(false);
    }

    async function toggleHipaa() {
        setTogglingHipaa(true);
        try {
            await axios.put(`/api/operator/tenants/${selectedTenant.tenantId}/hipaa`, {
                enabled: !tenantDetail.isHipaaEnabled
            });
            await loadTenantDetail(selectedTenant);
        } catch { }
        setTogglingHipaa(false);
    }

    async function syncPlanToStripe(planId) {
        setSyncingPlan(planId);
        setPlanMsg(null);
        try {
            const res = await axios.post(`/api/operator/plans/${planId}/sync-stripe`);
            setPlanMsg({ success: true, message: res.data.message });
            await loadAll();
        } catch (e) {
            setPlanMsg({ success: false, message: e.response?.data?.error || 'Failed to sync to Stripe.' });
        }
        setSyncingPlan(null);
    }

    async function createTenant() {
        if (!tenantForm.name || !tenantForm.domain || !tenantForm.mailboxAddress) {
            setCreateMsg({ success: false, message: 'All fields are required.' });
            return;
        }
        setCreating(true);
        setCreateMsg(null);
        try {
            await axios.post('/api/operator/tenants', tenantForm);
            setCreateMsg({ success: true, message: 'Tenant created successfully.' });
            setTenantForm({ name: '', domain: '', mailboxAddress: '' });
            setShowCreate(false);
            await loadAll();
        } catch (e) {
            setCreateMsg({ success: false, message: e.response?.data?.error || 'Failed to create tenant.' });
        }
        setCreating(false);
    }

    async function createPlan() {
        if (!planForm.name || !planForm.monthlyPrice) {
            setPlanMsg({ success: false, message: 'Name and price are required.' });
            return;
        }
        setCreatingPlan(true);
        setPlanMsg(null);
        try {
            const res = await axios.post('/api/operator/plans', {
                ...planForm,
                monthlyPrice: parseFloat(planForm.monthlyPrice),
                includedDocuments: parseInt(planForm.includedDocuments) || 0,
                extraDocumentPrice: parseFloat(planForm.extraDocumentPrice) || 0,
            });
            setPlanMsg({ success: true, message: res.data.message });
            setPlanForm({ name: '', description: '', monthlyPrice: '', includedDocuments: '', extraDocumentPrice: '', isPublic: true, sortOrder: 100 });
            setShowCreatePlan(false);
            await loadAll();
        } catch (e) {
            setPlanMsg({ success: false, message: e.response?.data?.error || 'Failed to create plan.' });
        }
        setCreatingPlan(false);
    }

    async function createPromo() {
        if (!promoForm.code) {
            setPromoMsg({ success: false, message: 'Code is required.' });
            return;
        }
        setCreatingPromo(true);
        setPromoMsg(null);
        try {
            await axios.post('/api/operator/promos', promoForm);
            setPromoMsg({ success: true, message: 'Promo code created.' });
            setPromoForm({ code: '', description: '', baseDiscountPercent: 0, discountPercent: 0, flatDiscountPerDoc: 0, freeDocCount: 0, maxRedemptions: 0 });
            setShowCreatePromo(false);
            await loadAll();
        } catch (e) {
            setPromoMsg({ success: false, message: e.response?.data?.error || 'Failed to create promo.' });
        }
        setCreatingPromo(false);
    }

    async function doAssignPromo() {
        if (!selectedTenantIds.length) {
            setAssignPromoMsg({ success: false, message: 'Select at least one tenant.' });
            return;
        }
        setAssigningPromoLoading(true);
        setAssignPromoMsg(null);
        try {
            const res = await axios.post(`/api/operator/promos/${assigningPromo}/assign`, { tenantIds: selectedTenantIds, sendEmail });
            setAssignPromoMsg({ success: true, message: res.data.message });
            setSelectedTenantIds([]);
            await loadAll();
        } catch (e) {
            setAssignPromoMsg({ success: false, message: e.response?.data?.error || 'Failed to assign promo.' });
        }
        setAssigningPromoLoading(false);
    }

    const filtered = tenants.filter(t => {
        if (!search) return true;
        const q = search.toLowerCase();
        return t.name?.toLowerCase().includes(q) || t.domain?.toLowerCase().includes(q) || t.mailboxAddress?.toLowerCase().includes(q);
    });

    return (
        <div style={S.page}>

            <div style={S.header}>
                <div>
                    <div style={S.headerTitle}>Operator Portal</div>
                    <div style={S.headerSub}>Manage all Doario tenants, plans and promos</div>
                </div>
            </div>

            {stats && (
                <div style={S.statsBar}>
                    <StatPill label="Active Tenants" value={stats.totalTenants} />
                    <StatPill label="Total Documents" value={stats.totalDocuments?.toLocaleString()} />
                    <StatPill label="Docs This Month" value={stats.docsThisMonth?.toLocaleString()} />
                    <StatPill label="Failed Payments" value={stats.failedPayments} accent={stats.failedPayments > 0} />
                </div>
            )}

            <div style={S.tabs}>
                {TABS.map(t => (
                    <button key={t} style={{ ...S.tabBtn, ...(tab === t && !selectedTenant ? S.tabBtnActive : {}) }}
                        onClick={() => { setTab(t); setSelectedTenant(null); }}>
                        {t}
                    </button>
                ))}
            </div>

            {/* ── TENANTS TAB ── */}
            {tab === 'Tenants' && !selectedTenant && (
                <div style={S.section}>
                    <div style={S.sectionHeader}>
                        <input style={S.search} placeholder="Search tenants..." value={search} onChange={e => setSearch(e.target.value)} />
                        <button style={S.primaryBtn} onClick={() => setShowCreate(!showCreate)}>
                            {showCreate ? 'Cancel' : '+ New Tenant'}
                        </button>
                    </div>

                    {showCreate && (
                        <div style={S.formCard}>
                            <div style={S.cardTitle}>Create New Tenant</div>
                            <label style={S.label}>Company Name</label>
                            <input style={S.input} placeholder="Acme Corp" value={tenantForm.name} onChange={e => setTenantForm(f => ({ ...f, name: e.target.value }))} />
                            <label style={S.label}>Domain</label>
                            <input style={S.input} placeholder="acmecorp.com" value={tenantForm.domain} onChange={e => setTenantForm(f => ({ ...f, domain: e.target.value }))} />
                            <label style={S.label}>Admin Email</label>
                            <input style={S.input} placeholder="admin@acmecorp.com" value={tenantForm.mailboxAddress} onChange={e => setTenantForm(f => ({ ...f, mailboxAddress: e.target.value }))} />
                            {createMsg && <Msg data={createMsg} />}
                            <div style={{ display: 'flex', gap: 8 }}>
                                <button style={{ ...S.primaryBtn, opacity: creating ? 0.6 : 1 }} onClick={createTenant} disabled={creating}>
                                    {creating ? 'Creating...' : 'Create Tenant'}
                                </button>
                                <button style={S.secondaryBtn} onClick={() => { setShowCreate(false); setCreateMsg(null); }}>Cancel</button>
                            </div>
                        </div>
                    )}

                    {loading ? <div style={S.loading}>Loading...</div> : (
                        <div style={S.table}>
                            <div style={S.tableHead}>
                                <div style={{ ...S.th, flex: 2 }}>Tenant</div>
                                <div style={S.th}>Plan</div>
                                <div style={S.th}>Monthly</div>
                                <div style={S.th}>Status</div>
                                <div style={S.th}>Last Payment</div>
                                <div style={S.th}></div>
                            </div>
                            {filtered.map(t => (
                                <div key={t.tenantId} style={S.tableRow}>
                                    <div style={{ ...S.td, flex: 2 }}>
                                        <div style={S.tenantName}>{t.name}</div>
                                        <div style={S.tenantDomain}>{t.domain}</div>
                                    </div>
                                    <div style={S.td}>{t.planName ? <span style={S.planBadge}>{t.planName}</span> : <span style={S.noPlanBadge}>No plan</span>}</div>
                                    <div style={S.td}>
                                        {t.monthlyPrice != null ? `$${Number(t.monthlyPrice).toFixed(2)}` : '—'}
                                        {t.negotiatedDiscount > 0 && <span style={{ ...S.planBadge, marginLeft: 6 }}>{t.negotiatedDiscount}% off</span>}
                                    </div>
                                    <div style={S.td}>
                                        {t.paymentFailed ? <span style={S.failBadge}>⚠ Failed</span>
                                            : t.hasStripeSubscription ? <span style={S.activeBadge}>Active</span>
                                                : <span style={S.pendingBadge}>No Stripe</span>}
                                    </div>
                                    <div style={S.td}>{t.lastPaymentAt ? new Date(t.lastPaymentAt).toLocaleDateString() : '—'}</div>
                                    <div style={S.td}><button style={S.viewBtn} onClick={() => loadTenantDetail(t)}>View</button></div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            )}

            {/* ── TENANT DETAIL ── */}
            {tab === 'Tenants' && selectedTenant && (
                <div style={S.section}>
                    <button style={S.backBtn} onClick={() => setSelectedTenant(null)}>← Back to Tenants</button>
                    {detailLoading ? <div style={S.loading}>Loading...</div> : tenantDetail && (
                        <>
                            <div style={S.detailHeader}>
                                <div style={S.detailName}>{tenantDetail.name}</div>
                                <div style={S.tenantDomain}>{tenantDetail.domain} · {tenantDetail.mailboxAddress}</div>
                            </div>
                            <div style={S.statsBar}>
                                <StatPill label="Total Docs" value={tenantDetail.totalDocuments?.toLocaleString()} />
                                <StatPill label="Active Staff" value={tenantDetail.activeStaff} />
                                <StatPill label="Docs This Month" value={tenantDetail.docsThisMonth?.toLocaleString()} />
                            </div>

                            {/* Current subscription */}
                            <div style={S.card}>
                                <div style={S.cardTitle}>Subscription</div>
                                {tenantDetail.subscription ? (
                                    <>
                                        <SubRow label="Plan"><span style={S.planBadge}>{tenantDetail.subscription.planName}</span></SubRow>
                                        <SubRow label="Monthly Price">${Number(tenantDetail.subscription.monthlyPrice).toFixed(2)}</SubRow>
                                        <SubRow label="Included Docs">{tenantDetail.subscription.includedDocuments}</SubRow>
                                        <SubRow label="Extra Doc Price">${Number(tenantDetail.subscription.extraDocumentPrice).toFixed(4)}</SubRow>
                                        <SubRow label="Stripe Sub"><span style={{ fontSize: 11, fontFamily: 'monospace', color: '#7a9ab0' }}>{tenantDetail.subscription.stripeSubscriptionId || '—'}</span></SubRow>
                                        {tenantDetail.subscription.lastPaymentAt && <SubRow label="Last Payment">{new Date(tenantDetail.subscription.lastPaymentAt).toLocaleDateString()}</SubRow>}
                                        {tenantDetail.subscription.paymentFailedAt && <SubRow label="Payment Failed"><span style={{ color: '#e53e3e' }}>⚠ {tenantDetail.subscription.paymentFailureCount} attempt(s)</span></SubRow>}
                                    </>
                                ) : <div style={{ fontSize: 13, color: '#7a9ab0' }}>No active subscription.</div>}
                                {tenantDetail.activePromoCode && <SubRow label="Active Promo"><span style={S.planBadge}>{tenantDetail.activePromoCode}</span></SubRow>}
                            </div>

                            {/* Negotiated discount */}
                            <div style={S.card}>
                                <div style={S.cardTitle}>Negotiated Discount</div>
                                <div style={{ fontSize: 12, color: '#7a9ab0', marginBottom: 12 }}>
                                    Private discount on the monthly base price for this client only. Preserved when they switch plans.
                                </div>
                                <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                                    <input
                                        style={{ ...S.input, width: 100, marginBottom: 0 }}
                                        type="number" min="0" max="100" step="1"
                                        placeholder="0"
                                        value={discountInput}
                                        onChange={e => setDiscountInput(e.target.value)}
                                    />
                                    <span style={{ fontSize: 13, color: '#4a6478' }}>% off base price</span>
                                    <button style={{ ...S.primaryBtn, opacity: savingDiscount ? 0.6 : 1 }} onClick={saveDiscount} disabled={savingDiscount}>
                                        {savingDiscount ? 'Saving...' : 'Save'}
                                    </button>
                                </div>
                                {discountMsg && <div style={{ marginTop: 8 }}><Msg data={discountMsg} /></div>}
                            </div>

                            {/* Assign plan */}
                            <div style={S.card}>
                                <div style={S.cardTitle}>Assign Plan</div>
                                {assignMsg && <Msg data={assignMsg} />}
                                <div style={S.plansGrid}>
                                    {plans.filter(p => p.isCurrentlyActive).map(plan => {
                                        const isCurrent = tenantDetail.subscription?.planName === plan.name;
                                        return (
                                            <div key={plan.subscriptionPlanId} style={{ ...S.planCard, borderColor: isCurrent ? '#0d9488' : '#e2eaef', background: isCurrent ? 'rgba(13,148,136,0.04)' : '#fff' }}>
                                                <div style={S.planCardName}>{plan.name}</div>
                                                <div style={S.planCardPrice}>${Number(plan.monthlyPrice).toFixed(2)}<span style={S.planCardPer}>/mo</span></div>
                                                <div style={S.planCardDocs}>{plan.includedDocuments} docs</div>
                                                <button
                                                    style={{ ...S.planBtn, opacity: isCurrent || assigningPlan ? 0.5 : 1, cursor: isCurrent || assigningPlan ? 'not-allowed' : 'pointer', background: isCurrent ? 'transparent' : '#0d9488', color: isCurrent ? '#0d9488' : '#fff', border: isCurrent ? '1px solid #0d9488' : 'none' }}
                                                    disabled={isCurrent || assigningPlan}
                                                    onClick={() => !isCurrent && assignPlan(plan.subscriptionPlanId)}
                                                >
                                                    {isCurrent ? 'Current' : assigningPlan ? '...' : 'Assign'}
                                                </button>
                                            </div>
                                        );
                                    })}
                                </div>
                            </div>

                            {/* Tenant info */}
                            <div style={S.card}>
                                <div style={S.cardTitle}>Tenant Info</div>
                                <SubRow label="Tenant ID"><span style={{ fontSize: 11, fontFamily: 'monospace', color: '#7a9ab0' }}>{tenantDetail.tenantId}</span></SubRow>
                                <SubRow label="Stripe Customer"><span style={{ fontSize: 11, fontFamily: 'monospace', color: '#7a9ab0' }}>{tenantDetail.stripeCustomerId || '—'}</span></SubRow>
                                <SubRow label="HIPAA">
                                    <button
                                        style={{ fontSize: 12, fontWeight: 600, padding: '4px 14px', borderRadius: 20, border: 'none', cursor: togglingHipaa ? 'not-allowed' : 'pointer', fontFamily: 'inherit', opacity: togglingHipaa ? 0.6 : 1, background: tenantDetail.isHipaaEnabled ? 'rgba(13,148,136,0.1)' : '#f0f4f7', color: tenantDetail.isHipaaEnabled ? '#0d9488' : '#7a9ab0' }}
                                        onClick={toggleHipaa}
                                        disabled={togglingHipaa}
                                    >
                                        {tenantDetail.isHipaaEnabled ? '✅ Enabled' : 'Disabled — Click to Enable'}
                                    </button>
                                </SubRow>
                                <SubRow label="Member Since">{new Date(tenantDetail.startDate).toLocaleDateString()}</SubRow>
                            </div>
                        </>
                    )}
                </div>
            )}

            {/* ── PLANS TAB ── */}
            {tab === 'Plans' && (
                <div style={S.section}>
                    <div style={S.sectionHeader}>
                        <div style={S.sectionTitle}>Subscription Plans</div>
                        <button style={S.primaryBtn} onClick={() => setShowCreatePlan(!showCreatePlan)}>
                            {showCreatePlan ? 'Cancel' : '+ New Plan'}
                        </button>
                    </div>

                    {planMsg && <Msg data={planMsg} />}

                    {showCreatePlan && (
                        <div style={S.formCard}>
                            <div style={S.cardTitle}>Create New Plan</div>
                            <div style={S.formGrid}>
                                <div>
                                    <label style={S.label}>Plan Name</label>
                                    <input style={S.input} placeholder="Enterprise" value={planForm.name} onChange={e => setPlanForm(f => ({ ...f, name: e.target.value }))} />
                                </div>
                                <div>
                                    <label style={S.label}>Monthly Price ($)</label>
                                    <input style={S.input} type="number" placeholder="99.00" value={planForm.monthlyPrice} onChange={e => setPlanForm(f => ({ ...f, monthlyPrice: e.target.value }))} />
                                </div>
                                <div>
                                    <label style={S.label}>Included Documents</label>
                                    <input style={S.input} type="number" placeholder="500" value={planForm.includedDocuments} onChange={e => setPlanForm(f => ({ ...f, includedDocuments: e.target.value }))} />
                                </div>
                                <div>
                                    <label style={S.label}>Extra Doc Price ($)</label>
                                    <input style={S.input} type="number" placeholder="0.50" step="0.01" value={planForm.extraDocumentPrice} onChange={e => setPlanForm(f => ({ ...f, extraDocumentPrice: e.target.value }))} />
                                </div>
                            </div>
                            <label style={S.label}>Description</label>
                            <input style={S.input} placeholder="For high-volume teams..." value={planForm.description} onChange={e => setPlanForm(f => ({ ...f, description: e.target.value }))} />
                            <label style={{ ...S.label, display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
                                <input type="checkbox" checked={planForm.isPublic} onChange={e => setPlanForm(f => ({ ...f, isPublic: e.target.checked }))} />
                                Show publicly to clients
                            </label>
                            <div style={{ fontSize: 12, color: '#7a9ab0', marginBottom: 12 }}>
                                💳 A Stripe price will be created automatically.
                            </div>
                            <div style={{ display: 'flex', gap: 8 }}>
                                <button style={{ ...S.primaryBtn, opacity: creatingPlan ? 0.6 : 1 }} onClick={createPlan} disabled={creatingPlan}>
                                    {creatingPlan ? 'Creating...' : 'Create Plan'}
                                </button>
                                <button style={S.secondaryBtn} onClick={() => { setShowCreatePlan(false); setPlanMsg(null); }}>Cancel</button>
                            </div>
                        </div>
                    )}

                    <div style={S.table}>
                        <div style={S.tableHead}>
                            <div style={{ ...S.th, flex: 2 }}>Plan</div>
                            <div style={S.th}>Monthly</div>
                            <div style={S.th}>Included Docs</div>
                            <div style={S.th}>Extra Doc</div>
                            <div style={S.th}>Stripe Price</div>
                            <div style={S.th}>Status</div>
                            <div style={S.th}></div>
                        </div>
                        {plans.map(p => (
                            <div key={p.subscriptionPlanId} style={S.tableRow}>
                                <div style={{ ...S.td, flex: 2 }}>
                                    <div style={S.tenantName}>{p.name}</div>
                                    <div style={S.tenantDomain}>{p.description}</div>
                                </div>
                                <div style={S.td}>${Number(p.monthlyPrice).toFixed(2)}</div>
                                <div style={S.td}>{p.includedDocuments?.toLocaleString()}</div>
                                <div style={S.td}>${Number(p.extraDocumentPrice).toFixed(4)}</div>
                                <div style={S.td}>
                                    {p.stripePriceId
                                        ? <span style={{ fontSize: 11, fontFamily: 'monospace', color: '#7a9ab0' }}>{p.stripePriceId.substring(0, 16)}...</span>
                                        : <span style={S.failBadge}>Not linked</span>
                                    }
                                </div>
                                <div style={S.td}>{p.isCurrentlyActive ? <span style={S.activeBadge}>Active</span> : <span style={S.noPlanBadge}>Archived</span>}</div>
                                <div style={S.td}>
                                    {p.needsStripeSync && (
                                        <button
                                            style={{ ...S.viewBtn, background: '#fef3c7', color: '#92400e', opacity: syncingPlan === p.subscriptionPlanId ? 0.6 : 1 }}
                                            onClick={() => syncPlanToStripe(p.subscriptionPlanId)}
                                            disabled={syncingPlan === p.subscriptionPlanId}
                                        >
                                            {syncingPlan === p.subscriptionPlanId ? 'Syncing...' : 'Sync to Stripe'}
                                        </button>
                                    )}
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* ── PROMOS TAB ── */}
            {tab === 'Promos' && (
                <div style={S.section}>
                    <div style={S.sectionHeader}>
                        <div style={S.sectionTitle}>Promo Codes</div>
                        <button style={S.primaryBtn} onClick={() => setShowCreatePromo(!showCreatePromo)}>
                            {showCreatePromo ? 'Cancel' : '+ New Promo'}
                        </button>
                    </div>

                    {promoMsg && <Msg data={promoMsg} />}

                    {showCreatePromo && (
                        <div style={S.formCard}>
                            <div style={S.cardTitle}>Create Promo Code</div>
                            <div style={S.formGrid}>
                                <div>
                                    <label style={S.label}>Code</label>
                                    <input style={S.input} placeholder="WELCOME20" value={promoForm.code} onChange={e => setPromoForm(f => ({ ...f, code: e.target.value.toUpperCase() }))} />
                                </div>
                                <div>
                                    <label style={S.label}>Max Redemptions (0 = unlimited)</label>
                                    <input style={S.input} type="number" placeholder="0" value={promoForm.maxRedemptions} onChange={e => setPromoForm(f => ({ ...f, maxRedemptions: parseInt(e.target.value) || 0 }))} />
                                </div>
                                <div>
                                    <label style={S.label}>Base Price Discount %</label>
                                    <input style={S.input} type="number" placeholder="0" value={promoForm.baseDiscountPercent} onChange={e => setPromoForm(f => ({ ...f, baseDiscountPercent: parseFloat(e.target.value) || 0 }))} />
                                </div>
                                <div>
                                    <label style={S.label}>Extra Docs Discount %</label>
                                    <input style={S.input} type="number" placeholder="0" value={promoForm.discountPercent} onChange={e => setPromoForm(f => ({ ...f, discountPercent: parseFloat(e.target.value) || 0 }))} />
                                </div>
                                <div>
                                    <label style={S.label}>Flat $ off per extra doc</label>
                                    <input style={S.input} type="number" placeholder="0.10" step="0.01" value={promoForm.flatDiscountPerDoc} onChange={e => setPromoForm(f => ({ ...f, flatDiscountPerDoc: parseFloat(e.target.value) || 0 }))} />
                                </div>
                                <div>
                                    <label style={S.label}>Free Doc Bonus</label>
                                    <input style={S.input} type="number" placeholder="50" value={promoForm.freeDocCount} onChange={e => setPromoForm(f => ({ ...f, freeDocCount: parseInt(e.target.value) || 0 }))} />
                                </div>
                            </div>
                            <label style={S.label}>Description</label>
                            <input style={S.input} placeholder="20% off base price for new clients Q2 2026" value={promoForm.description} onChange={e => setPromoForm(f => ({ ...f, description: e.target.value }))} />
                            <div style={{ display: 'flex', gap: 8 }}>
                                <button style={{ ...S.primaryBtn, opacity: creatingPromo ? 0.6 : 1 }} onClick={createPromo} disabled={creatingPromo}>
                                    {creatingPromo ? 'Creating...' : 'Create Promo'}
                                </button>
                                <button style={S.secondaryBtn} onClick={() => { setShowCreatePromo(false); setPromoMsg(null); }}>Cancel</button>
                            </div>
                        </div>
                    )}

                    {assigningPromo && (
                        <div style={{ ...S.formCard, borderColor: '#0d9488' }}>
                            <div style={S.cardTitle}>
                                Assign {promos.find(p => p.promoCodeId === assigningPromo)?.code} to Tenants
                            </div>
                            <div style={{ fontSize: 12, color: '#7a9ab0', marginBottom: 12 }}>
                                Select tenants to assign this promo. Optionally send them an email with the code.
                            </div>
                            <div style={{ maxHeight: 200, overflowY: 'auto', border: '1px solid #e2eaef', borderRadius: 8, marginBottom: 12 }}>
                                {tenants.map(t => (
                                    <label key={t.tenantId} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 12px', cursor: 'pointer', borderBottom: '1px solid #f0f4f7' }}>
                                        <input
                                            type="checkbox"
                                            checked={selectedTenantIds.includes(t.tenantId)}
                                            onChange={e => setSelectedTenantIds(prev =>
                                                e.target.checked ? [...prev, t.tenantId] : prev.filter(id => id !== t.tenantId)
                                            )}
                                        />
                                        <span style={{ fontSize: 13, fontWeight: 600 }}>{t.name}</span>
                                        <span style={{ fontSize: 11, color: '#7a9ab0' }}>{t.mailboxAddress}</span>
                                    </label>
                                ))}
                            </div>
                            <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, marginBottom: 16, cursor: 'pointer' }}>
                                <input type="checkbox" checked={sendEmail} onChange={e => setSendEmail(e.target.checked)} />
                                Send promo email to selected tenants
                            </label>
                            {assignPromoMsg && <Msg data={assignPromoMsg} />}
                            <div style={{ display: 'flex', gap: 8 }}>
                                <button style={{ ...S.primaryBtn, opacity: assigningPromoLoading ? 0.6 : 1 }} onClick={doAssignPromo} disabled={assigningPromoLoading}>
                                    {assigningPromoLoading ? 'Assigning...' : `Assign to ${selectedTenantIds.length} tenant(s)`}
                                </button>
                                <button style={S.secondaryBtn} onClick={() => { setAssigningPromo(null); setSelectedTenantIds([]); setAssignPromoMsg(null); }}>
                                    Cancel
                                </button>
                            </div>
                        </div>
                    )}

                    <div style={S.table}>
                        <div style={S.tableHead}>
                            <div style={{ ...S.th, flex: 1 }}>Code</div>
                            <div style={{ ...S.th, flex: 2 }}>Description</div>
                            <div style={S.th}>Base Discount</div>
                            <div style={S.th}>Doc Discount</div>
                            <div style={S.th}>Redemptions</div>
                            <div style={S.th}>Status</div>
                            <div style={S.th}></div>
                        </div>
                        {promos.map(p => (
                            <div key={p.promoCodeId} style={S.tableRow}>
                                <div style={{ ...S.td, flex: 1 }}><span style={S.planBadge}>{p.code}</span></div>
                                <div style={{ ...S.td, flex: 2, fontSize: 12, color: '#4a6478' }}>{p.description || '—'}</div>
                                <div style={S.td}>{p.baseDiscountPercent > 0 ? `${p.baseDiscountPercent}% off` : '—'}</div>
                                <div style={S.td}>
                                    {p.discountPercent > 0 ? `${p.discountPercent}% off` :
                                        p.flatDiscountPerDoc > 0 ? `$${p.flatDiscountPerDoc}/doc` :
                                            p.freeDocCount > 0 ? `+${p.freeDocCount} free` : '—'}
                                </div>
                                <div style={S.td}>{p.redemptionCount}{p.maxRedemptions > 0 ? ` / ${p.maxRedemptions}` : ''}</div>
                                <div style={S.td}>
                                    {!p.isActive ? <span style={S.noPlanBadge}>Inactive</span> :
                                        p.isExpired ? <span style={S.failBadge}>Expired</span> :
                                            <span style={S.activeBadge}>Active</span>}
                                </div>
                                <div style={S.td}>
                                    <button style={S.viewBtn} onClick={() => { setAssigningPromo(p.promoCodeId); setAssignPromoMsg(null); setSelectedTenantIds([]); }}>
                                        Assign
                                    </button>
                                </div>
                            </div>
                        ))}
                        {promos.length === 0 && <div style={{ padding: '20px', fontSize: 13, color: '#7a9ab0', textAlign: 'center' }}>No promo codes yet.</div>}
                    </div>
                </div>
            )}
        </div>
    );
}

function SubRow({ label, children }) {
    return (
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: 13, marginBottom: 10 }}>
            <span style={{ color: '#7a9ab0', fontWeight: 600, fontSize: 12 }}>{label}</span>
            <span>{children}</span>
        </div>
    );
}

function StatPill({ label, value, accent }) {
    return (
        <div style={{ border: `1px solid ${accent ? '#e53e3e' : '#e2eaef'}`, borderRadius: 10, padding: '12px 20px', background: '#fff', display: 'flex', flexDirection: 'column', gap: 2 }}>
            <div style={{ fontSize: 20, fontWeight: 800, color: accent ? '#e53e3e' : '#0f2d4a' }}>{value ?? '—'}</div>
            <div style={{ fontSize: 11, color: '#7a9ab0', fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>{label}</div>
        </div>
    );
}

function Msg({ data }) {
    return (
        <div style={{ fontSize: 12, fontWeight: 600, padding: '10px 14px', borderRadius: 8, marginBottom: 12, background: data.success ? '#d1fae5' : '#fee2e2', color: data.success ? '#065f46' : '#991b1b' }}>
            {data.success ? '✅' : '❌'} {data.message}
        </div>
    );
}

const S = {
    page: { fontFamily: "'Plus Jakarta Sans', sans-serif", background: '#f7f9fb', minHeight: '100vh', color: '#1a2e3b' },
    header: { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', padding: '28px 40px 20px', background: '#fff', borderBottom: '1px solid #e2eaef' },
    headerTitle: { fontSize: 20, fontWeight: 800, color: '#0f2d4a' },
    headerSub: { fontSize: 12, color: '#7a9ab0', marginTop: 3 },
    statsBar: { display: 'flex', gap: 12, padding: '16px 40px', background: '#fff', borderBottom: '1px solid #e2eaef' },
    tabs: { display: 'flex', gap: 4, padding: '12px 40px', background: '#fff', borderBottom: '1px solid #e2eaef' },
    tabBtn: { fontSize: 13, fontWeight: 600, padding: '7px 18px', border: '1px solid #d1dde6', borderRadius: 8, background: '#fff', color: '#4a6478', cursor: 'pointer', fontFamily: 'inherit' },
    tabBtnActive: { background: '#0f2d4a', color: '#fff', borderColor: '#0f2d4a' },
    section: { padding: '28px 40px' },
    sectionHeader: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 },
    sectionTitle: { fontSize: 16, fontWeight: 800, color: '#0f2d4a' },
    search: { flex: 1, padding: '9px 14px', fontSize: 13, border: '1px solid #d1dde6', borderRadius: 8, fontFamily: 'inherit', outline: 'none', background: '#fff', marginRight: 12 },
    loading: { fontSize: 13, color: '#7a9ab0', padding: '20px 0' },
    table: { background: '#fff', border: '1px solid #e2eaef', borderRadius: 12, overflow: 'hidden' },
    tableHead: { display: 'flex', padding: '12px 20px', background: '#f7f9fb', borderBottom: '1px solid #e2eaef' },
    tableRow: { display: 'flex', padding: '14px 20px', alignItems: 'center', borderBottom: '1px solid #f0f4f7' },
    th: { flex: 1, fontSize: 11, fontWeight: 700, color: '#7a9ab0', textTransform: 'uppercase', letterSpacing: 0.5 },
    td: { flex: 1, fontSize: 13, color: '#1a2e3b' },
    tenantName: { fontWeight: 700, color: '#0f2d4a', marginBottom: 2 },
    tenantDomain: { fontSize: 11, color: '#7a9ab0' },
    planBadge: { background: 'rgba(13,148,136,0.1)', color: '#0d9488', fontSize: 11, fontWeight: 700, padding: '3px 10px', borderRadius: 20 },
    noPlanBadge: { background: '#f0f4f7', color: '#7a9ab0', fontSize: 11, fontWeight: 600, padding: '3px 10px', borderRadius: 20 },
    activeBadge: { background: 'rgba(13,148,136,0.1)', color: '#0d9488', fontSize: 11, fontWeight: 700, padding: '3px 10px', borderRadius: 20 },
    pendingBadge: { background: '#fef3c7', color: '#92400e', fontSize: 11, fontWeight: 600, padding: '3px 10px', borderRadius: 20 },
    failBadge: { background: '#fee2e2', color: '#991b1b', fontSize: 11, fontWeight: 700, padding: '3px 10px', borderRadius: 20 },
    viewBtn: { fontSize: 12, fontWeight: 600, color: '#0d9488', background: 'rgba(13,148,136,0.08)', border: 'none', borderRadius: 6, padding: '5px 14px', cursor: 'pointer', fontFamily: 'inherit' },
    backBtn: { fontSize: 13, fontWeight: 600, color: '#4a6478', background: 'transparent', border: 'none', cursor: 'pointer', fontFamily: 'inherit', padding: '0 0 20px 0', display: 'block' },
    detailHeader: { marginBottom: 16 },
    detailName: { fontSize: 22, fontWeight: 800, color: '#0f2d4a' },
    card: { background: '#fff', border: '1px solid #e2eaef', borderRadius: 12, padding: '20px 24px', marginBottom: 20 },
    cardTitle: { fontSize: 14, fontWeight: 700, color: '#0f2d4a', marginBottom: 16 },
    plansGrid: { display: 'flex', gap: 12 },
    planCard: { flex: 1, border: '2px solid', borderRadius: 10, padding: '16px', textAlign: 'center' },
    planCardName: { fontSize: 12, fontWeight: 800, color: '#0f2d4a', textTransform: 'uppercase', letterSpacing: 1, marginBottom: 8 },
    planCardPrice: { fontSize: 20, fontWeight: 800, color: '#0f2d4a', marginBottom: 4 },
    planCardPer: { fontSize: 11, fontWeight: 400, color: '#7a9ab0' },
    planCardDocs: { fontSize: 11, color: '#4a6478', fontWeight: 600, marginBottom: 12 },
    planBtn: { width: '100%', fontSize: 12, fontWeight: 700, borderRadius: 7, padding: '7px 0', fontFamily: 'inherit' },
    formCard: { background: '#fff', border: '1px solid #e2eaef', borderRadius: 12, padding: '24px 28px', marginBottom: 24 },
    formGrid: { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 12 },
    label: { fontSize: 12, fontWeight: 600, color: '#4a6478', marginBottom: 4, display: 'block' },
    input: { width: '100%', padding: '9px 12px', fontSize: 13, border: '1px solid #d1dde6', borderRadius: 8, fontFamily: 'inherit', outline: 'none', marginBottom: 12, boxSizing: 'border-box', background: '#fff', color: '#1a2e3b' },
    primaryBtn: { fontSize: 13, fontWeight: 700, color: '#fff', background: '#0d9488', border: 'none', borderRadius: 8, padding: '9px 20px', cursor: 'pointer', fontFamily: 'inherit' },
    secondaryBtn: { fontSize: 13, fontWeight: 600, color: '#4a6478', background: 'transparent', border: '1px solid #d1dde6', borderRadius: 8, padding: '9px 20px', cursor: 'pointer', fontFamily: 'inherit' },
};
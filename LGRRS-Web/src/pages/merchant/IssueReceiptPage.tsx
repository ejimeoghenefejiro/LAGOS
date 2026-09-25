import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import type { CatalogItem } from "../../api/types";
import { loadDeliveryPreferences } from "../../state/receiptPreferences";
import { api, ApiError } from "../../api/client";
import type { ReceiptHistoryItem } from "../../api/types";
import { useSession } from "../../state/session";
import { useMerchantProfile } from "../../state/merchantProfile";
import ReceiptsTable from "../../components/ReceiptsTable";
import StatusPill, { labelFor as statusLabel } from "../../components/StatusPill";
import ChannelButton from "../../components/ChannelButton";
import { IconReceiptPlus, IconSend, IconShieldCheck, IconSms, IconSmartphone, IconWhatsApp } from "../../components/icons";

const CHANNEL_OPTIONS = [
  { value: "WHATSAPP", label: "WhatsApp", description: "Send receipt via WhatsApp", icon: IconWhatsApp, tone: "whatsapp" },
  { value: "SMS", label: "SMS", description: "Send receipt via SMS", icon: IconSms, tone: "sms" },
  { value: "IN_APP", label: "In-App", description: "Send receipt to customer's LGRRS app", icon: IconSmartphone, tone: "inapp-list" }
];

export default function IssueReceiptPage() {
  const { session } = useSession();
  const { profile } = useMerchantProfile();
  const [history, setHistory] = useState<ReceiptHistoryItem[]>([]);
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [itemService, setItemService] = useState("");
  const [amount, setAmount] = useState("");
  const [catalog, setCatalog] = useState<CatalogItem[]>([]);
  const [catalogError, setCatalogError] = useState("");
  const [selectedItem, setSelectedItem] = useState("");
  useEffect(() => {
    if (!session) return;
    api.get<CatalogItem[]>("/api/merchant/catalog", session.accessToken)
      .then(items => setCatalog(items.filter(i => i.isActive)))
      .catch(() => setCatalogError("Saved items could not load. You can enter the receipt manually."));
  }, [session]);
  const [chosenChannels, setChannels] = useState<string[] | null>(null);
  const channels = chosenChannels ?? loadDeliveryPreferences(profile?.merchantId);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [preview, setPreview] = useState<ReceiptHistoryItem | null>(null);

  const loadHistory = async () => {
    if (!session) return;
    const items = await api.get<ReceiptHistoryItem[]>("/api/merchant/receipts", session.accessToken);
    setHistory(items);
  };

  useEffect(() => {
    loadHistory().catch(() => setError("Could not load receipt history."));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session]);

  if (!session) return null;

  const toggleChannel = (value: string) => {
    setChannels(channels.includes(value) ? channels.filter(c => c !== value) : [...channels, value]);
  };

  const generateReceipt = async () => {
    setError(null);
    setSubmitting(true);
    try {
      const created = await api.post<ReceiptHistoryItem>(
        "/api/merchant/receipts",
        {
          customerName: customerName || null,
          customerPhone: customerPhone || null,
          itemService,
          amount: Number(amount),
          deliveryChannels: channels
        },
        session.accessToken
      );
      setPreview(created);
      await loadHistory();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not create receipt.");
    } finally {
      setSubmitting(false);
    }
  };

  const startAnother = () => {
    setPreview(null);
    setCustomerName("");
    setCustomerPhone("");
    setItemService("");
    setAmount("");
    setSelectedItem("");
    setChannels(null);
  };

  return (
    <div className="stack-lg">
      {!preview ? (
        <section className="step-card">
          <div className="step-header">
            <span className="step-badge tone-teal">1</span>
            <div className="step-icon tone-teal-solid"><IconReceiptPlus width={20} height={20} /></div>
            <div>
              <h2>Create and Send Receipt</h2>
              <p className="muted">Enter customer details, receipt information, and choose how you'd like to send the receipt.</p>
            </div>
          </div>

          <div className="two-col">
            <div className="stack-md">
              <label className="field-label">Saved product or service (optional)
                <select value={selectedItem} onChange={e => {
                  setSelectedItem(e.target.value);
                  const item = catalog.find(i => i.catalogItemId === e.target.value);
                  if (item) { setItemService(item.name); setAmount(item.price.toFixed(2)); }
                }}>
                  <option value="">Enter manually</option>
                  {catalog.map(i => <option key={i.catalogItemId} value={i.catalogItemId}>{i.name} — ₦{i.price.toLocaleString()}</option>)}
                </select>
              </label>
              <Link to="/merchant/catalog">Manage products &amp; services</Link>
              {catalogError && <p role="status">{catalogError}</p>}
              <label className="field-label">
                Customer Name (optional)
                <input value={customerName} onChange={(e) => setCustomerName(e.target.value)} placeholder="Tunde A." />
              </label>
              <label className="field-label">
                Customer Phone Number (required for their private wallet)
                <input value={customerPhone} onChange={(e) => setCustomerPhone(e.target.value)} placeholder="0808 112 7788" />
              </label>
              <label className="field-label">
                Item / Service
                <input value={itemService} onChange={(e) => { setItemService(e.target.value); setSelectedItem(""); }} placeholder="Haircut" />
              </label>
              <label className="field-label">
                Amount
                <div className="amount-input">
                  <span>₦</span>
                  <input value={amount} onChange={(e) => setAmount(e.target.value)} type="number" placeholder="2,500" />
                </div>
              </label>
            </div>

            <div className="channel-list">
              <div className="field-label">Receipt Delivery Method</div>
              <div className="channel-list-box">
                {CHANNEL_OPTIONS.map((opt) => (
                  <label key={opt.value} className={`channel-row tone-${opt.tone}`}>
                    <input
                      type="checkbox"
                      checked={channels.includes(opt.value)}
                      onChange={() => toggleChannel(opt.value)}
                    />
                    <span className="channel-icon"><opt.icon width={18} height={18} /></span>
                    <span>
                      <span className="channel-label">{opt.label}</span>
                      <span className="channel-desc">{opt.description}</span>
                      {customerPhone && <span className="channel-desc">{customerPhone}</span>}
                    </span>
                  </label>
                ))}
              </div>
            </div>
          </div>

          <button
            className="primary full-width tone-teal"
            onClick={generateReceipt}
            disabled={submitting || !itemService.trim() || !amount || channels.length === 0}
          >
            <IconSend width={16} height={16} /> {submitting ? "Sending..." : "Generate Receipt & Send"}
          </button>

          {error && <p className="error">{error}</p>}
        </section>
      ) : (
        <section className="step-card">
          <div className="step-header">
            <span className="step-badge tone-teal">2</span>
            <div className="step-header-grow">
              <h2>Receipt Preview</h2>
            </div>
            <StatusPill status={preview.deliveryStatus} />
          </div>

          <div className="two-col">
            <div className="preview-summary-block">
              <div className="preview-business">
                {session.displayName}
                {profile && (
                  <span className="preview-business-meta">
                    {profile.businessType} · {profile.lgaCode} Local Government, Lagos State
                  </span>
                )}
              </div>
              <dl className="preview-dl">
                <dt>Date &amp; Time</dt>
                <dd>{new Date(preview.transactionDate).toLocaleString()}</dd>
                <dt>Item / Service</dt>
                <dd>{preview.itemService}</dd>
                <dt>Amount</dt>
                <dd className="amount-highlight">₦{preview.amount.toLocaleString()}</dd>
                <dt>Customer</dt>
                <dd>{preview.customerName ?? "—"}</dd>
                <dt>Phone</dt>
                <dd>{preview.customerPhoneMasked}</dd>
                <dt>Status</dt>
                <dd><StatusPill status={preview.deliveryStatus} /></dd>
              </dl>
            </div>

            <div className="receipt-summary-panel">
              <div className="step-icon tone-green"><IconSend width={18} height={18} /></div>
              <h3>Receipt Summary</h3>
              <p className="summary-status-line">
                Status: <span className="status-text-green">{statusLabel(preview.deliveryStatus)}</span>
              </p>
              <p className="muted">Customer will receive the receipt link via their preferred channel.</p>
              <div className="field-label">Delivery Channels</div>
              <div className="channel-buttons">
                {preview.channels.map((c) => (
                  <ChannelButton key={c} channel={c} />
                ))}
              </div>
              <p className="secure-note"><IconShieldCheck width={16} height={16} /> Secure delivery. No sensitive data shared.</p>
            </div>
          </div>

          <button className="primary full-width tone-teal" onClick={startAnother}>
            Issue Another Receipt
          </button>
        </section>
      )}

      <section className="step-card">
        <div className="section-heading-row">
          <h2>Recent Receipts</h2>
        </div>
        <ReceiptsTable receipts={history.slice(0, 10)} />
      </section>
    </div>
  );
}

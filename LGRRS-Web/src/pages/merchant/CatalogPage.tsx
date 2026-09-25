import { useEffect, useState, type FormEvent } from "react";
import { api } from "../../api/client";
import { useSession } from "../../state/session";
import type { CatalogItem } from "../../api/types";
export default function CatalogPage() {
  const { session } = useSession();
  const [items, setItems] = useState<CatalogItem[]>([]);
  const [editing, setEditing] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [kind, setKind] = useState("Service");
  const [price, setPrice] = useState("");
  const [active, setActive] = useState(true);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [search, setSearch] = useState("");
  useEffect(() => {
    if (!session) return;
    api.get<CatalogItem[]>("/api/merchant/catalog", session.accessToken).then(setItems)
      .catch(() => setError("Could not load the catalogue. Please reload.")).finally(() => setLoading(false));
  }, [session]);
  function reset() { setEditing(null); setName(""); setKind("Service"); setPrice(""); setActive(true); }
  async function save(e: FormEvent) {
    e.preventDefault(); if (!session) return;
    setBusy(true); setError(""); setMessage("");
    try {
      const data = { name: name.trim(), kind, price: Number(price), isActive: active };
      const saved = editing
        ? await api.patch<CatalogItem>(`/api/merchant/catalog/${editing}`, data, session.accessToken)
        : await api.post<CatalogItem>("/api/merchant/catalog", data, session.accessToken);
      setItems(prev => [...prev.filter(i => i.catalogItemId !== saved.catalogItemId), saved].sort((a,b) => a.name.localeCompare(b.name)));
      reset(); setMessage("Item saved. Active items are available when issuing a receipt.");
    } catch (err) { setError(err instanceof Error ? err.message : "Could not save item."); }
    finally { setBusy(false); }
  }
  return <div className="stack-lg">
    <div><h1>Products &amp; Services</h1><p>Save frequently sold items and prices to speed up receipt creation.</p></div>
    {error && <p role="alert" className="error">{error}</p>}
    {message && <p role="status" className="success">{message}</p>}
    <form className="step-card stack-md" onSubmit={save}>
      <h2>{editing ? "Edit item" : "Add an item"}</h2>
      <fieldset disabled={busy} className="registration-fields form-grid">
        <label className="field-label">Name<input required maxLength={200} value={name} onChange={e => setName(e.target.value)} placeholder="Haircut or Bread" /></label>
        <label className="field-label">Type<select value={kind} onChange={e => setKind(e.target.value)}><option>Service</option><option>Product</option></select></label>
        <label className="field-label">Price (₦)<input required type="number" min="0.01" max="1000000000" step="0.01" value={price} onChange={e => setPrice(e.target.value)} /></label>
        <label className="field-label">Availability<select value={active ? "active" : "inactive"} onChange={e => setActive(e.target.value === "active")}><option value="active">Active</option><option value="inactive">Inactive</option></select></label>
      </fieldset>
      <div className="step-actions">{editing && <button type="button" disabled={busy} onClick={reset}>Cancel edit</button>}<button className="primary" disabled={busy || !name.trim()}>{busy ? "Saving…" : "Save item"}</button></div>
    </form>
    <section className="step-card stack-md">
      <label className="field-label">Search saved items<input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by name" /></label>
      {loading ? <p role="status">Loading items…</p> : <div className="table-scroll"><table className="receipts-table">
        <thead><tr><th>Name</th><th>Type</th><th>Price</th><th>Status</th><th>Action</th></tr></thead>
        <tbody>{items.filter(i => i.name.toLowerCase().includes(search.toLowerCase())).map(i => <tr key={i.catalogItemId}>
          <td>{i.name}</td><td>{i.kind}</td><td>₦{i.price.toLocaleString(undefined, { minimumFractionDigits: 2 })}</td><td>{i.isActive ? "Active" : "Inactive"}</td>
          <td><button disabled={busy} onClick={() => { setEditing(i.catalogItemId); setName(i.name); setKind(i.kind); setPrice(String(i.price)); setActive(i.isActive); setMessage(""); window.scrollTo({ top: 0 }); }}>Edit</button></td>
        </tr>)}</tbody>
      </table>{!items.length && <p>No saved items yet. Add your first product or service above.</p>}</div>}
      <p className="muted">Mark an item inactive to remove it from receipt selection. Changing a saved price does not change existing receipts.</p>
    </section>
  </div>;
}

import { useEffect, useRef, useState } from "react";
import { NavLink, Navigate, Outlet } from "react-router-dom";
import { useSession } from "../../state/session";
import { Roles } from "../../state/roles";
import {
  IconBarChart,
  IconBell,
  IconCalendarRange,
  IconClipboardList,
  IconHexagon,
  IconHome,
  IconMenu,
  IconSettings,
  IconShieldCheck,
  IconStore,
  IconTicket,
  IconTrophy,

} from "../../components/icons";

const NAV_ITEMS = [
  { to: "/admin", label: "Overview", icon: IconHome, end: true },
  { to: "/admin/receipts", label: "Receipts", icon: IconTicket, end: false },
  { to: "/admin/merchants", label: "Merchants", icon: IconStore, end: false },

  { to: "/admin/draws", label: "Draws & Winners", icon: IconTrophy, end: false },
  { to: "/admin/fraud", label: "Fraud & Risk", icon: IconShieldCheck, end: false },
  { to: "/admin/reports", label: "Customer Reports", icon: IconBarChart, end: false },
  { to: "/admin/audit-log", label: "Audit Log", icon: IconClipboardList, end: false },
  { to: "/admin/settings", label: "Settings", icon: IconSettings, end: false }
];

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return parts.slice(0, 2).map((p) => p[0]?.toUpperCase() ?? "").join("");
}

export default function AdminLayout() {
  const { session } = useSession();

  const [mobile, setMobile] = useState(() => window.matchMedia("(max-width: 760px)").matches);
  const [navOpen, setNavOpen] = useState(false);
  const closeRef = useRef<HTMLButtonElement>(null);
  useEffect(() => { if (mobile && navOpen) closeRef.current?.focus(); }, [mobile, navOpen]);
  const toggleRef = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    const media = window.matchMedia("(max-width: 760px)");
    const resize = () => { setMobile(media.matches); setNavOpen(false); };
    const escape = (e: KeyboardEvent) => { if (e.key === "Escape") { setNavOpen(false); toggleRef.current?.focus(); } };
    media.addEventListener("change", resize); document.addEventListener("keydown", escape);
    return () => { media.removeEventListener("change", resize); document.removeEventListener("keydown", escape); };
  }, []);
  const visible = mobile ? navOpen : !navOpen;
  if (!session || session.role !== Roles.LgaAdmin) {
    return <Navigate to="/admin/login" replace />;
  }

  return (
    <div className={`admin-shell admin-interactive${visible ? " nav-visible" : ""}`}>
      <aside className="admin-sidebar" id="admin-navigation" hidden={!visible}>
        <div className="admin-brand">
          <IconHexagon width={32} height={32} className="admin-logo" />
          <div>
            <div className="admin-brand-title">LGRRS</div>
            <div className="admin-brand-subtitle">Lagos LGA Receipt Reward Scheme</div>
          </div>
        </div>

        {mobile && <button ref={closeRef} onClick={() => { setNavOpen(false); toggleRef.current?.focus(); }}>Close navigation</button>}
        <nav className="admin-nav" aria-label="Administrator navigation">
          {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
            <NavLink onClick={() => { if (mobile) { setNavOpen(false); toggleRef.current?.focus(); } }} key={to} to={to} end={end} className={({ isActive }) => `admin-nav-item${isActive ? " active" : ""}`}>
              <Icon width={18} height={18} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="admin-demo-note">
          <IconClipboardList width={16} height={16} />
          <div>
            <strong>DEMO DATA</strong>
            <p>All data on this dashboard is for demonstration purposes only.</p>
          </div>
        </div>

        <div className="admin-profile-card">
          <span className="admin-avatar">{initials(session.displayName)}</span>
          <div>
            <div className="admin-profile-name">{session.displayName}</div>
            <div className="admin-profile-role">Ikeja LGA</div>
          </div>
        </div>
      </aside>

      {mobile && navOpen && <button className="admin-nav-backdrop" aria-label="Close navigation" onClick={() => { setNavOpen(false); toggleRef.current?.focus(); }} />}
      <div className="admin-main" inert={mobile && navOpen ? true : undefined}>
        <header className="admin-topbar">
          <div className="admin-topbar-left">
            <button ref={toggleRef} className="admin-nav-toggle" aria-label="Toggle navigation" aria-expanded={visible} aria-controls="admin-navigation" onClick={() => setNavOpen(v => !v)}><IconMenu width={20} height={20} /></button>
            <div>
              <div className="admin-page-title">LGA Dashboard</div>
              <div className="admin-page-subtitle">Ikeja Local Government Area</div>
            </div>
          </div>
          <div className="admin-topbar-right">
            <span className="demo-pill">Demo Environment</span>
            <span className="admin-date-pill">
              <IconCalendarRange width={15} height={15} /> {new Date().toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric" })}
            </span>
            <span className="bell-wrap">
              <IconBell width={19} height={19} />
              <span className="bell-badge">3</span>
            </span>
            <span className="admin-avatar small">{initials(session.displayName)}</span>
          </div>
        </header>

        <main className="admin-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

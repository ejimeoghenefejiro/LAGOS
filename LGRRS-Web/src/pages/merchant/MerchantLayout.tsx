import { useEffect, useRef, useState } from "react";
import { Link, NavLink, Navigate, Outlet } from "react-router-dom";
import { useSession } from "../../state/session";
import { Roles } from "../../state/roles";
import { MerchantProfileContext } from "../../state/merchantProfile";
import { api } from "../../api/client";
import type { MerchantSummary } from "../../api/types";
import {
  IconChevronDown,
  IconHexagon,
  IconHistory,
  IconHome,
  IconMenu,
  IconReceiptPlus,
  IconSettings,
  IconStore,
  IconMail,
  IconPhone
} from "../../components/icons";

const NAV_ITEMS = [
  { to: "/merchant/catalog", label: "Products & Services", icon: IconStore, end: false },
  { to: "/merchant", label: "Dashboard", icon: IconHome, end: true },
  { to: "/merchant/issue-receipt", label: "Issue Receipt", icon: IconReceiptPlus, end: false },
  { to: "/merchant/receipts", label: "Receipt History", icon: IconHistory, end: false },
  { to: "/merchant/settings", label: "Settings", icon: IconSettings, end: false }
];

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return parts.slice(0, 2).map((p) => p[0]?.toUpperCase() ?? "").join("");
}

export default function MerchantLayout() {
  const { session, setSession } = useSession();
  const [navToggled, setNavToggled] = useState(false);
  const [accountOpen, setAccountOpen] = useState(false);
  const [mobile, setMobile] = useState(() => window.matchMedia("(max-width: 760px)").matches);
  const accountRef = useRef<HTMLDivElement>(null);
  const accountButton = useRef<HTMLButtonElement>(null);
  const navButton = useRef<HTMLButtonElement>(null);
  const [profile, setProfile] = useState<MerchantSummary | null>(null);

  useEffect(() => {
    const media = window.matchMedia("(max-width: 760px)");
    const resize = () => { setMobile(media.matches); setNavToggled(false); setAccountOpen(false); };
    const dismiss = (event: PointerEvent) => {
      if (!accountRef.current?.contains(event.target as Node)) setAccountOpen(false);
    };
    const escape = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      if (accountOpen) { setAccountOpen(false); accountButton.current?.focus(); }
      else if (mobile && navToggled) { setNavToggled(false); navButton.current?.focus(); }
    };
    media.addEventListener("change", resize);
    document.addEventListener("pointerdown", dismiss);
    document.addEventListener("keydown", escape);
    return () => {
      media.removeEventListener("change", resize);
      document.removeEventListener("pointerdown", dismiss);
      document.removeEventListener("keydown", escape);
    };
  }, [accountOpen, mobile, navToggled]);

  useEffect(() => {
    if (!session || session.role !== Roles.Merchant) return;
    api.get<MerchantSummary>("/api/merchant/profile", session.accessToken).then(setProfile).catch(() => {});
  }, [session]);

  if (!session || session.role !== Roles.Merchant) {
    return <Navigate to="/merchant/login" replace />;
  }

  return (
    <MerchantProfileContext.Provider value={{ profile }}>
      <div className={`merchant-shell${navToggled ? " nav-toggled" : ""}`}>
        <aside className="merchant-sidebar" id="merchant-desktop-navigation" hidden={mobile || navToggled}>
          <nav className="merchant-nav">
            {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
              <NavLink
                key={to}
                to={to}
                end={end}
                className={({ isActive }) => `merchant-nav-item${isActive ? " active" : ""}`}
              >
                <Icon width={18} height={18} />
                <span>{label}</span>
              </NavLink>
            ))}
          </nav>

          <div className="merchant-sidebar-footer">
            <div className="merchant-profile-card">
              <div className="merchant-profile-icon">
                <IconStore width={20} height={20} />
              </div>
              <div className="merchant-business-name">{session.displayName}</div>
              <div className="merchant-profile-meta">
                {profile ? `${profile.businessType} · ${profile.lgaCode}` : " "}
              </div>
              <div className="merchant-profile-badge">LGRRS Merchant</div>
              {profile?.lgrrsSystemId && <div className="merchant-profile-meta">{profile.lgrrsSystemId}</div>}
            </div>

            <div className="merchant-help-card">
              <div className="help-title">Need Help?</div>
              <div className="help-line"><IconPhone width={14} height={14} /> 01-888-LGRRS</div>
              <div className="help-line"><IconMail width={14} height={14} /> support@lgrrs.ng</div>
            </div>
          </div>
        </aside>

        <div className="merchant-main">
          <header className="merchant-topbar">
            <div className="merchant-topbar-left">
              <button ref={navButton} className="merchant-menu-toggle" type="button"
                aria-label={mobile ? "Toggle navigation" : "Toggle sidebar"}
                aria-expanded={mobile ? navToggled : !navToggled}
                aria-controls={mobile ? "merchant-mobile-navigation" : "merchant-desktop-navigation"}
                onClick={() => { setNavToggled(v => !v); setAccountOpen(false); }}>
                <IconMenu width={20} height={20} />
              </button>
              <IconHexagon width={30} height={30} className="brand-mark" />
              <div>
                <div className="merchant-title"><span className="merchant-desktop-title">LGRRS Merchant Portal</span><span className="merchant-mobile-title">LGRRS</span></div>
                <div className="merchant-subtitle">Local Government Receipt Reward Scheme</div>
              </div>
            </div>
            <div className="merchant-topbar-right">
              <span className="demo-pill">Demo Data</span>
              <div className="merchant-account" ref={accountRef}>
              <button ref={accountButton} type="button" className="avatar-chip merchant-account-toggle"
                aria-label="Account options" aria-expanded={accountOpen} aria-controls="merchant-account-panel"
                onClick={() => setAccountOpen(v => !v)}>
                <span className="avatar-circle">{initials(session.displayName)}</span>
                <span className="avatar-text">
                  <span className="avatar-name">{session.displayName}</span>
                  <span className="avatar-role">Merchant</span>
                </span>
                <IconChevronDown width={16} height={16} />
              </button>
              {accountOpen && <div className="merchant-account-panel" id="merchant-account-panel">
                <strong>{session.displayName}</strong>
                <span className="muted">{profile?.lgrrsSystemId ?? "Merchant account"}</span>
                <Link to="/merchant/settings" onClick={() => setAccountOpen(false)}>Business settings</Link>
                <Link to="/" onClick={() => setAccountOpen(false)}>Switch portal</Link>
                <button type="button" onClick={() => setSession(null)}>Sign out</button>
              </div>}
              </div>
            </div>
          </header>
          {mobile && navToggled && <nav className="merchant-mobile-nav" id="merchant-mobile-navigation" aria-label="Merchant navigation">
            {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => <NavLink key={to} to={to} end={end}
              className={({ isActive }) => `merchant-nav-item${isActive ? " active" : ""}`}
              onClick={() => { setNavToggled(false); navButton.current?.focus(); }}>
              <Icon width={18} height={18} /><span>{label}</span>
            </NavLink>)}
          </nav>}

          <main className="merchant-content">
            <Outlet />
          </main>
        </div>
      </div>
    </MerchantProfileContext.Provider>
  );
}

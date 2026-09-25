import { NavLink, Outlet } from "react-router-dom";
import { IconChevronDown, IconHexagon, IconHistory, IconTicket, IconTrophy } from "../../components/icons";

const NAV_ITEMS = [
  { to: "/check", label: "Check Ticket", icon: IconTicket, end: true },
  { to: "/check/entries", label: "My Receipts", icon: IconHistory, end: false },
  { to: "/check/winners", label: "Winners", icon: IconTrophy, end: false },
  { to: "/check/help", label: "Help", icon: HelpIcon, end: false }
];

function HelpIcon(props: { width?: number; height?: number; className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <circle cx="12" cy="12" r="9" />
      <path d="M9.2 9.5a2.8 2.8 0 0 1 5.4.9c0 1.6-2.4 1.9-2.4 3.5" />
      <circle cx="12" cy="17" r="0.6" fill="currentColor" stroke="none" />
    </svg>
  );
}

export default function ConsumerLayout() {
  return (
    <div className="consumer-shell">
      <div className="consumer-phone">
        <header className="consumer-header">
          <div className="consumer-header-left">
            <IconHexagon width={34} height={34} className="consumer-logo" />
            <div>
              <div className="consumer-title">LGRRS</div>
              <div className="consumer-subtitle">Lagos LGA Receipt Reward Scheme</div>
            </div>
          </div>
          <span className="consumer-lang">EN <IconChevronDown width={14} height={14} /></span>
        </header>

        <main className="consumer-content">
          <Outlet />
        </main>

        <nav className="consumer-nav">
          {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
            <NavLink key={to} to={to} end={end} className={({ isActive }) => `consumer-nav-item${isActive ? " active" : ""}`}>
              <Icon width={20} height={20} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
      </div>
    </div>
  );
}

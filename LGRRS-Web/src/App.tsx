import { useState } from "react";
import ThemeToggle from "./components/ThemeToggle";
import MerchantSettingsPage from "./pages/merchant/MerchantSettingsPage";
import CatalogPage from "./pages/merchant/CatalogPage";
import ReceiptWalletPage from "./pages/consumer/ReceiptWalletPage";
import PaperReceiptClaimPage from "./pages/consumer/PaperReceiptClaimPage";
import ReportSalePage from "./pages/consumer/ReportSalePage";
import AdminSaleReportsPage from "./pages/admin/AdminSaleReportsPage";
import { Link, Navigate, Route, Routes, useLocation } from "react-router-dom";
import { loadStoredSession, persistSession, SessionContext, useSession, type Session } from "./state/session";
import CheckTicketPage from "./pages/consumer/CheckTicketPage";
import ConsumerLoginPage from "./pages/consumer/ConsumerLoginPage";
import ConsumerLayout from "./pages/consumer/ConsumerLayout";
import ConsumerPlaceholderPage from "./pages/consumer/ConsumerPlaceholderPage";
import MerchantLoginPage from "./pages/merchant/MerchantLoginPage";
import MerchantRegisterPage from "./pages/merchant/MerchantRegisterPage";
import MerchantLayout from "./pages/merchant/MerchantLayout";
import MerchantDashboardPage from "./pages/merchant/MerchantDashboardPage";
import IssueReceiptPage from "./pages/merchant/IssueReceiptPage";
import ReceiptHistoryPage from "./pages/merchant/ReceiptHistoryPage";
import AdminLoginPage from "./pages/admin/AdminLoginPage";
import AdminLayout from "./pages/admin/AdminLayout";
import AdminOverviewPage from "./pages/admin/AdminOverviewPage";
import AdminReceiptsPage from "./pages/admin/AdminReceiptsPage";
import AdminDrawsPage from "./pages/admin/AdminDrawsPage";
import AdminMerchantsPage from "./pages/admin/AdminMerchantsPage";
import AdminMerchantDetailPage from "./pages/admin/AdminMerchantDetailPage";
import AdminFraudPage from "./pages/admin/AdminFraudPage";
import AdminAuditLogPage from "./pages/admin/AdminAuditLogPage";
import AdminPlaceholderPage from "./pages/admin/AdminPlaceholderPage";

function Landing() {
  return (
    <div className="page-card">
      <h1>LGRRS Prototype</h1>
      <p className="muted">Ask for a receipt. Make every purchase visible. Get a chance to win.</p>
      <p>Merchants record sales, customers receive private receipts, and the LGA can follow up on missing or incorrect receipts.</p>
      <nav className="landing-links">
        <Link to="/check/login">Customer: My Receipts &amp; Rewards</Link>
        <Link to="/merchant/login">Merchant Portal</Link>
        <Link to="/admin/login">LGA Administrator</Link>
      </nav>
    </div>
  );
}

const NO_CHROME_PATHS = new Set(["/merchant/login", "/merchant/register", "/check/login", "/admin/login"]);

function AppShell() {
  const { session } = useSession();
  const location = useLocation();
  const isMerchantPortal = location.pathname.startsWith("/merchant") && !NO_CHROME_PATHS.has(location.pathname);
  const isConsumerApp = location.pathname.startsWith("/check") && !NO_CHROME_PATHS.has(location.pathname);
  const isAdminPortal = location.pathname.startsWith("/admin") && !NO_CHROME_PATHS.has(location.pathname);
  const hideChrome = isMerchantPortal || isConsumerApp || isAdminPortal || NO_CHROME_PATHS.has(location.pathname);

  const routes = (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/check/login" element={<ConsumerLoginPage />} />
      <Route path="/check" element={<ConsumerLayout />}>
        <Route index element={<CheckTicketPage />} />
        <Route path="entries" element={<ReceiptWalletPage />} />
        <Route path="claim" element={<PaperReceiptClaimPage />} />
        <Route path="report" element={<ReportSalePage />} />
        <Route path="winners" element={<ConsumerPlaceholderPage title="Winners" />} />
        <Route path="help" element={<ConsumerPlaceholderPage title="Help" />} />
      </Route>
      <Route path="/merchant/login" element={<MerchantLoginPage />} />
      <Route path="/merchant/register" element={<MerchantRegisterPage />} />
      <Route path="/merchant" element={<MerchantLayout />}>
        <Route index element={<MerchantDashboardPage />} />
        <Route path="issue-receipt" element={<IssueReceiptPage />} />
        <Route path="customers" element={<Navigate to="/merchant" replace />} />
        <Route path="receipts" element={<ReceiptHistoryPage />} />
        <Route path="settings" element={<MerchantSettingsPage />} />
        <Route path="catalog" element={<CatalogPage />} />
      </Route>
      <Route path="/admin/login" element={<AdminLoginPage />} />
      <Route path="/admin" element={<AdminLayout />}>
        <Route index element={<AdminOverviewPage />} />
        <Route path="receipts" element={<AdminReceiptsPage />} />
        <Route path="merchants" element={<AdminMerchantsPage />} />
        <Route path="merchants/:merchantId" element={<AdminMerchantDetailPage />} />
        <Route path="residents" element={<Navigate to="/admin" replace />} />
        <Route path="draws" element={<AdminDrawsPage />} />
        <Route path="fraud" element={<AdminFraudPage />} />
        <Route path="reports" element={<AdminSaleReportsPage />} />
        <Route path="audit-log" element={<AdminAuditLogPage />} />
        <Route path="settings" element={<AdminPlaceholderPage title="Settings" />} />
      </Route>
    </Routes>
  );

  if (hideChrome) {
    return routes;
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <Link to="/" className="brand">LGRRS</Link>
        {session && <span className="session-badge">{session.role} · {session.displayName}</span>}
      </header>
      <main>{routes}</main>
    </div>
  );
}

export default function App() {
  const [session, setSessionState] = useState<Session | null>(() => loadStoredSession());

  const setSession = (next: Session | null) => {
    setSessionState(next);
    persistSession(next);
  };

  return (
    <SessionContext.Provider value={{ session, setSession }}>
      <ThemeToggle />
      <AppShell />
    </SessionContext.Provider>
  );
}

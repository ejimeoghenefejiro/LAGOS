import { useState } from "react";
import { Link, Navigate, useSearchParams } from "react-router-dom";
import { api, ApiError } from "../../api/client";
import type { ClaimSubmittedResponse, PublicTicketCheckResponse } from "../../api/types";
import { useSession } from "../../state/session";
import { Roles } from "../../state/roles";
import {
  IconCalendar,
  IconCheckCircleFilled,
  IconCoin,
  IconGift,
  IconInfoCircle,
  IconLock,
  IconMapPin,
  IconScan,
  IconStore,
  IconTicket,
  IconTrophy,
  IconUserCircle
} from "../../components/icons";

const BANKS = ["Access Bank", "GTBank", "Zenith Bank", "First Bank", "UBA", "Fidelity Bank", "Union Bank", "Sterling Bank"];

function formatDate(iso?: string | null): string {
  if (!iso) return "-";
  return new Date(iso).toLocaleString(undefined, { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function formatShortDate(iso?: string | null): string {
  if (!iso) return "-";
  return new Date(iso).toLocaleDateString(undefined, { day: "2-digit", month: "short", year: "numeric" });
}

function resultTitle(result: PublicTicketCheckResponse): { title: string; valid: boolean } {
  if (!result.valid) return { title: "RECEIPT NOT FOUND", valid: false };
  if (result.status === "VOIDED_OR_REFUNDED") return { title: "RECEIPT NOT ELIGIBLE", valid: false };
  if (result.status === "UNDER_REVIEW") return { title: "RECEIPT UNDER REVIEW", valid: false };
  return { title: "RECEIPT IS VALID", valid: true };
}

export default function CheckTicketPage() {
  const { session } = useSession();
  const [searchParams] = useSearchParams();
  const [ticketId, setTicketId] = useState(() => searchParams.get("receipt") ?? "");
  const [result, setResult] = useState<PublicTicketCheckResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [showClaimForm, setShowClaimForm] = useState(false);
  const [fullName, setFullName] = useState("");
  const [claimPhone, setClaimPhone] = useState("");
  const [bankName, setBankName] = useState("");
  const [accountNumber, setAccountNumber] = useState("");
  const [claim, setClaim] = useState<ClaimSubmittedResponse | null>(null);
  const [claimError, setClaimError] = useState<string | null>(null);
  const [claimSubmitting, setClaimSubmitting] = useState(false);

  if (!session || session.role !== Roles.Consumer) {
    return <Navigate to="/check/login" replace />;
  }

  const checkTicket = async () => {
    setError(null);
    setResult(null);
    setClaim(null);
    setShowClaimForm(false);
    setLoading(true);
    try {
      const response = await api.get<PublicTicketCheckResponse>(
        `/api/public/receipts/${ticketId.trim()}/check`,
        session.accessToken
      );
      setResult(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong. Try again.");
    } finally {
      setLoading(false);
    }
  };

  const submitClaim = async () => {
    setClaimError(null);
    setClaimSubmitting(true);
    try {
      const response = await api.post<ClaimSubmittedResponse>(
        "/api/public/prize-claims",
        { receiptId: ticketId.trim(), fullName, bankName, accountNumber },
        session.accessToken
      );
      setClaim(response);
    } catch (err) {
      setClaimError(err instanceof ApiError ? err.message : "Could not submit claim.");
    } finally {
      setClaimSubmitting(false);
    }
  };

  const status = result ? resultTitle(result) : null;

  return (
    <div className="consumer-page-stack">
      <Link to="/check/entries">My receipts →</Link>
      <Link to="/check/report">Missing or incorrect receipt? Report a purchase</Link>
      <h1 className="consumer-page-title">Check Your Receipt</h1>
      <p className="consumer-page-subtitle">Enter your Receipt ID to check if it is valid and see if you are a winner.</p>

      <div className="consumer-search-row">
        <div className="consumer-search-input">
          <IconScan width={18} height={18} />
          <input placeholder="Enter Receipt ID" value={ticketId} onChange={(e) => setTicketId(e.target.value)} />
        </div>
        <button className="consumer-button" onClick={checkTicket} disabled={!ticketId.trim() || loading}>
          {loading ? "Checking..." : "Check Now"}
        </button>
      </div>
      <div className="consumer-hint-row">
        <span className="consumer-example">Example: LGR-IKJ-20260829-X8F92K</span>
        <span className="consumer-link"><IconInfoCircle width={13} height={13} /> How it works</span>
      </div>

      {error && <p className="consumer-error">{error}</p>}

      {result && status && (
        <div className={`consumer-result-card${status.valid ? " valid" : " invalid"}`}>
          <div className="consumer-result-icon">
            <IconCheckCircleFilled width={26} height={26} />
          </div>
          <h3 className="consumer-result-title">{status.title}</h3>
          <p className="consumer-result-desc">
            {status.valid
              ? `This receipt was issued by a registered business in ${result.lgaName ?? "Ikeja"} Local Government.`
              : "This receipt could not be validated. Check the ID and try again."}
          </p>

          {status.valid && (
            <>
              <hr className="consumer-divider" />
              <div className="consumer-detail-list">
                <div className="consumer-detail-row">
                  <IconCalendar width={16} height={16} />
                  <span>Receipt Date</span>
                  <strong>{formatDate(result.receiptDate)}</strong>
                </div>
                <div className="consumer-detail-row">
                  <IconStore width={16} height={16} />
                  <span>Business</span>
                  <strong>{result.merchantName ?? "-"}</strong>
                </div>
                <div className="consumer-detail-row">
                  <IconMapPin width={16} height={16} />
                  <span>Location</span>
                  <strong>{result.lgaName ? `${result.lgaName} Local Government, Lagos` : "-"}</strong>
                </div>
                <div className="consumer-detail-row">
                  <IconCoin width={16} height={16} />
                  <span>Amount</span>
                  <strong>₦{result.amount?.toLocaleString() ?? "-"}</strong>
                </div>
                <div className="consumer-detail-row">
                  <IconTicket width={16} height={16} />
                  <span>Entry ID</span>
                  <strong>{result.entryRef ?? "-"}</strong>
                </div>
              </div>
            </>
          )}
        </div>
      )}

      {result?.winner && (
        <div className="consumer-winner-banner">
          <div className="consumer-winner-head">
            <div className="consumer-trophy-circle">
              <IconTrophy width={22} height={22} />
            </div>
            <div>
              <div className="consumer-winner-title">CONGRATULATIONS!</div>
              <div className="consumer-winner-sub">
                You are a winner for the {result.drawName ?? "current draw"}.
              </div>
            </div>
            <IconGift width={64} height={64} className="consumer-gift-illustration" />
          </div>

          <div className="consumer-prize-row">
            <div className="consumer-prize-box">
              <span>Prize Won</span>
              <strong>₦{result.prizeAmount?.toLocaleString()}</strong>
            </div>
            {!claim && result.claimEligible && (
              <button className="consumer-button" onClick={() => setShowClaimForm(true)}>Claim Prize</button>
            )}
          </div>
          <p className="consumer-winner-deadline">Claim your prize before {formatShortDate(result.claimDeadline)}</p>
          {!result.claimEligible && <p>This prize has already been claimed or the claim window is closed.</p>}
        </div>
      )}

      {result && !result.winner && <p role="status">{
        result.drawStatus === "COMPLETED" ? "This draw is complete. Your receipt was not selected."
        : result.drawStatus === "PENDING" ? "Your receipt is entered. The draw is pending."
        : "This receipt has no eligible entry in the current draw."
      }</p>}

      {showClaimForm && !claim && result?.claimEligible && (
        <div className="consumer-claim-card">
          <div className="consumer-claim-header">
            <span className="consumer-claim-icon"><IconUserCircle width={18} height={18} /></span>
            <h3>Claim Your Prize</h3>
          </div>
          <p className="consumer-muted">Please provide your details below to claim your prize.</p>

          <label className="consumer-field-label">
            Full Name
            <input className="consumer-input" placeholder="Enter your full name" value={fullName} onChange={(e) => setFullName(e.target.value)} />
          </label>
          <label className="consumer-field-label">
            Phone Number
            <input className="consumer-input" placeholder="Enter your phone number" value={claimPhone} onChange={(e) => setClaimPhone(e.target.value)} />
          </label>
          <label className="consumer-field-label">
            Bank Name
            <select className="consumer-input" value={bankName} onChange={(e) => setBankName(e.target.value)}>
              <option value="">Select your bank</option>
              {BANKS.map((b) => <option key={b} value={b}>{b}</option>)}
            </select>
          </label>
          <label className="consumer-field-label">
            Account Number
            <input className="consumer-input" placeholder="Enter your account number" value={accountNumber} onChange={(e) => setAccountNumber(e.target.value)} />
          </label>

          <button
            className="consumer-button full-width"
            onClick={submitClaim}
            disabled={claimSubmitting || !fullName.trim() || !bankName || !accountNumber.trim()}
          >
            {claimSubmitting ? "Submitting..." : "Submit Claim"}
          </button>
          <p className="consumer-secure-note"><IconLock width={13} height={13} /> Your information is secure and will only be used to process your prize claim.</p>

          {claimError && <p className="consumer-error">{claimError}</p>}
        </div>
      )}

      {claim && (
        <div className="consumer-claim-card">
          <p>Claim submitted. Reference: <strong>{claim.claimRef}</strong></p>
          <p className="consumer-muted">Status: {claim.status}</p>
        </div>
      )}

      <div className="consumer-info-box">
        <IconInfoCircle width={18} height={18} />
        <div>
          <strong>Important</strong>
          <p>Prizes can only be claimed by the owner of the phone number used when verifying the receipt.</p>
        </div>
      </div>
    </div>
  );
}

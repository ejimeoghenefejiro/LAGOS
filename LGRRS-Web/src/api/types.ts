export interface TokenResponse {
  accessToken: string;
  role: string;
  displayName: string;
}
export interface CatalogItem {
  catalogItemId: string;
  name: string;
  kind: string;
  price: number;
  isActive: boolean;
}

export interface MerchantSummary {
  lgrrsSystemId?: string | null;
  merchantId: string;
  businessName: string;
  businessType: string;
  lgaCode: string;
  status: string;
}

export interface ReceiptHistoryItem {
  receiptRefMasked: string;
  customerName?: string | null;
  customerPhoneMasked: string;
  itemService: string;
  amount: number;
  deliveryStatus: string;
  channels: string[];
  transactionDate: string;
}

export interface ClaimSubmittedResponse {
  claimRef: string;
  status: string;
}

export interface PublicTicketCheckResponse {
  valid: boolean;
  status?: string | null;
  drawStatus?: string | null;
  winner: boolean;
  prizeAmount?: number | null;
  claimEligible: boolean;
  merchantName?: string | null;
  lgaName?: string | null;
  receiptDate?: string | null;
  amount?: number | null;
  entryRef?: string | null;
  drawName?: string | null;
  claimDeadline?: string | null;
}

export interface AdminDashboard {
  isDemoData: boolean;
  verifiedReceipts: number;
  participatingMerchants: number;
  totalTransactionValue: number;
  rewardEntries: number;
  fraudAttemptsBlocked: number;
  prizesWon: number;
  currentPrizePool: number;
}

export interface KpiMetric {
  value: number;
  weekOverWeekChangePercent?: number | null;
}

export interface WeeklyPoint {
  weekLabel: string;
  count: number;
}

export interface TopMerchantItem {
  merchantId: string;
  businessName: string;
  lgaCode: string;
  receiptsIssued: number;
}

export interface FraudRiskSummary {
  high: number;
  medium: number;
  low: number;
}

export interface AdminOverview {
  isDemoData: boolean;
  verifiedReceipts: KpiMetric;
  participatingMerchants: KpiMetric;
  uniqueResidents: KpiMetric;
  totalTransactionValue: KpiMetric;
  rewardEntriesThisWeek: KpiMetric;
  fraudAttemptsBlocked: KpiMetric;
  prizesWonThisWeek: KpiMetric;
  currentPrizePool: number;
  drawClosesInDays?: number | null;
  receiptTrend: WeeklyPoint[];
  topMerchants: TopMerchantItem[];
  fraudRisk: FraudRiskSummary;
}

export interface CurrentDraw {
  lgaCode: string | null;
  drawPeriodId: string;
  type: string;
  startDate: string;
  endDate: string;
  drawDate?: string | null;
  status: string;
  prizeBudget: number;
  eligibleEntries: number;
}

export interface DrawWinnerItem {
  drawResultId: string;
  prizeTier: string;
  prizeAmount: number;
  receiptRefMasked: string;
  merchantName: string;
  lgaCode: string;
  drawTimestamp: string;
  hasClaim: boolean;
}

export interface RunDrawResponse {
  drawPeriodId: string;
  candidateSetCount: number;
  candidateSetHash: string;
  winners: DrawWinnerItem[];
}

export interface AuditEventItem {
  actorName?: string | null;
  actorRole?: string | null;
  eventId: string;
  eventType: string;
  entityType: string;
  entityId?: string | null;
  timestamp: string;
  summary: string;
  statusLabel: string;
  statusTone: string;
}

export interface AdminMerchant {
  merchantId: string;
  businessName: string;
  businessType: string;
  lgaCode: string;
  status: string;
}

export interface FraudFlagItem {
  flagId: string;
  entityType: string;
  entityId: string;
  ruleCode: string;
  severity: string;
  status: string;
  createdAt: string;
}

export interface AdminMerchantDetail {
  businessAddress?: string | null;
  phoneNumber: string;
  reviewReason?: string | null;
  reviewedAt?: string | null;
  merchantId: string;
  businessName: string;
  businessType: string;
  lgaCode: string;
  lgrrsSystemId?: string | null;
  status: string;
  createdAt: string;
  verifiedAt?: string | null;
  receiptsIssued: number;
  salesValue: number;
  averageSale: number;
  rewardEntries: number;
}

export interface AdminMerchantReceiptItem {
  receiptRefMasked: string;
  customerName?: string | null;
  customerPhoneMasked: string;
  itemService: string;
  amount: number;
  status: string;
  deliveryStatus: string;
  transactionDate: string;
}

export interface AdminReceiptItem {
  receiptRefMasked: string;
  merchantName: string;
  lgaCode: string;
  customerName?: string | null;
  customerPhoneMasked: string;
  itemService: string;
  amount: number;
  status: string;
  transactionDate: string;
}

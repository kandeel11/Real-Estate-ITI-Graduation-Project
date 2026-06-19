export type PaymentStatus = 'Pending' | 'Paid' | 'Late' | 'Overdue' | 'Refunded';
export type PaymentMethod = 'BankTransfer' | 'Stripe' | 'Other';

export interface RentPayment {
  id: string;
  leaseId: string;
  amount: number;
  dueDate: string;
  paidAt?: string | null;
  status: PaymentStatus;
  paymentMethod?: PaymentMethod | null;
  transactionId?: string | null;
  lateFee?: number;
  aiPredictedLate?: boolean;
  receiptUrl?: string | null;
  notes?: string | null;
  createdAt?: string;
  updatedAt?: string;
  // enrichment fields — now returned by all endpoints
  propertyId?: string;
  tenantId?: string;
  landlordId?: string;
  tenantName?: string;
  landlordName?: string;
  propertyTitle?: string;
}

export interface ProcessPaymentRequest {
  paymentMethod: PaymentMethod;
  cardToken?: string | null;
}

export interface SubscriptionCheckoutRequest {
  userId: string;
  subscriptionId?: string | null;
  planId: string;
  billingCycle: 0 | 1; // 0 = monthly, 1 = annual
  successRedirectUrl: string;
  cancelRedirectUrl: string;
}

export interface SubscriptionCheckoutResponse {
  paymentId: string;
  checkoutUrl: string;
  checkoutSessionId: string;
}

export interface ProcessPaymentResponse {
  isSuccess: boolean;
  transactionId?: string;
  receiptUrl?: string;
  errorMessage?: string;
}

export interface GenerateMonthlyResponse {
  generatedCount: number;
  skippedCount: number;
  errors: string[];
}

// Payment schedule (from /api/leases/{id}/payment-schedule)
export interface PaymentPeriod {
  periodNumber: number;
  periodStart: string;
  periodEnd: string;
  dueDate: string;
  amount: number;
  isCurrent: boolean;
  isNext: boolean;
  isPast: boolean;
}

export interface PaymentScheduleSummary {
  totalInstallments: number;
  pastInstallments: number;
  currentInstallments: number;
  futureInstallments: number;
  totalAmount: number;
  amountDueSoFar: number;
  amountRemaining: number;
}

/** Shape returned by GET /api/brokers/me/payments/schedule and GET /api/companies/mine/payments/schedule */
export interface BrokerLeaseSchedule {
  leaseId: string;
  propertyId: string;
  propertyTitle: string;
  tenantId: string;
  startDate: string;
  endDate: string;
  schedule: PaymentSchedule;
}

export interface PaymentSchedule {
  leaseId: string;
  installmentAmount: number;
  paymentFrequency: string;
  totalRentAmount: number;
  durationInMonths: number;
  startDate: string;
  endDate: string;
  paymentDayOfMonth: number;
  currentPeriod: PaymentPeriod | null;
  nextPeriod: PaymentPeriod | null;
  allPeriods: PaymentPeriod[];
  summary: PaymentScheduleSummary;
}

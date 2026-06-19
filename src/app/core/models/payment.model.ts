export type PaymentStatus = 'Pending' | 'Paid' | 'Overdue' | 'Failed' | 'Refunded';
export type PaymentMethod = 'cash' | 'card' | 'banktransfer' | 'wallet' | 'stripe';

export interface RentPayment {
  id: string;
  amount: number;
  commissionAmount: number;
  status: PaymentStatus;
  dueDate: string;
  paidAt?: string;
  paymentType: string;
  leaseId: string;
  tenantId: string;
  landlordId: string;
  tenantName?: string;
  landlordName?: string;
  propertyTitle?: string;
}

export interface ProcessPaymentRequest {
  paymentMethod: PaymentMethod;
  cardToken?: string;
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

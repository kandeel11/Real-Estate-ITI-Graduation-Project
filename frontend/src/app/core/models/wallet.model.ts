export interface WalletBalance {
  ownerId: string;
  balance: number;
  currency: string;
}

export type LedgerEntryType = 'Credit' | 'Debit';

export interface LedgerEntry {
  type: LedgerEntryType;
  amount: number;
  description: string;
  occurredAt: string;
}

export interface LedgerResponse {
  items: LedgerEntry[];
  total: number;
}

export type WithdrawalStatus = 'Pending' | 'Completed' | 'Rejected';

export interface WithdrawalRequest {
  id: string;
  ownerId: string;
  ownerName?: string;
  amount: number;
  commissionAmount: number;
  netAmount: number;
  status: WithdrawalStatus;
  bankAccountDetails: string;
  requestedAt?: string;
  processedAt?: string;
}

export interface CreateWithdrawalRequest {
  amount: number;
  bankAccountDetails: string;
}

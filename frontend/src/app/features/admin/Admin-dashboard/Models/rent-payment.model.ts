export type PaymentStatus = 'paid' | 'pending' | 'overdue';

export interface RentPayment {
  id: string;
  propertyId: string;
  propertyTitle: string;
  tenantName: string;
  tenantAvatar?: string;
  amount: number;
  dueDate: string;
  paidDate?: string;
  status: PaymentStatus;
}

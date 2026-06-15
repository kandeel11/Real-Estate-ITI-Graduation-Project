export type AssignmentStatus =
  | 'Pending'
  | 'VendorAccepted'
  | 'InProgress'
  | 'AwaitingPayment'
  | 'Completed'
  | 'Rejected'
  | 'WaitingForVendorDecision';

export interface AssignedTicket {
  id: string;
  assignmentId: string;
  title: string;
  description: string;
  status: AssignmentStatus;
  propertyAddress?: string;
  categoryName?: string;
  categoryIcon?: string;
  createdAt: string;
  tenantName?: string;
  tenantPhone?: string;
  amount?: number;
  currency?: string;
}

export interface TicketPayment {
  id: string;
  ticketId: string;
  amount: number;
  currency: string;
  status: string;
  transactionId?: string;
  createdAt: string;
}

export interface AdminTicket {
  id: string;
  title: string;
  description: string;
  status: string;
  propertyAddress?: string;
  categoryName?: string;
  categoryIcon?: string;
  createdAt: string;
  tenantName?: string;
  companyName?: string;
  assignmentId?: string;
  amount?: number;
  currency?: string;
}

// ── Real API shape ──────────────────────────────────────────────────────────

export interface AiEvaluation {
  id: string;
  responsibleParty: string;
  category: string;
  confidenceScore: number;
  reasoning: string;
  evaluatedAt: string;
}

export interface VendorAssignment {
  id: string;
  vendorId: string;
  status: string;
  rejectionReason: string | null;
  acceptedAt: string | null;
  createdAt: string;
}

export interface TicketAttachment {
  id: string;
  fileUrl: string;
  fileType: string;
  fileSizeKb: number;
  uploadedAt: string;
}

export interface TicketComment {
  id: string;
  userId: string;
  userRole: string;
  content: string;
  isInternal: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface StatusHistoryEntry {
  fromStatus: string;
  toStatus: string;
  changedBy: string;
  notes: string | null;
  changedAt: string;
}

export interface TicketPaymentDto {
  id: string;
  amount: number;
  currency: string;
  status: string;
  paymentMethod: string | null;
  transactionId: string | null;
  paidAt: string | null;
  createdAt: string;
}

export interface TicketDetailDto {
  id: string;
  ticketNumber: string;
  propertyId: string;
  propertyAddress: string;
  tenantId: string;
  tenantEmail: string;
  tenantPhone: string;
  ownerId: string;
  title: string;
  description: string;
  priority: string; // 'Low' | 'Medium' | 'Emergency'
  status: string;   // 'AssignedToVendor' | 'VendorAccepted' | 'InProgress' | 'AwaitingPayment' | 'Completed'
  responsibleParty: string;
  resolutionMethod: string;
  aiEvaluation: AiEvaluation | null;
  vendorAssignment: VendorAssignment | null;
  attachments: TicketAttachment[];
  comments: TicketComment[];
  statusHistory: StatusHistoryEntry[];
  payment: TicketPaymentDto | null;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string | null;
}

// ── Legacy types kept for any remaining references ──────────────────────────

export type AssignmentStatus =
  | 'Pending'
  | 'VendorAccepted'
  | 'InProgress'
  | 'AwaitingPayment'
  | 'Completed'
  | 'Rejected'
  | 'WaitingForVendorDecision'
  | 'AssignedToVendor';

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

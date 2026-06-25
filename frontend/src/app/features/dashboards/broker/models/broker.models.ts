// NOTE: Property listings use the real shape from features/properties/models/property.model.ts

export interface BrokerProfile {
  id: string;
  name: string;
  email: string;
  phone: string;
  license: string;
  companyName: string;
  bio: string;
  photo: string | null;
  languages: string[];
  specializations: string[];
  rating: number;
  rankInCompany: number;
  totalBrokers: number;
}

export interface BrokerStats {
  assignedProperties: number;
  newInquiries: number;
  closedDeals: number;
  rating: number;
  rankInCompany: number;
  totalBrokers: number;
}

export interface BrokerTask {
  id: string;
  text: string;
  done: boolean;
}

export interface BrokerActivity {
  id: string;
  text: string;
  timeAgo: string;
}

export interface Lead {
  id: string;
  tenantName: string;
  tenantEmail?: string;
  tenantPhone?: string;
  propertyId?: string;
  propertyTitle: string;
  status: 'new' | 'contacted' | 'negotiating' | 'closed_won' | 'closed_lost';
  stageEnteredAt: number;
  dealValue?: string;
  notes?: string;
  /** True once a lease has been sent to the tenant from this lead (persisted across reloads). */
  hasLease?: boolean;
  /** An uploaded-but-not-yet-sent lease for this lead — offer "Resume" to finish it. */
  draftLeaseId?: string | null;
}

export interface ChatMessage {
  id: string;
  senderId: string;
  senderName: string;
  senderRole: 'broker' | 'tenant' | 'owner';
  text: string;
  sentAt: string;
}

export interface InquiryDetail {
  id: string;
  propertyId: string;
  propertyTitle: string;
  tenantId: string;
  tenantName: string;
  tenantEmail: string;
  message: string;
  status: 'new' | 'replied' | 'converted';
  createdAt: string;
  /** Preview + sort key for the chat list (falls back to the inquiry message). */
  lastMessage?: string;
  lastMessageAt?: string;
}

/** Payment cadence — mirrors the backend Document.Domain.Enums.PaymentFrequency. */
export type PaymentFrequency = 'Monthly' | 'Bimonthly' | 'Quarterly' | 'SemiAnnually' | 'Annually';

export interface LeaseAiExtraction {
  tenantName: string;
  tenantEmail: string;
  ownerName: string;
  propertyTitle: string;
  startDate: string;
  endDate: string;
  installmentAmount: number;
  paymentFrequency: PaymentFrequency;
  totalRentAmount?: number | null;
  durationInMonths?: number | null;
  deposit: number;
  paymentDayOfMonth?: number | null;
  extractionStatus: 'pending' | 'done' | 'failed';
  /** Overall confidence score (0–100). */
  confidence: number;
  /** Per-field confidence scores returned by the AI for each extracted value. */
  fieldConfidence?: Partial<{
    tenantName: number; ownerName: number;
    startDate: number; endDate: number;
    installmentAmount: number; paymentFrequency: number;
    totalRentAmount: number; deposit: number; paymentDayOfMonth: number;
  }>;
}

export interface LeaseOffer {
  id: string;
  propertyId: string;
  propertyTitle: string;
  propertyAddress?: string;
  brokerName?: string;
  tenantName?: string;
  tenantEmail?: string;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  deposit: number;
  pdfUrl?: string;
  status: 'pending_tenant' | 'accepted' | 'declined';
  sentAt: string;
}

export interface Inquiry {
  id: string;
  from: string;
  property: string;
  message: string;
  status: 'new' | 'replied' | 'converted';
  timeAgo: string;
  reply?: string;
  leadId?: string;
}

export interface PerformanceStats {
  dealsClosedThisMonth: number;
  avgResponseTimeMinutes: number;
  conversionRate: number;
  rating: number;
  ratingCount: number;
  rankInCompany: number;
  totalBrokers: string;
  monthlyDeals: { month: string; deals: number }[];
}

export interface Rating {
  id: string;
  stars: number;
  comment: string;
  reviewer: string;
}

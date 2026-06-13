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

export interface Property {
  id: string;
  title: string;
  price: string;
  location: string;
  beds: number;
  baths: number;
  owner: string;
  status: 'active' | 'inactive';
  views: number;
  inquiries: number;
  newInquiries: number;
  thumbnail: string | null;
}

export interface Lead {
  id: string;
  tenantName: string;
  propertyTitle: string;
  status: 'new' | 'contacted' | 'negotiating' | 'closed_won' | 'closed_lost';
  stageEnteredAt: number;
  dealValue?: string;
  email?: string;
  phone?: string;
  notes?: string;
}

export interface Inquiry {
  id: string;
  from: string;
  property: string;
  message: string;
  status: 'new' | 'replied';
  timeAgo: string;
  reply?: string;
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

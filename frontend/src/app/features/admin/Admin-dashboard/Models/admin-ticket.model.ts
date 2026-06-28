export interface AdminTicket {
  id: string;
  title: string;
  description: string;
  status: string;
  ticketNumber?: string;
  priority?: string;
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

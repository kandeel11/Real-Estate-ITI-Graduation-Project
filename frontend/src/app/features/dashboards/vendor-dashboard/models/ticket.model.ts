export type TicketStatus = 'open' | 'in_progress' | 'completed' | 'cancelled';
export type TicketPriority = 'low' | 'medium' | 'high' | 'urgent';
export type PaymentStatus = 'pending' | 'paid' | 'overdue';

export interface Ticket {
  id: string;
  ticketNumber: string;
  title: string;
  description: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  customerName: string;
  customerAvatar?: string;
  propertyTitle: string;
  propertyAddress: string;
  status: TicketStatus;
  priority: TicketPriority;
  price: number;
  paymentStatus: PaymentStatus;
  createdAt: string;
  scheduledAt?: string;
  completedAt?: string;
}

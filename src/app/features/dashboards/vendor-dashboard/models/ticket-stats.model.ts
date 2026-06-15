export interface TicketStats {
  totalTickets: number;
  openTickets: number;
  inProgressTickets: number;
  completedTickets: number;
  cancelledTickets: number;

  totalEarnings: number;
  monthlyEarnings: number;
  pendingPayments: number;

  earningsGrowth: number;
  averageRating: number;
}

export interface CategoryEarnings {
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  ticketsCount: number;
  totalEarnings: number;
}

export interface MonthlyEarningPoint {
  month: string;
  earnings: number;
  tickets: number;
}

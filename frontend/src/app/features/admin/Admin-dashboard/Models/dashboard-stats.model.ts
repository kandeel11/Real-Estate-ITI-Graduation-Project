export interface DashboardStats {
  totalUsers: number;
  usersGrowth: number;
  totalCompanies: number;
  companiesGrowth: number;
  pendingProperties: number;
  monthlyRevenue: number;    // total rent collected on platform
  revenueGrowth: number;
  totalWithdrawn: number;    // total completed owner withdrawals
  platformCommission: number; // total commission earned by platform
  netYield: number;          // platformCommission / monthlyRevenue * 100
}

export interface MonthlyRevenuePoint {
  month: string; // "Jan", "Feb", ...
  amount: number;
}

export interface DashboardStats {
  totalUsers: number;
  usersGrowth: number; // % change vs last month
  totalCompanies: number;
  companiesGrowth: number;
  pendingProperties: number;
  monthlyRevenue: number;
  revenueGrowth: number;
}

export interface MonthlyRevenuePoint {
  month: string; // "Jan", "Feb", ...
  amount: number;
}

export interface PricePoint {
  month: string;
  avg: number;
  median: number;
}
export interface DistrictStat {
  name: string;
  avgPrice: number;
  change: number;
  listings: number;
  type: 'up' | 'down' | 'flat';
}
export interface PropertyTypeStat {
  type: string;
  count: number;
  percent: number;
  color: string;
}
export interface MarketKpi {
  label: string;
  value: string;
  change: number;
  icon: string;
  color: string;
}
export interface DemandPoint {
  month: string;
  inquiries: number;
  views: number;
}
export interface RentVsBuyPoint {
  month: string;
  rent: number;
  buy: number;
}

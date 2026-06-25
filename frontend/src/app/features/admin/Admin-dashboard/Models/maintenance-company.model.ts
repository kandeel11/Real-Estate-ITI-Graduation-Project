import { MaintenanceCategory } from './maintenance-category.model';

export interface MaintenanceCompany {
  id: string;
  companyName: string;
  email: string;
  phone: string;
  city: string;
  serviceRadiusKm: number;
  logoUrl: string | null;
  description: string;
  isActive: boolean;
  averageRating: number;
  completedTicketsCount: number;
  activeTicketsCount: number;
  createdAt: string;
  categories: MaintenanceCategory[];
}

export interface CreateMaintenanceCompanyRequest {
  companyName: string;
  categoryIds: string[];
  email: string;
  phone: string;
  city: string;
  fullName: string;
  tempPassword: string;
  description: string;
  serviceRadiusKm: number;
  logoUrl: string;
}

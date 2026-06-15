export type UserRole =
  | 'admin'
  | 'owner'
  | 'tenant'
  | 'broker'
  | 'company_manager'
  | 'maintenance_company';

export type UserStatus = 'all' | 'active' | 'banned';

export interface AdminUser {
  id: string;
  email: string;
  fullName: string;
  phone: string;
  role: UserRole;
  companyId: string | null;
  isActive: boolean;
  isEmailVerified: boolean;
  banReason: string | null;
  bannedAt: string | null;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface UsersResponse {
  items: AdminUser[];
  total: number;
  page: number;
  pageSize: number;
}

export const ROLE_OPTIONS: { key: UserRole | 'all'; label: string; icon: string; color: string }[] =
  [
    { key: 'all', label: 'All Roles', icon: 'fa-users', color: 'indigo' },
    { key: 'admin', label: 'Admin', icon: 'fa-shield-halved', color: 'purple' },
    { key: 'owner', label: 'Owner', icon: 'fa-key', color: 'blue' },
    { key: 'tenant', label: 'Tenant', icon: 'fa-house-user', color: 'green' },
    { key: 'broker', label: 'Broker', icon: 'fa-handshake', color: 'amber' },
    { key: 'company_manager', label: 'Co. Manager', icon: 'fa-building-user', color: 'teal' },
    { key: 'maintenance_company', label: 'Maintenance', icon: 'fa-wrench', color: 'orange' },
  ];

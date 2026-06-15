export type UserRole = 'admin' | 'owner' | 'tenant' | 'agent';
export type UserStatus = 'active' | 'pending' | 'suspended';

export interface AdminUser {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role: UserRole;
  status: UserStatus;
  joinedAt: string;
  phone?: string;
}

export type CompanyStatus = 'verified' | 'pending' | 'suspended';

export interface Company {
  id: string;
  name: string;
  logo?: string;
  ownerName: string;
  email: string;
  propertiesCount: number;
  status: CompanyStatus;
  joinedAt: string;
}

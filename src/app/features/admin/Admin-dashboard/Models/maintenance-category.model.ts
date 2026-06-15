export interface MaintenanceCategory {
  id: string;
  name: string;
  icon: string;
  description: string;
  isActive: boolean;
}

export interface CreateMaintenanceCategoryRequest {
  name: string;
  icon: string;
  description: string;
}

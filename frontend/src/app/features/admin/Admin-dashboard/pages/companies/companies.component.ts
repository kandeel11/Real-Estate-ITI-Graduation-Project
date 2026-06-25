import { Component, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../../../../environments/environment';

export interface AdminCompany {
  id: string;
  managerId: string;
  name: string;
  commercialRegister: string;
  logoUrl: string | null;
  website: string | null;
  description: string | null;
  city: string;
  address: string;
  phone: string;
  email: string;
  isVerified: boolean;
  isActive: boolean;
  ratingAvg: number;
  totalRatings: number;
  createdAt: string;
}

export interface AdminBroker {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  phone: string;
  profileImageUrl: string | null;
  licenseNumber: string;
  isActive: boolean;
  closedDealsCount: number;
  avgResponseTimeMinutes: number;
  ratingAvg: number;
  totalRatings: number;
}

@Component({
  selector: 'app-admin-companies',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './companies.component.html',
  styleUrls: ['./companies.component.scss'],
})
export class CompaniesComponent {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  companies = signal<AdminCompany[]>([]);
  loading = signal(true);
  searchQuery = signal('');

  selectedCompany = signal<AdminCompany | null>(null);
  brokers = signal<AdminBroker[]>([]);
  loadingBrokers = signal(false);

  filtered = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.companies();
    return this.companies().filter(
      (c) =>
        c.name.toLowerCase().includes(q) ||
        c.email.toLowerCase().includes(q) ||
        c.city.toLowerCase().includes(q),
    );
  });

  constructor() {
    this.http.get<AdminCompany[]>(`${this.base}/companies`).subscribe({
      next: (list) => {
        this.companies.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openCompany(company: AdminCompany) {
    this.selectedCompany.set(company);
    this.brokers.set([]);
    this.loadingBrokers.set(true);
    this.http
      .get<AdminBroker[]>(`${this.base}/companies/${company.id}/brokers`)
      .subscribe({
        next: (list) => {
          this.brokers.set(list);
          this.loadingBrokers.set(false);
        },
        error: () => this.loadingBrokers.set(false),
      });
  }

  closeModal() {
    this.selectedCompany.set(null);
    this.brokers.set([]);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }
}

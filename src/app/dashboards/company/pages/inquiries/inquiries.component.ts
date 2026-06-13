/**
 * Company Inquiries History
 * Every inquiry on the company's listings, enriched with the tenant who sent
 * it, the broker who handled it, and the property — with status filter tabs.
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { CompanyService } from '../../services/company.service';
import { CompanyInquiry } from '../../../../shared/models/company.model';

type StatusFilter = 'all' | 'Pending' | 'Accepted' | 'Closed' | 'Expired';

const STATUS_BADGES: Record<string, string> = {
  Pending: 'badge bg-warning text-dark',
  Accepted: 'badge bg-success',
  Closed: 'badge bg-primary',
  Expired: 'badge bg-secondary',
};

@Component({
  selector: 'app-company-inquiries',
  standalone: true,
  imports: [DatePipe, TranslatePipe],
  templateUrl: './inquiries.component.html',
  styleUrl: './inquiries.component.css',
})
export class InquiriesComponent implements OnInit {
  private companySvc = inject(CompanyService);

  inquiries = signal<CompanyInquiry[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  statusFilter = signal<StatusFilter>('all');

  counts = computed(() => {
    const list = this.inquiries();
    return {
      all: list.length,
      Pending: list.filter((i) => i.status === 'Pending').length,
      Accepted: list.filter((i) => i.status === 'Accepted').length,
      Closed: list.filter((i) => i.status === 'Closed').length,
      Expired: list.filter((i) => i.status === 'Expired').length,
    };
  });

  filtered = computed(() => {
    const f = this.statusFilter();
    return f === 'all'
      ? this.inquiries()
      : this.inquiries().filter((i) => i.status === f);
  });

  tabs: { label: string; value: StatusFilter }[] = [
    { label: 'COMMON.ALL',               value: 'all' },
    { label: 'COMMON.PENDING',           value: 'Pending' },
    { label: 'COMPANY_INQUIRIES.ACCEPTED', value: 'Accepted' },
    { label: 'COMPANY_INQUIRIES.CLOSED',   value: 'Closed' },
    { label: 'COMPANY_INQUIRIES.EXPIRED',  value: 'Expired' },
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.companySvc.getInquiries().subscribe({
      next: (list) => {
        this.inquiries.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load inquiries. Is the backend running?');
        this.loading.set(false);
      },
    });
  }

  badgeClass(status: string): string {
    return STATUS_BADGES[status] ?? 'badge bg-secondary';
  }
}

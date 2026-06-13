import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs';
import { LeaseService, ApiLease } from '../../services/lease.service';
import { AiService } from '../../services/ai.service';

interface LeaseView {
  id: string;
  propertyId: string;
  propertyAddress: string;
  rentAmount: number;
  securityDeposit: number;
  startDate: string;
  endDate: string;
  paymentDayOfMonth: number;
  isActive: boolean;
  contractFileUrl: string;
  daysUntilExpiry: number;
}

interface ChatEntry {
  role: 'user' | 'assistant';
  content: string;
}

@Component({
  selector: 'app-my-lease',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, FormsModule],
  templateUrl: './my-lease.component.html',
  styleUrl: './my-lease.component.css',
})
export class MyLeaseComponent implements OnInit {
  private readonly leaseService = inject(LeaseService);
  private readonly aiService = inject(AiService);

  protected activeTab = signal<'active' | 'history'>('active');
  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected activeLease = signal<LeaseView | null>(null);
  protected leaseHistory = signal<LeaseView[]>([]);
  protected expandedLeaseId = signal<string | null>(null);

  protected chatMessages = signal<ChatEntry[]>([]);
  protected chatInput = '';
  protected chatLoading = signal(false);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      leases: this.leaseService.getLeases(),
      properties: this.leaseService.getRentedProperties(),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ leases, properties }) => {
          // Build leaseId → address map from rented-properties
          const addressMap = new Map<string, string>(
            properties.map(p => {
              const parts = [p.property.address, p.property.district, p.property.city].filter(Boolean);
              return [p.leaseId, parts.join(', ')];
            })
          );

          const views = leases.map(l => this.toView(l, addressMap.get(l.id)));
          this.leaseHistory.set(views);
          this.activeLease.set(views.find(l => l.isActive) ?? null);
        },
        error: () => this.error.set('Failed to load lease data. Please try again.'),
      });
  }

  private toView(lease: ApiLease, address?: string): LeaseView {
    const end = new Date(lease.endDate).getTime();
    const daysUntilExpiry = Math.max(0, Math.ceil((end - Date.now()) / 86_400_000));

    return {
      id: lease.id,
      propertyId: lease.propertyId,
      propertyAddress: address ?? `Property ${lease.propertyId.slice(-6)}`,
      rentAmount: lease.rentAmount,
      securityDeposit: lease.securityDeposit,
      startDate: lease.startDate,
      endDate: lease.endDate,
      paymentDayOfMonth: lease.paymentDayOfMonth,
      isActive: lease.isActive,
      contractFileUrl: lease.contractFileUrl,
      daysUntilExpiry,
    };
  }

  leaseElapsedPercent(lease: LeaseView): number {
    const start = new Date(lease.startDate).getTime();
    const end = new Date(lease.endDate).getTime();
    return Math.min(100, Math.max(0, Math.round(((Date.now() - start) / (end - start)) * 100)));
  }

  ordinalSuffix(n: number): string {
    if (n === 1) return 'st';
    if (n === 2) return 'nd';
    if (n === 3) return 'rd';
    return 'th';
  }

  downloadContract(lease: LeaseView): void {
    window.open(this.leaseService.contractUrl(lease.contractFileUrl), '_blank');
  }

  toggleExpand(id: string): void {
    this.expandedLeaseId.update(cur => (cur === id ? null : id));
  }

  sendChatMessage(): void {
    const text = this.chatInput.trim();
    if (!text || this.chatLoading()) return;

    this.chatMessages.update(msgs => [...msgs, { role: 'user', content: text }]);
    this.chatInput = '';
    this.chatLoading.set(true);

    const lease = this.activeLease();
    this.aiService
      .chatRag({ message: text, leaseId: lease?.id })
      .pipe(finalize(() => this.chatLoading.set(false)))
      .subscribe({
        next: res => this.chatMessages.update(msgs => [...msgs, { role: 'assistant', content: res.message }]),
        error: () =>
          this.chatMessages.update(msgs => [
            ...msgs,
            { role: 'assistant', content: 'Sorry, I could not process your request. Please try again.' },
          ]),
      });
  }
}

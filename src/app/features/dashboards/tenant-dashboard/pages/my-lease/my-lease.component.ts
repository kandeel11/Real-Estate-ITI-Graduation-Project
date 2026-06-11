import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { Lease, LeaseService } from '../../services/lease.service';
import { AiService } from '../../services/ai.service';

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
  protected activeLease = signal<Lease | null>(null);
  protected leaseHistory = signal<Lease[]>([]);
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

    this.leaseService
      .getActiveLease()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: lease => this.activeLease.set(lease),
        error: () => this.error.set('Failed to load lease data. Please try again.'),
      });

    this.leaseService.getLeaseHistory().subscribe({
      next: history => this.leaseHistory.set(history),
    });
  }

  leaseElapsedPercent(lease: Lease): number {
    const start = new Date(lease.startDate).getTime();
    const end = new Date(lease.endDate).getTime();
    const now = Date.now();
    return Math.min(100, Math.max(0, Math.round(((now - start) / (end - start)) * 100)));
  }

  downloadContract(): void {
    const lease = this.activeLease();
    if (!lease) return;
    this.leaseService.downloadContract(lease.id).subscribe({
      next: ({ downloadUrl }) => window.open(downloadUrl, '_blank'),
    });
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

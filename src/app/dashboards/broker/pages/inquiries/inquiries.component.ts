import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { BrokerDataService } from '../../services/broker-data.service';
import { Inquiry, InquiryDetail } from '../../models/broker.models';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { ChatModalComponent } from '../../components/chat-modal/chat-modal.component';

@Component({
  selector: 'app-broker-inquiries',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, ChatModalComponent],
  templateUrl: './inquiries.component.html',
  styleUrl: './inquiries.component.css',
})
export class InquiriesComponent implements OnInit {
  private readonly dataService = inject(BrokerDataService);
  private readonly toast = inject(ToastService);

  readonly inquiries = signal<Inquiry[]>([]);
  /** Full details kept alongside the compact list so the chat modal has the rich shape. */
  private readonly details = signal<InquiryDetail[]>([]);
  readonly chatInquiry = signal<InquiryDetail | null>(null);
  readonly loading = signal(true);
  readonly filter = signal<'all' | 'new' | 'replied'>('all');
  readonly search = signal('');
  readonly sendingId = signal<string | null>(null);
  readonly convertingId = signal<string | null>(null);
  replyText: Record<string, string> = {};

  readonly filtered = computed(() => {
    const f = this.filter();
    const q = this.search().toLowerCase();
    return this.inquiries().filter(i => {
      const matchFilter = f === 'all' || i.status === f;
      const matchSearch = !q ||
        i.from.toLowerCase().includes(q) ||
        i.property.toLowerCase().includes(q);
      return matchFilter && matchSearch;
    });
  });

  ngOnInit(): void {
    this.dataService.getInquiryDetails().subscribe({
      next: list => {
        this.details.set(list);
        this.inquiries.set(list.map(i => ({
          id: i.id,
          from: i.tenantName,
          property: i.propertyTitle,
          message: i.message,
          status: i.status === 'new' ? 'new' as const : 'replied' as const,
          timeAgo: i.createdAt,
        })));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openChat(inquiry: Inquiry): void {
    const detail = this.details().find(d => d.id === inquiry.id);
    if (detail) this.chatInquiry.set(detail);
  }

  closeChat(): void {
    this.chatInquiry.set(null);
  }

  onChatLeadConverted(): void {
    const id = this.chatInquiry()?.id;
    this.chatInquiry.set(null);
    if (id) {
      this.inquiries.update(list => list.filter(i => i.id !== id));
      this.toast.success('Converted to lead.');
    }
  }

  sendReply(inquiry: Inquiry): void {
    const reply = this.replyText[inquiry.id]?.trim();
    if (!reply) return;
    this.sendingId.set(inquiry.id);
    this.dataService.replyToInquiry(inquiry.id, reply).subscribe({
      next: () => {
        this.inquiries.update(list =>
          list.map(i => i.id === inquiry.id ? { ...i, reply, status: 'replied' as const } : i));
        this.replyText[inquiry.id] = '';
        this.sendingId.set(null);
      },
      error: () => this.sendingId.set(null),
    });
  }

  convertToLead(inquiry: Inquiry): void {
    this.convertingId.set(inquiry.id);
    this.dataService.convertToLead(inquiry.id).subscribe({
      next: () => {
        this.inquiries.update(list => list.filter(i => i.id !== inquiry.id));
        this.convertingId.set(null);
        this.toast.success('Converted to lead.');
      },
      error: (err) => {
        this.convertingId.set(null);
        this.toast.error(err?.error?.error || 'Could not convert this inquiry to a lead.');
      },
    });
  }
}

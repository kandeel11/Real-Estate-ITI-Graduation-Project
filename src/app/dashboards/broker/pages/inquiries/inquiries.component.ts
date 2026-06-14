import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { BrokerDataService } from '../../services/broker-data.service';
import { Inquiry } from '../../models/broker.models';

@Component({
  selector: 'app-broker-inquiries',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './inquiries.component.html',
  styleUrl: './inquiries.component.css',
})
export class InquiriesComponent implements OnInit {
  private readonly dataService = inject(BrokerDataService);

  readonly inquiries = signal<Inquiry[]>([]);
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
    this.dataService.getInquiries().subscribe({
      next: list => { this.inquiries.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
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
      },
      error: () => this.convertingId.set(null),
    });
  }
}

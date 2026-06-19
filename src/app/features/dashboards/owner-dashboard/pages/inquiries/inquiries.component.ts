import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { OwnerDataService } from '../../services/owner-data.service';
import { Inquiry } from '../../models/owner.models';
import { ToastService } from '../../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-owner-inquiries',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './inquiries.component.html',
  styleUrl: './inquiries.component.css',
})
export class InquiriesComponent implements OnInit {
  private readonly dataService = inject(OwnerDataService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  /** Where the "Chat" button sends the user (overridden in the owner copy). */
  protected readonly messagesRoute = '/dashboard/owner/messages';

  readonly inquiries = signal<Inquiry[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<'all' | 'new' | 'replied'>('all');
  readonly search = signal('');
  readonly sendingId = signal<string | null>(null);
  readonly convertingId = signal<string | null>(null);
  /** Property titles whose group is collapsed (all expanded by default). */
  readonly collapsed = signal<Set<string>>(new Set());
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

  /** Inquiries grouped under their property — each property is a collapsible section. */
  readonly groups = computed(() => {
    const map = new Map<string, Inquiry[]>();
    for (const i of this.filtered()) {
      const key = i.property || '—';
      const arr = map.get(key);
      if (arr) arr.push(i); else map.set(key, [i]);
    }
    return [...map.entries()]
      .map(([property, items]) => ({
        property,
        items,
        newCount: items.filter(x => x.status === 'new').length,
      }))
      .sort((a, b) => b.items.length - a.items.length);
  });

  isOpen(property: string): boolean {
    return !this.collapsed().has(property);
  }

  toggleGroup(property: string): void {
    this.collapsed.update(set => {
      const next = new Set(set);
      next.has(property) ? next.delete(property) : next.add(property);
      return next;
    });
  }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase() || '?';
  }

  ngOnInit(): void {
    this.dataService.getInquiryDetails().subscribe({
      next: list => {
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

  /** Open the full conversation in the Messages tab, pre-selected. */
  openChat(inquiry: Inquiry): void {
    this.router.navigate([this.messagesRoute], { queryParams: { inquiry: inquiry.id } });
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

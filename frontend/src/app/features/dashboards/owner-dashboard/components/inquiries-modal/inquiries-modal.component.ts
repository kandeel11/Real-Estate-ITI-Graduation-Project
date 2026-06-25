import { Component, Input, Output, EventEmitter, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { OwnerDataService } from '../../services/owner-data.service';
import { InquiryDetail } from '../../models/owner.models';
import { Property } from '../../../../../features/properties/models/property.model';

@Component({
  selector: 'app-owner-inquiries-modal',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './inquiries-modal.component.html',
  styleUrl: './inquiries-modal.component.css',
})
export class InquiriesModalComponent implements OnInit {
  @Input({ required: true }) property!: Property;
  @Output() closed    = new EventEmitter<void>();
  @Output() openChat  = new EventEmitter<InquiryDetail>();

  private readonly svc = inject(OwnerDataService);

  loading    = signal(true);
  inquiries  = signal<InquiryDetail[]>([]);

  ngOnInit(): void {
    this.svc.getInquiriesByProperty(this.property.id).subscribe(list => {
      this.inquiries.set(list);
      this.loading.set(false);
    });
  }

  /** Called by parent after chat closes, to refresh status badges. */
  refresh(): void {
    this.svc.getInquiriesByProperty(this.property.id).subscribe(list => this.inquiries.set(list));
  }

  markConverted(inquiryId: string): void {
    this.inquiries.update(list =>
      list.map(i => i.id === inquiryId ? { ...i, status: 'converted' as const } : i)
    );
  }

  timeAgo(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const h = Math.floor(diff / 3600000);
    if (h < 1) return 'just now';
    if (h < 24) return `${h}h ago`;
    return `${Math.floor(h / 24)}d ago`;
  }
}

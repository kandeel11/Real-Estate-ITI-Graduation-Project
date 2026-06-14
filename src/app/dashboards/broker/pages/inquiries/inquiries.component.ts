import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BrokerDataService } from '../../services/broker-data.service';
import { Inquiry } from '../../models/broker.models';

type FilterType = 'all' | 'new' | 'replied';

@Component({
  selector: 'app-broker-inquiries',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styles: [
    `
      .page {
        padding: 2rem;
        max-width: 900px;
        margin: 0 auto;
      }
      .page-header {
        margin-bottom: 1.5rem;
      }
      .page-header h1 {
        font-size: 1.4rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0 0 1rem;
      }
      .filter-tabs {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
      }
      .tab {
        padding: 0.4rem 1rem;
        border-radius: 20px;
        font-size: 0.85rem;
        font-weight: 500;
        cursor: pointer;
        border: 1.5px solid #dee2e6;
        background: #fff;
        color: #6c757d;
        transition: all 0.15s;
      }
      .tab.active {
        background: #2d6a4f;
        color: #fff;
        border-color: #2d6a4f;
      }
      .tab:hover:not(.active) {
        border-color: #2d6a4f;
        color: #2d6a4f;
      }
      .inquiry-list {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
      .inquiry-card {
        background: #fff;
        border-radius: 12px;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
        padding: 1.25rem;
        border-left: 4px solid transparent;
      }
      .inquiry-card.status-new {
        border-left-color: #ef4444;
      }
      .inquiry-card.status-replied {
        border-left-color: #10b981;
      }
      .inquiry-top {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        margin-bottom: 0.5rem;
        flex-wrap: wrap;
      }
      .status-badge {
        font-size: 0.75rem;
        font-weight: 700;
        padding: 0.2rem 0.6rem;
        border-radius: 20px;
      }
      .badge-new {
        background: #fee2e2;
        color: #991b1b;
      }
      .badge-replied {
        background: #d1fae5;
        color: #065f46;
      }
      .inquiry-time {
        font-size: 0.78rem;
        color: #aaa;
      }
      .inquiry-from {
        font-size: 0.95rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0 0 0.2rem;
      }
      .inquiry-property {
        font-size: 0.82rem;
        color: #6c757d;
        margin: 0 0 0.6rem;
      }
      .inquiry-message {
        font-size: 0.88rem;
        color: #444;
        background: #f8f9fa;
        border-radius: 8px;
        padding: 0.75rem;
        margin-bottom: 0.75rem;
        line-height: 1.5;
      }
      .reply-box {
        background: #f0faf5;
        border-radius: 8px;
        padding: 0.75rem;
        margin-bottom: 0.75rem;
      }
      .reply-box p {
        font-size: 0.78rem;
        color: #888;
        margin: 0 0 0.3rem;
        font-weight: 600;
      }
      .reply-box span {
        font-size: 0.87rem;
        color: #333;
      }
      .reply-input {
        width: 100%;
        border: 1.5px solid #dee2e6;
        border-radius: 8px;
        padding: 0.6rem;
        font-size: 0.88rem;
        font-family: inherit;
        resize: vertical;
        min-height: 70px;
        box-sizing: border-box;
        margin-bottom: 0.5rem;
      }
      .reply-input:focus {
        outline: none;
        border-color: #2d6a4f;
      }
      .actions {
        display: flex;
        gap: 0.6rem;
        flex-wrap: wrap;
      }
      .btn {
        padding: 0.45rem 0.9rem;
        border-radius: 8px;
        font-size: 0.83rem;
        font-weight: 500;
        cursor: pointer;
        border: none;
      }
      .btn-primary {
        background: #2d6a4f;
        color: #fff;
      }
      .btn-primary:hover {
        background: #1b4332;
      }
      .btn-outline {
        background: #fff;
        color: #2d6a4f;
        border: 1.5px solid #2d6a4f;
      }
      .btn-outline:hover {
        background: #f0faf5;
      }
      .btn-secondary {
        background: #f8f9fa;
        color: #555;
        border: 1.5px solid #dee2e6;
      }
      .btn-secondary:hover {
        background: #e9ecef;
      }
      .skeleton {
        background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
        background-size: 200% 100%;
        animation: shimmer 1.5s infinite;
        border-radius: 8px;
      }
      @keyframes shimmer {
        0% {
          background-position: 200% 0;
        }
        100% {
          background-position: -200% 0;
        }
      }
      .empty {
        text-align: center;
        padding: 3rem;
        color: #aaa;
      }
      .empty-icon {
        font-size: 3rem;
        margin-bottom: 1rem;
      }
    `,
  ],
  template: `
    <div class="page">
      <div class="page-header">
        <h1>Inquiries ({{ filtered.length }} total, {{ newCount }} unanswered)</h1>
        <div class="filter-tabs">
          <button class="tab" [class.active]="activeFilter === 'all'" (click)="setFilter('all')">
            All
          </button>
          <button class="tab" [class.active]="activeFilter === 'new'" (click)="setFilter('new')">
            Unanswered ({{ newCount }})
          </button>
          <button
            class="tab"
            [class.active]="activeFilter === 'replied'"
            (click)="setFilter('replied')"
          >
            Replied
          </button>
        </div>
      </div>

      <!-- Skeletons -->
      @if (loading) {
        @for (i of [1, 2, 3]; track i) {
          <div class="inquiry-card" style="margin-bottom:1rem;">
            <div class="skeleton" style="height:16px; width:40%; margin-bottom:10px;"></div>
            <div class="skeleton" style="height:14px; width:60%; margin-bottom:8px;"></div>
            <div class="skeleton" style="height:60px; margin-bottom:8px;"></div>
            <div class="skeleton" style="height:32px; width:200px;"></div>
          </div>
        }

        <!-- Empty -->
      } @else if (filtered.length === 0) {
        <div class="empty">
          <div class="empty-icon">📭</div>
          <p>No inquiries found.</p>
        </div>

        <!-- List -->
      } @else {
        <div class="inquiry-list">
          @for (inq of filtered; track inq.id) {
            <div
              class="inquiry-card"
              [class.status-new]="inq.status === 'new'"
              [class.status-replied]="inq.status === 'replied'"
            >
              <div class="inquiry-top">
                <span
                  class="status-badge"
                  [class.badge-new]="inq.status === 'new'"
                  [class.badge-replied]="inq.status === 'replied'"
                >
                  {{ inq.status === 'new' ? '🔴 NEW' : '✅ Replied' }}
                </span>
                <span class="inquiry-time">{{ inq.timeAgo }}</span>
              </div>

              <p class="inquiry-from">From: {{ inq.from }}</p>
              <p class="inquiry-property">Property: {{ inq.property }}</p>
              <div class="inquiry-message">{{ inq.message }}</div>

              @if (inq.reply) {
                <div class="reply-box">
                  <p>Your reply:</p>
                  <span>{{ inq.reply }}</span>
                </div>
              }

              @if (replyingId === inq.id) {
                <textarea
                  class="reply-input"
                  [(ngModel)]="replyText"
                  placeholder="Type your reply..."
                >
                </textarea>
                <div class="actions">
                  <button class="btn btn-primary" (click)="submitReply(inq)">Send Reply</button>
                  <button class="btn btn-secondary" (click)="cancelReply()">Cancel</button>
                </div>
              } @else {
                <div class="actions">
                  <button class="btn btn-primary" (click)="startReply(inq.id)">
                    {{ inq.reply ? 'Reply Again' : 'Reply' }}
                  </button>
                  @if (inq.status === 'new') {
                    <button class="btn btn-secondary" (click)="markRead(inq)">Mark Read</button>
                  }
                  <button class="btn btn-outline" (click)="convertToLead(inq)">
                    Convert to Lead
                  </button>
                  <button class="btn btn-secondary" (click)="forward(inq)">Forward</button>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class InquiriesComponent implements OnInit {
  private brokerService = inject(BrokerDataService);
  private destroyRef = inject(DestroyRef);

  inquiries: Inquiry[] = [];
  filtered: Inquiry[] = [];
  activeFilter: FilterType = 'all';
  replyingId: string | null = null;
  replyText = '';
  loading = true;

  get newCount(): number {
    return this.inquiries.filter((i) => i.status === 'new').length;
  }

  ngOnInit() {
    this.brokerService
      .getInquiries()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => {
        this.inquiries = data;
        this.applyFilter();
        this.loading = false;
      });
  }

  setFilter(filter: FilterType) {
    this.activeFilter = filter;
    this.applyFilter();
  }

  applyFilter() {
    if (this.activeFilter === 'all') {
      this.filtered = this.inquiries;
    } else {
      this.filtered = this.inquiries.filter((i) => i.status === this.activeFilter);
    }
  }

  startReply(id: string) {
    this.replyingId = id;
    this.replyText = '';
  }

  cancelReply() {
    this.replyingId = null;
    this.replyText = '';
  }

  submitReply(inq: Inquiry) {
    if (!this.replyText.trim()) return;
    this.brokerService
      .replyToInquiry(inq.id, this.replyText)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        inq.reply = this.replyText;
        inq.status = 'replied';
        this.cancelReply();
        this.applyFilter();
      });
  }

  markRead(inq: Inquiry) {
    inq.status = 'replied';
    this.applyFilter();
  }

  convertToLead(inq: Inquiry) {
    this.brokerService
      .convertToLead(inq.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        alert(`${inq.from} has been converted to a lead.`);
      });
  }

  forward(inq: Inquiry) {
    alert(`Forwarding inquiry from ${inq.from} to another broker.`);
  }
}

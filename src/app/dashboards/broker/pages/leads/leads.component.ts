import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  DragDropModule,
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { BrokerDataService } from '../../services/broker-data.service';
import { Lead } from '../../models/broker.models';

interface KanbanColumn {
  status: Lead['status'];
  label: string;
  color: string;
  leads: Lead[];
}

@Component({
  selector: 'app-broker-leads',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule],
  styles: [
    `
      .page {
        padding: 2rem;
        max-width: 1300px;
        margin: 0 auto;
      }
      .page-header {
        margin-bottom: 1.5rem;
      }
      .page-header h1 {
        font-size: 1.4rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0;
      }
      .kanban {
        display: grid;
        grid-template-columns: repeat(5, 1fr);
        gap: 1rem;
        align-items: start;
      }
      @media (max-width: 1024px) {
        .kanban {
          grid-template-columns: repeat(3, 1fr);
        }
      }
      @media (max-width: 640px) {
        .kanban {
          grid-template-columns: 1fr;
        }
      }
      .column {
        background: #f8f9fa;
        border-radius: 12px;
        padding: 0.75rem;
        min-height: 400px;
      }
      .column-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 0.75rem;
        padding: 0 0.25rem;
      }
      .column-title {
        font-size: 0.88rem;
        font-weight: 600;
        color: #1a1a2e;
      }
      .column-count {
        font-size: 0.78rem;
        font-weight: 600;
        padding: 0.15rem 0.55rem;
        border-radius: 20px;
      }
      .lead-card {
        background: #fff;
        border-radius: 10px;
        padding: 0.9rem;
        margin-bottom: 0.6rem;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
        cursor: pointer;
        transition: box-shadow 0.15s;
        border: 1.5px solid transparent;
      }
      .lead-card:hover {
        box-shadow: 0 3px 10px rgba(0, 0, 0, 0.12);
        border-color: #2d6a4f22;
      }
      .lead-card.cdk-drag-preview {
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.18);
        border-color: #2d6a4f;
      }
      .lead-card.cdk-drag-placeholder {
        opacity: 0.3;
      }
      .lead-name {
        font-size: 0.9rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0 0 0.2rem;
      }
      .lead-property {
        font-size: 0.8rem;
        color: #6c757d;
        margin: 0 0 0.4rem;
      }
      .lead-meta {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
      .lead-time {
        font-size: 0.75rem;
        color: #aaa;
      }
      .lead-value {
        font-size: 0.78rem;
        font-weight: 600;
        color: #2d6a4f;
      }
      .cdk-drop-list-dragging .lead-card:not(.cdk-drag-placeholder) {
        transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
      }
      .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.45);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        padding: 1rem;
      }
      .modal {
        background: #fff;
        border-radius: 16px;
        padding: 1.75rem;
        width: 100%;
        max-width: 520px;
        max-height: 90vh;
        overflow-y: auto;
      }
      .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 1.25rem;
      }
      .modal-header h2 {
        font-size: 1.1rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0;
      }
      .close-btn {
        background: none;
        border: none;
        font-size: 1.4rem;
        cursor: pointer;
        color: #aaa;
        padding: 0;
        line-height: 1;
      }
      .close-btn:hover {
        color: #333;
      }
      .modal-section {
        margin-bottom: 1.25rem;
      }
      .modal-section h4 {
        font-size: 0.82rem;
        font-weight: 600;
        color: #888;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin: 0 0 0.6rem;
      }
      .info-row {
        display: flex;
        gap: 1rem;
        flex-wrap: wrap;
        font-size: 0.88rem;
        color: #333;
        margin-bottom: 0.35rem;
      }
      .stage-badge {
        display: inline-block;
        padding: 0.25rem 0.75rem;
        border-radius: 20px;
        font-size: 0.82rem;
        font-weight: 500;
      }
      .notes-area {
        width: 100%;
        border: 1.5px solid #dee2e6;
        border-radius: 8px;
        padding: 0.6rem;
        font-size: 0.88rem;
        font-family: inherit;
        resize: vertical;
        min-height: 80px;
        box-sizing: border-box;
      }
      .notes-area:focus {
        outline: none;
        border-color: #2d6a4f;
      }
      .modal-actions {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
        margin-top: 1rem;
      }
      .btn {
        padding: 0.5rem 1rem;
        border-radius: 8px;
        font-size: 0.85rem;
        font-weight: 500;
        cursor: pointer;
        border: none;
      }
      .btn-success {
        background: #2d6a4f;
        color: #fff;
      }
      .btn-success:hover {
        background: #1b4332;
      }
      .btn-danger {
        background: #fee2e2;
        color: #991b1b;
      }
      .btn-danger:hover {
        background: #fecaca;
      }
      .btn-outline {
        background: #fff;
        color: #2d6a4f;
        border: 1.5px solid #2d6a4f;
      }
      .btn-outline:hover {
        background: #f0faf5;
      }
      .btn-save {
        background: #2d6a4f;
        color: #fff;
        margin-top: 0.5rem;
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
      .empty-col {
        text-align: center;
        padding: 2rem 0.5rem;
        color: #ccc;
        font-size: 0.82rem;
      }
    `,
  ],
  template: `
    <div class="page">
      <div class="page-header">
        <h1>Leads Pipeline</h1>
      </div>

      <!-- Skeleton -->
      @if (loading) {
        <div class="kanban">
          @for (i of [1, 2, 3, 4, 5]; track i) {
            <div class="column">
              <div class="skeleton" style="height:20px; width:80%; margin-bottom:12px;"></div>
              @for (j of [1, 2]; track j) {
                <div class="skeleton" style="height:80px; margin-bottom:8px;"></div>
              }
            </div>
          }
        </div>
      } @else {
        <div class="kanban">
          @for (col of columns; track col.status) {
            <div class="column">
              <div class="column-header">
                <span class="column-title">{{ col.label }}</span>
                <span
                  class="column-count"
                  [style.background]="col.color + '22'"
                  [style.color]="col.color"
                >
                  {{ col.leads.length }}
                </span>
              </div>

              <div
                cdkDropList
                [id]="col.status"
                [cdkDropListData]="col.leads"
                [cdkDropListConnectedTo]="connectedIds"
                (cdkDropListDropped)="onDrop($event)"
                style="min-height: 60px;"
              >
                @if (col.leads.length === 0) {
                  <div class="empty-col">Drop leads here</div>
                }

                @for (lead of col.leads; track lead.id) {
                  <div class="lead-card" cdkDrag (click)="openModal(lead)">
                    <p class="lead-name">{{ lead.tenantName }}</p>
                    <p class="lead-property">{{ lead.propertyTitle }}</p>
                    <div class="lead-meta">
                      <span class="lead-time">{{ timeInStage(lead.stageEnteredAt) }}</span>
                      @if (lead.dealValue) {
                        <span class="lead-value">{{ lead.dealValue }}</span>
                      }
                    </div>
                  </div>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>

    <!-- Lead Detail Modal -->
    @if (selectedLead) {
      <div class="modal-overlay" (click)="closeModal()">
        <div class="modal" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2>{{ selectedLead.tenantName }}</h2>
            <button class="close-btn" (click)="closeModal()">×</button>
          </div>

          <div class="modal-section">
            <h4>Property</h4>
            <div class="info-row">🏠 {{ selectedLead.propertyTitle }}</div>
          </div>

          <div class="modal-section">
            <h4>Stage</h4>
            <span
              class="stage-badge"
              [style.background]="getColColor(selectedLead.status) + '22'"
              [style.color]="getColColor(selectedLead.status)"
            >
              {{ getColLabel(selectedLead.status) }}
            </span>
            <span style="font-size:0.82rem; color:#aaa; margin-left:0.75rem;">
              {{ timeInStage(selectedLead.stageEnteredAt) }} in this stage
            </span>
          </div>

          @if (selectedLead.email || selectedLead.phone) {
            <div class="modal-section">
              <h4>Contact</h4>
              @if (selectedLead.email) {
                <div class="info-row">📧 {{ selectedLead.email }}</div>
              }
              @if (selectedLead.phone) {
                <div class="info-row">📞 {{ selectedLead.phone }}</div>
              }
            </div>
          }

          @if (selectedLead.dealValue) {
            <div class="modal-section">
              <h4>Deal Value</h4>
              <div class="info-row">{{ selectedLead.dealValue }}</div>
            </div>
          }

          <div class="modal-section">
            <h4>Notes</h4>
            <textarea
              class="notes-area"
              [(ngModel)]="noteText"
              placeholder="Add a note about this lead..."
            ></textarea>
            <button class="btn btn-save" (click)="saveNote()">Save Note</button>
          </div>

          <div class="modal-actions">
            <button class="btn btn-success" (click)="moveTo('closed_won')">
              Move to Closed Won
            </button>
            <button class="btn btn-danger" (click)="moveTo('closed_lost')">
              Move to Closed Lost
            </button>
            <button class="btn btn-outline" (click)="closeModal()">Close</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class LeadsComponent implements OnInit {
  private brokerService = inject(BrokerDataService);
  private destroyRef = inject(DestroyRef);

  loading = true;
  selectedLead: Lead | null = null;
  noteText = '';

  columns: KanbanColumn[] = [
    { status: 'new', label: 'New', color: '#3b82f6', leads: [] },
    { status: 'contacted', label: 'Contacted', color: '#f59e0b', leads: [] },
    { status: 'negotiating', label: 'Negotiating', color: '#8b5cf6', leads: [] },
    { status: 'closed_won', label: 'Closed Won', color: '#10b981', leads: [] },
    { status: 'closed_lost', label: 'Closed Lost', color: '#ef4444', leads: [] },
  ];

  get connectedIds(): string[] {
    return this.columns.map((c) => c.status);
  }

  ngOnInit() {
    this.brokerService
      .getLeads()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((leads) => {
        this.columns.forEach((col) => {
          col.leads = leads.filter((l) => l.status === col.status);
        });
        this.loading = false;
      });
  }

  onDrop(event: CdkDragDrop<Lead[]>) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex,
      );
      const lead = event.container.data[event.currentIndex];
      const newStatus = event.container.id as Lead['status'];
      lead.status = newStatus;
      lead.stageEnteredAt = Date.now();
      this.brokerService.updateLeadStatus(lead.id, newStatus).subscribe();
    }
  }

  timeInStage(ts: number): string {
    const diff = Date.now() - ts;
    const hours = Math.floor(diff / 3600000);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    return `${days}d ago`;
  }

  getColLabel(status: Lead['status']): string {
    return this.columns.find((c) => c.status === status)?.label ?? status;
  }

  getColColor(status: Lead['status']): string {
    return this.columns.find((c) => c.status === status)?.color ?? '#888';
  }

  openModal(lead: Lead) {
    this.selectedLead = lead;
    this.noteText = lead.notes ?? '';
  }

  closeModal() {
    this.selectedLead = null;
    this.noteText = '';
  }

  saveNote() {
    if (this.selectedLead) {
      this.selectedLead.notes = this.noteText;
    }
  }

  moveTo(status: Lead['status']) {
    if (!this.selectedLead) return;
    const fromCol = this.columns.find((c) => c.leads.includes(this.selectedLead!));
    const toCol = this.columns.find((c) => c.status === status);
    if (!fromCol || !toCol || fromCol === toCol) return;
    fromCol.leads = fromCol.leads.filter((l) => l.id !== this.selectedLead!.id);
    this.selectedLead.status = status;
    this.selectedLead.stageEnteredAt = Date.now();
    toCol.leads.push(this.selectedLead);
    this.brokerService.updateLeadStatus(this.selectedLead.id, status).subscribe();
    this.closeModal();
  }
}

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { DragDropModule, CdkDragDrop, transferArrayItem, moveItemInArray } from '@angular/cdk/drag-drop';
import { BrokerDataService } from '../../services/broker-data.service';
import { Lead } from '../../models/broker.models';
import { LeaseUploadModalComponent } from '../../components/lease-upload-modal/lease-upload-modal.component';

interface KanbanColumn { status: Lead['status']; labelKey: string; color: string; leads: Lead[]; }

@Component({
  selector: 'app-broker-leads',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslatePipe, DragDropModule, LeaseUploadModalComponent],
  templateUrl: './leads.component.html',
  styleUrl: './leads.component.css',
})
export class LeadsComponent implements OnInit {
  private readonly dataService = inject(BrokerDataService);

  readonly loading = signal(true);
  readonly selectedLead = signal<Lead | null>(null);
  noteText = '';
  readonly closingLeadId = signal<string | null>(null);
  readonly leaseUploadLead = signal<Lead | null>(null);

  readonly columns = signal<KanbanColumn[]>([
    { status: 'new',          labelKey: 'BROKER.LEADS.COL_NEW',          color: '#3b82f6', leads: [] },
    { status: 'contacted',    labelKey: 'BROKER.LEADS.COL_CONTACTED',    color: '#f59e0b', leads: [] },
    { status: 'negotiating',  labelKey: 'BROKER.LEADS.COL_NEGOTIATING',  color: '#8b5cf6', leads: [] },
    { status: 'closed_won',   labelKey: 'BROKER.LEADS.COL_CLOSED_WON',   color: '#10b981', leads: [] },
    { status: 'closed_lost',  labelKey: 'BROKER.LEADS.COL_CLOSED_LOST',  color: '#ef4444', leads: [] },
  ]);

  readonly connectedIds = computed(() => this.columns().map(c => c.status));

  /** Open (non-closed) pipeline stages a lead can be moved to from the detail modal. */
  readonly openStages: { status: Lead['status']; labelKey: string }[] = [
    { status: 'new',         labelKey: 'BROKER.LEADS.COL_NEW' },
    { status: 'contacted',   labelKey: 'BROKER.LEADS.COL_CONTACTED' },
    { status: 'negotiating', labelKey: 'BROKER.LEADS.COL_NEGOTIATING' },
  ];
  readonly movingStage = signal(false);

  /** Pipeline order. Both closed states are terminal (same rank). */
  private readonly stageRank: Record<Lead['status'], number> = {
    new: 0, contacted: 1, negotiating: 2, closed_won: 3, closed_lost: 3,
  };

  /**
   * Forward-only: a lead may advance to a later stage, never back to an earlier
   * one, and closed leads are terminal (can't move at all).
   */
  canMoveTo(lead: Lead, status: Lead['status']): boolean {
    if (lead.status === status) return false;
    if (lead.status === 'closed_won' || lead.status === 'closed_lost') return false;
    return this.stageRank[status] > this.stageRank[lead.status];
  }

  /** Re-emit the columns array so zoneless change detection picks up array mutations. */
  private refreshColumns(): void { this.columns.set([...this.columns()]); }

  ngOnInit(): void {
    this.dataService.getLeads().subscribe({
      next: leads => {
        const cols = this.columns();
        leads.forEach(l => cols.find(c => c.status === l.status)?.leads.push(l));
        this.refreshColumns();
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  drop(event: CdkDragDrop<Lead[]>, targetCol: KanbanColumn): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      this.refreshColumns();
      return;
    }

    const lead = event.previousContainer.data[event.previousIndex];

    // Forward-only: ignore backward / terminal drops — CDK snaps the card back.
    if (!this.canMoveTo(lead, targetCol.status)) return;

    const prevStatus = lead.status;
    transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
    lead.status = targetCol.status;
    this.dataService.updateLeadStatus(lead.id, targetCol.status).subscribe({
      error: () => this.moveLead(lead, prevStatus), // revert if the server rejects it
    });
    this.refreshColumns();
  }

  openDetail(lead: Lead): void {
    this.selectedLead.set(lead);
    this.noteText = lead.notes ?? '';
  }

  closeDetail(): void { this.selectedLead.set(null); }

  saveNotes(): void {
    const lead = this.selectedLead();
    if (lead) {
      lead.notes = this.noteText;
      this.closeDetail();
    }
  }

  closeWon(lead: Lead): void {
    this.closingLeadId.set(lead.id);
    this.dataService.closeLead(lead.id, 'won').subscribe({
      next: () => {
        this.moveLead(lead, 'closed_won');
        this.closingLeadId.set(null);
        this.selectedLead.set(null);
        this.leaseUploadLead.set(lead);
      },
      error: () => this.closingLeadId.set(null),
    });
  }

  closeLost(lead: Lead): void {
    this.closingLeadId.set(lead.id);
    this.dataService.closeLead(lead.id, 'lost').subscribe({
      next: () => {
        this.moveLead(lead, 'closed_lost');
        this.closingLeadId.set(null);
        this.selectedLead.set(null);
      },
      error: () => this.closingLeadId.set(null),
    });
  }

  /** Advance a lead to an open stage (new/contacted/negotiating) without dragging. */
  moveToStage(lead: Lead, status: Lead['status']): void {
    if (!this.canMoveTo(lead, status) || this.movingStage()) return;
    this.movingStage.set(true);
    this.dataService.updateLeadStatus(lead.id, status).subscribe({
      next: () => { this.moveLead(lead, status); this.movingStage.set(false); },
      error: () => this.movingStage.set(false),
    });
  }

  private moveLead(lead: Lead, newStatus: Lead['status']): void {
    const cols = this.columns();
    const fromCol = cols.find(c => c.status === lead.status);
    if (fromCol) {
      const idx = fromCol.leads.indexOf(lead);
      if (idx > -1) fromCol.leads.splice(idx, 1);
    }
    lead.status = newStatus;
    cols.find(c => c.status === newStatus)?.leads.push(lead);
    this.refreshColumns();
  }

  /** Leads whose lease was created this session (immediate UI, before a reload). */
  private readonly leasedIds = signal<Set<string>>(new Set());

  onLeaseCreated(): void {
    const lead = this.leaseUploadLead();
    if (lead) this.leasedIds.update(s => new Set(s).add(lead.id));
    this.leaseUploadLead.set(null);
  }

  /** A lease exists for this lead (from the backend, or created just now). */
  hasLease(lead: Lead): boolean {
    return lead.hasLease === true || this.leasedIds().has(lead.id);
  }

  timeInStage(ts: number): string {
    const diff = Date.now() - ts;
    const h = Math.floor(diff / 3600000);
    if (h < 24) return `${h}h`;
    return `${Math.floor(h / 24)}d`;
  }

  isClosing(lead: Lead): boolean {
    return this.closingLeadId() === lead.id;
  }

  canClose(lead: Lead): boolean {
    return lead.status !== 'closed_won' && lead.status !== 'closed_lost';
  }
}

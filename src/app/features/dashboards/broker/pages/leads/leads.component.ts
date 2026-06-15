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
    } else {
      const lead = event.previousContainer.data[event.previousIndex];
      transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
      lead.status = targetCol.status;
      this.dataService.updateLeadStatus(lead.id, targetCol.status).subscribe();
    }
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

  onLeaseCreated(): void {
    this.leaseUploadLead.set(null);
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

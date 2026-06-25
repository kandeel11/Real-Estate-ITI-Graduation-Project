import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CompanyService,
  CompanyLeadPipelineDto,
  BrokerPipelineDto,
  BrokerPipeline,
  LeadPipelineItem,
} from '../../services/company.service';

interface PipelineCol {
  key: keyof BrokerPipeline;
  label: string;
  color: string;
  icon: string;
}

@Component({
  selector: 'app-company-pipeline',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pipeline.component.html',
  styleUrl: './pipeline.component.css',
})
export class PipelineComponent implements OnInit {
  private readonly svc = inject(CompanyService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly pipeline = signal<CompanyLeadPipelineDto | null>(null);
  readonly selectedBrokerId = signal<string>('all');

  readonly COLUMNS: PipelineCol[] = [
    { key: 'new',         label: 'New',         color: '#3b82f6', icon: 'fa-solid fa-circle-plus' },
    { key: 'contacted',   label: 'Contacted',   color: '#f59e0b', icon: 'fa-solid fa-phone' },
    { key: 'negotiating', label: 'Negotiating', color: '#8b5cf6', icon: 'fa-solid fa-handshake' },
    { key: 'closedWon',   label: 'Won',         color: '#10b981', icon: 'fa-solid fa-circle-check' },
    { key: 'closedLost',  label: 'Lost',        color: '#ef4444', icon: 'fa-solid fa-circle-xmark' },
  ];

  readonly brokers = computed<BrokerPipelineDto[]>(() => this.pipeline()?.brokers ?? []);

  readonly columnLeads = computed<Record<string, LeadPipelineItem[]>>(() => {
    const data = this.pipeline();
    const empty = { new: [], contacted: [], negotiating: [], closedWon: [], closedLost: [] };
    if (!data) return empty;
    const id = this.selectedBrokerId();
    const source = id === 'all' ? data.brokers : data.brokers.filter(b => b.brokerId === id);
    return {
      new:         source.flatMap(b => b.pipeline.new         ?? []),
      contacted:   source.flatMap(b => b.pipeline.contacted   ?? []),
      negotiating: source.flatMap(b => b.pipeline.negotiating ?? []),
      closedWon:   source.flatMap(b => b.pipeline.closedWon   ?? []),
      closedLost:  source.flatMap(b => b.pipeline.closedLost  ?? []),
    };
  });

  readonly stats = computed(() => {
    const cols = this.columnLeads();
    const active = (cols['new']?.length ?? 0) + (cols['contacted']?.length ?? 0) + (cols['negotiating']?.length ?? 0);
    const allLeads = Object.values(cols).flat();
    return {
      total:  allLeads.length,
      active,
      won:    cols['closedWon']?.length  ?? 0,
      lost:   cols['closedLost']?.length ?? 0,
      value:  allLeads.reduce((s, l) => s + (l.dealValue ?? 0), 0),
    };
  });

  ngOnInit(): void {
    this.loadPipeline();
  }

  loadPipeline(): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.getLeadPipeline().subscribe({
      next:  data => { this.pipeline.set(data); this.loading.set(false); },
      error: ()   => { this.error.set('Failed to load pipeline data. Please try again.'); this.loading.set(false); },
    });
  }

  colLeads(key: string): LeadPipelineItem[] {
    return (this.columnLeads() as Record<string, LeadPipelineItem[]>)[key] ?? [];
  }

  totalLeadsForAll(): number {
    return this.pipeline()?.brokers.reduce((s, b) => s + b.totalLeads, 0) ?? 0;
  }

  leadAge(iso: string): string {
    const h = Math.floor((Date.now() - new Date(iso).getTime()) / 3_600_000);
    if (h < 1)  return 'just now';
    if (h < 24) return `${h}h`;
    const d = Math.floor(h / 24);
    return d < 30 ? `${d}d` : `${Math.floor(d / 30)}mo`;
  }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }

  shortId(id: string): string {
    return '#' + id.slice(-6).toUpperCase();
  }

  trackById(_: number, item: LeadPipelineItem): string { return item.id; }
  trackByBroker(_: number, b: BrokerPipelineDto): string { return b.brokerId; }
}

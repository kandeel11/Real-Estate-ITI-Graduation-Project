import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { BrokerDataService, BrokerLease } from '../../services/broker-data.service';

type LeaseFilter = 'all' | 'active' | 'ended' | 'pending';

@Component({
  selector: 'app-broker-leases',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe],
  templateUrl: './leases.component.html',
  styleUrl: './leases.component.css',
})
export class LeasesComponent implements OnInit {
  private readonly svc = inject(BrokerDataService);

  readonly leases = signal<BrokerLease[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<LeaseFilter>('all');

  readonly filtered = computed(() => {
    const f = this.filter();
    const list = this.leases();
    if (f === 'all') return list;
    if (f === 'pending') return list.filter(l => l.tenantStatus === 'pending_tenant');
    return list.filter(l => l.status === f);
  });

  ngOnInit(): void {
    this.svc.getLeases().subscribe({
      next: l => { this.leases.set(l); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  computedStatus(lease: BrokerLease): 'active' | 'ended' | 'expiring' {
    const end = new Date(lease.endDate);
    const now = new Date();
    const daysLeft = (end.getTime() - now.getTime()) / 86400000;
    if (end < now) return 'ended';
    if (daysLeft <= 30) return 'expiring';
    return 'active';
  }
}

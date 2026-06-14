import { Component, OnInit, inject, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../../core/services/auth.service';
import { BrokerDataService } from '../../services/broker-data.service';
import { BrokerStats, BrokerTask, BrokerActivity } from '../../models/broker.models';

@Component({
  selector: 'app-broker-overview',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.css',
})
export class OverviewComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly dataService = inject(BrokerDataService);

  readonly isCompanyBroker = computed(() => !!this.auth.user()?.companyId);
  readonly userName = computed(() => this.auth.user()?.fullName ?? 'Broker');

  readonly stats = signal<BrokerStats | null>(null);
  readonly tasks = signal<BrokerTask[]>([]);
  readonly activity = signal<BrokerActivity[]>([]);
  readonly loading = signal(true);

  private loaded = 0;
  private done(): void { if (++this.loaded >= 3) this.loading.set(false); }

  ngOnInit(): void {
    this.dataService.getStats().subscribe({ next: s => { this.stats.set(s); this.done(); }, error: () => this.done() });
    this.dataService.getTasks().subscribe({ next: t => { this.tasks.set(t); this.done(); }, error: () => this.done() });
    this.dataService.getRecentActivity().subscribe({ next: a => { this.activity.set(a); this.done(); }, error: () => this.done() });
  }

  toggleTask(task: BrokerTask): void {
    this.tasks.update(list => list.map(t => t.id === task.id ? { ...t, done: !t.done } : t));
  }

  get stars(): number[] { return [1, 2, 3, 4, 5]; }
}

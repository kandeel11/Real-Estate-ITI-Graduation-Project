import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { BrokerDataService } from '../../services/broker-data.service';
import { PerformanceStats, Rating } from '../../models/broker.models';

@Component({
  selector: 'app-broker-performance',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './performance.component.html',
  styleUrl: './performance.component.css',
})
export class PerformanceComponent implements OnInit {
  private readonly dataService = inject(BrokerDataService);

  readonly stats = signal<PerformanceStats | null>(null);
  readonly ratings = signal<Rating[]>([]);
  readonly loading = signal(true);

  readonly maxDeals = computed(() =>
    Math.max(...(this.stats()?.monthlyDeals.map(m => m.deals) ?? [1]), 1));

  private loaded = 0;
  private checkDone(): void { if (++this.loaded >= 2) this.loading.set(false); }

  ngOnInit(): void {
    this.dataService.getPerformance().subscribe({ next: s => { this.stats.set(s); this.checkDone(); }, error: () => this.checkDone() });
    this.dataService.getRatings().subscribe({ next: r => { this.ratings.set(r); this.checkDone(); }, error: () => this.checkDone() });
  }

  stars(n: number): number[] { return Array.from({ length: n }, (_, i) => i); }
}

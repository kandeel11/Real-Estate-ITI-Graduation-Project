import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { OwnerDataService, BrokerLease } from '../../services/owner-data.service';

type LeaseFilter = 'all' | 'active' | 'ended' | 'pending';

@Component({
  selector: 'app-owner-leases',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe],
  templateUrl: './leases.component.html',
  styleUrl: './leases.component.css',
})
export class LeasesComponent implements OnInit {
  private readonly svc = inject(OwnerDataService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly leases = signal<BrokerLease[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<LeaseFilter>('all');

  // PDF viewer
  readonly contractRawUrl = signal<string | null>(null);
  readonly contractSafeUrl = signal<SafeResourceUrl | null>(null);
  readonly contractLoading = signal<boolean>(false);
  readonly contractViewError = signal<string | null>(null);
  private _contractBlobUrl: string | null = null;

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

  viewContract(lease: BrokerLease): void {
    this.contractLoading.set(true);
    this.contractViewError.set(null);
    this.contractSafeUrl.set(null);
    this.contractRawUrl.set(null);
    this.svc.getContractUrl(lease.id).subscribe({
      next: ({ contractFileUrl }) => {
        this.contractRawUrl.set(contractFileUrl);
        fetch(contractFileUrl)
          .then(r => r.blob())
          .then(blob => {
            if (this._contractBlobUrl) URL.revokeObjectURL(this._contractBlobUrl);
            this._contractBlobUrl = URL.createObjectURL(blob);
            this.contractSafeUrl.set(
              this.sanitizer.bypassSecurityTrustResourceUrl(this._contractBlobUrl)
            );
            this.contractLoading.set(false);
          })
          .catch(() => {
            this.contractViewError.set('Failed to load contract. Please try again.');
            this.contractLoading.set(false);
          });
      },
      error: () => {
        this.contractViewError.set('Failed to load contract. Please try again.');
        this.contractLoading.set(false);
      },
    });
  }

  closeContractViewer(): void {
    if (this._contractBlobUrl) URL.revokeObjectURL(this._contractBlobUrl);
    this._contractBlobUrl = null;
    this.contractSafeUrl.set(null);
    this.contractRawUrl.set(null);
    this.contractViewError.set(null);
    this.contractLoading.set(false);
  }
}

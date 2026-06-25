/**
 * Saved Properties (Wishlist) tab
 * Lists the listings the tenant has hearted, fetched from the backend
 * (GET /api/properties/saved via SavedPropertyService). Tenants can open or
 * un-save each card; un-saving updates the shared signal so badges stay in sync.
 */

import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { SavedPropertyService } from '../../../../../core/services/saved-property.service';

@Component({
  selector: 'app-tenant-saved',
  standalone: true,
  imports: [DecimalPipe, RouterLink, TranslatePipe],
  templateUrl: './saved.component.html',
  styleUrl: './saved.component.css',
})
export class SavedComponent implements OnInit {
  protected savedSvc = inject(SavedPropertyService);

  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.savedSvc.loadList().subscribe({
      next: () => this.loading.set(false),
      error: () => {
        this.error.set('Could not load your saved properties. Please try again.');
        this.loading.set(false);
      },
    });
  }

  typeIcon(type: string): string {
    switch (type) {
      case 'Villa': return 'fa-solid fa-house-chimney';
      case 'House': return 'fa-solid fa-house-user';
      default: return 'fa-solid fa-building';
    }
  }

  unsave(id: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.savedSvc.remove(id);
  }
}

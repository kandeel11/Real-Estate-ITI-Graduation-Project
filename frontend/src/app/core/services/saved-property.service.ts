/**
 * Saved Properties (Wishlist)
 * Backend-backed tenant wishlist (Property module: POST/DELETE /properties/{id}/save,
 * GET /properties/saved[/ids]). The set of saved ids is loaded for the signed-in
 * tenant and kept in a signal so cards and the detail page reflect the toggle
 * state reactively. Toggling is optimistic — it updates the signal immediately and
 * reverts if the request fails (e.g. the plan's saved-listings limit is reached).
 */

import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import { AuthService } from './auth.service';
import { Property, PropertyDetail } from '../../features/properties/models/property.model';

/** Matches the backend's SavedPropertyDto (GET /api/properties/saved). */
export interface SavedProperty {
  id: string;
  title: string;
  propertyType: string;
  price: number;
  bedrooms: number;
  bathrooms: number;
  areaSqm: number;
  city: string;
  district: string;
  primaryImageUrl: string | null;
  isFeatured: boolean;
  savedAt: string;
}

@Injectable({ providedIn: 'root' })
export class SavedPropertyService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly baseUrl = environment.apiUrl;

  /** Ids the tenant has saved — source of truth for toggle state. */
  private readonly ids = signal<Set<string>>(new Set());
  readonly savedIds = this.ids.asReadonly();
  readonly count = computed(() => this.ids().size);

  /** Full snapshots for the "Saved" tab (loaded on demand via loadList()). */
  private readonly items = signal<SavedProperty[]>([]);
  readonly list = computed(() =>
    [...this.items()].sort((a, b) => b.savedAt.localeCompare(a.savedAt))
  );

  /** Last toggle error (e.g. plan limit reached) — surfaced as a toast by callers. */
  readonly lastError = signal<string | null>(null);

  constructor() {
    // Saved listings are a tenant-only concept — load ids when a tenant signs in,
    // clear when they sign out or another role is active.
    effect(() => {
      const user = this.auth.user();
      if (user?.role === 'tenant') {
        this.refreshIds();
      } else {
        this.ids.set(new Set());
        this.items.set([]);
      }
    });
  }

  isSaved(id: string): boolean {
    return this.ids().has(id);
  }

  /** Toggle a property in the wishlist (optimistic; reverts on failure). */
  toggle(p: Property | PropertyDetail): void {
    if (this.isSaved(p.id)) this.remove(p.id);
    else this.save(p.id);
  }

  save(id: string): void {
    if (this.isSaved(id)) return;
    this.lastError.set(null);
    this.setSaved(id, true); // optimistic

    this.http.post(`${this.baseUrl}${API_ENDPOINTS.PROPERTIES.SAVE(id)}`, {}).subscribe({
      error: (err) => {
        this.setSaved(id, false); // revert
        this.lastError.set(err?.error?.error ?? 'Could not save this property. Please try again.');
      },
    });
  }

  remove(id: string): void {
    if (!this.isSaved(id)) return;
    this.setSaved(id, false); // optimistic
    this.items.set(this.items().filter((p) => p.id !== id));

    this.http.delete(`${this.baseUrl}${API_ENDPOINTS.PROPERTIES.SAVE(id)}`).subscribe({
      error: () => {
        this.setSaved(id, true); // revert
        this.lastError.set('Could not remove this property. Please try again.');
      },
    });
  }

  /** Loads the full saved list for the "Saved" tab; also resyncs the id set. */
  loadList(): Observable<SavedProperty[]> {
    return this.http
      .get<SavedProperty[]>(`${this.baseUrl}${API_ENDPOINTS.PROPERTIES.SAVED}`)
      .pipe(
        tap((list) => {
          this.items.set(list);
          this.ids.set(new Set(list.map((p) => p.id)));
        })
      );
  }

  clearError(): void {
    this.lastError.set(null);
  }

  // ── internals ────────────────────────────────────────────────────
  private refreshIds(): void {
    this.http
      .get<string[]>(`${this.baseUrl}${API_ENDPOINTS.PROPERTIES.SAVED_IDS}`)
      .subscribe({
        next: (ids) => this.ids.set(new Set(ids)),
        error: () => this.ids.set(new Set()),
      });
  }

  private setSaved(id: string, saved: boolean): void {
    const next = new Set(this.ids());
    saved ? next.add(id) : next.delete(id);
    this.ids.set(next);
  }
}

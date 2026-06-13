/**
 * Public Property Detail
 * Full listing from GET /api/properties/{id}: gallery, specs, amenities,
 * the listing broker/company, and a tenant inquiry form (POST /api/inquiries).
 * Non-tenants see a prompt to sign in instead of the form.
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PropertyService } from '../../services/property.service';
import { PropertyDetail } from '../../models/property.model';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [DecimalPipe, RouterLink, FormsModule],
  templateUrl: './detail.component.html',
  styleUrl: './detail.component.css',
})
export class DetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private propertySvc = inject(PropertyService);
  private auth = inject(AuthService);

  property = signal<PropertyDetail | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  activeImage = signal<string | null>(null);

  isTenant = computed(() => this.auth.userRole() === 'tenant');
  isAuthenticated = this.auth.authenticated;

  // ── Inquiry form ────────────────────────────────────────────────
  message = '';
  sending = signal(false);
  inquirySent = signal(false);
  inquiryError = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Property not found.');
      this.loading.set(false);
      return;
    }
    this.propertySvc.getById(id).subscribe({
      next: (detail) => {
        this.property.set(detail);
        const primary = detail.media.find((m) => m.isPrimary) ?? detail.media[0];
        this.activeImage.set(primary?.mediaUrl ?? null);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('This listing could not be loaded — it may no longer be available.');
        this.loading.set(false);
      },
    });
  }

  typeIcon(type: string): string {
    switch (type) {
      case 'Villa':
        return 'fa-solid fa-house-chimney';
      case 'House':
        return 'fa-solid fa-house-user';
      default:
        return 'fa-solid fa-building';
    }
  }

  sendInquiry(): void {
    const p = this.property();
    if (!p || this.sending()) return;
    if (!this.message.trim()) {
      this.inquiryError.set('Please write a message first.');
      return;
    }

    this.sending.set(true);
    this.inquiryError.set(null);

    this.propertySvc
      .createInquiry(p.id, p.ownerId, p.companyId, this.message.trim())
      .subscribe({
        next: () => {
          this.sending.set(false);
          this.inquirySent.set(true);
          this.message = '';
        },
        error: (err) => {
          this.sending.set(false);
          this.inquiryError.set(
            err?.error?.error ?? 'Could not send your inquiry. Please try again.'
          );
        },
      });
  }
}

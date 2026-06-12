import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { finalize } from 'rxjs';
import { LeaseService, RentedPropertyItem, ApiPropertyMedia, ApiAmenity } from '../../services/lease.service';
import { RatingModalComponent } from '../../../../../shared/components/rating-modal/rating-modal.component';

interface PropertyView {
  id: string;
  leaseId: string;
  title: string;
  fullAddress: string;
  type: string;
  bedrooms: number;
  bathrooms: number;
  area: number;
  isFurnished: boolean;
  petsAllowed: boolean;
  mainImage: string;
  monthlyRent: number;
  isActive: boolean;
  startDate: string;
  endDate: string;
  daysUntilExpiry: number;
  media: ApiPropertyMedia[];
  amenities: ApiAmenity[];
  lightboxUrl: string | null;
}

@Component({
  selector: 'app-my-property',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe, RatingModalComponent],
  templateUrl: './my-property.component.html',
  styleUrl: './my-property.component.css',
})
export class MyPropertyComponent implements OnInit {
  private readonly leaseService = inject(LeaseService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected properties = signal<PropertyView[]>([]);
  protected lightboxUrl = signal<string | null>(null);

  protected showRatingModal = signal(false);
  protected ratingPropertyId = signal<string | null>(null);
  protected ratingLeaseId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    this.leaseService.getRentedProperties()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: items => this.properties.set(items.map(item => this.toView(item))),
        error: () => this.error.set('Failed to load your properties. Please try again.'),
      });
  }

  private toView(item: RentedPropertyItem): PropertyView {
    const p = item.property;
    const parts = [p.address, p.district, p.city, p.governorate].filter(Boolean);
    const primaryMedia = p.media.find(m => m.isPrimary) ?? p.media[0];

    return {
      id: p.id,
      leaseId: item.leaseId,
      title: p.title,
      fullAddress: parts.join(', '),
      type: p.propertyType,
      bedrooms: p.bedrooms,
      bathrooms: p.bathrooms,
      area: p.areaSqm,
      isFurnished: p.isFurnished,
      petsAllowed: p.petsAllowed,
      mainImage: primaryMedia?.url ?? '',
      monthlyRent: item.monthlyRent,
      isActive: item.isActive,
      startDate: item.startDate,
      endDate: item.endDate,
      daysUntilExpiry: item.daysUntilExpiry,
      media: p.media,
      amenities: p.amenities,
      lightboxUrl: null,
    };
  }

  openLightbox(url: string): void {
    this.lightboxUrl.set(url);
  }

  closeLightbox(): void {
    this.lightboxUrl.set(null);
  }

  openRatingModal(prop: PropertyView): void {
    this.ratingPropertyId.set(prop.id);
    this.ratingLeaseId.set(prop.leaseId);
    this.showRatingModal.set(true);
  }
}

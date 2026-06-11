import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { forkJoin } from 'rxjs';
import { Amenity, LeaseService, PropertyMedia, RentedProperty } from '../../services/lease.service';

interface PropertyView extends RentedProperty {
  media: PropertyMedia[];
  amenities: Amenity[];
  lightboxUrl: string | null;
}

@Component({
  selector: 'app-my-property',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe],
  templateUrl: './my-property.component.html',
  styleUrl: './my-property.component.css',
})
export class MyPropertyComponent implements OnInit {
  private readonly leaseService = inject(LeaseService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected properties = signal<PropertyView[]>([]);
  protected lightboxUrl = signal<string | null>(null);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    this.leaseService.getRentedProperties().subscribe({
      next: props => {
        if (props.length === 0) {
          this.loading.set(false);
          return;
        }
        forkJoin(
          props.map(p =>
            forkJoin({
              media: this.leaseService.getPropertyMedia(p.id),
              amenities: this.leaseService.getPropertyAmenities(p.id),
            })
          )
        ).subscribe({
          next: extras => {
            this.properties.set(
              props.map((p, i) => ({
                ...p,
                media: extras[i].media,
                amenities: extras[i].amenities,
                lightboxUrl: null,
              }))
            );
            this.loading.set(false);
          },
          error: () => {
            this.properties.set(props.map(p => ({ ...p, media: [], amenities: [], lightboxUrl: null })));
            this.loading.set(false);
          },
        });
      },
      error: () => {
        this.error.set('Failed to load your properties. Please try again.');
        this.loading.set(false);
      },
    });
  }

  openLightbox(url: string): void {
    this.lightboxUrl.set(url);
  }

  closeLightbox(): void {
    this.lightboxUrl.set(null);
  }
}

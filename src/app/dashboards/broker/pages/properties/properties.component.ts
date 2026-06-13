import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BrokerDataService } from '../../services/broker-data.service';
import { Property } from '../../models/broker.models';

@Component({
  selector: 'app-broker-properties',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styles: [
    `
      .page {
        padding: 2rem;
        max-width: 1100px;
        margin: 0 auto;
      }
      .page-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1.5rem;
        flex-wrap: wrap;
        gap: 1rem;
      }
      .page-header h1 {
        font-size: 1.4rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0;
      }
      .filters {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
      }
      .filters select {
        padding: 0.45rem 0.85rem;
        border: 1.5px solid #dee2e6;
        border-radius: 8px;
        font-size: 0.88rem;
        background: #fff;
        color: #333;
        cursor: pointer;
      }
      .filters select:focus {
        outline: none;
        border-color: #2d6a4f;
      }
      .property-list {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
      .property-card {
        background: #fff;
        border-radius: 12px;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
        padding: 1.25rem;
        display: flex;
        gap: 1.25rem;
        align-items: flex-start;
      }
      .thumb {
        width: 72px;
        height: 72px;
        border-radius: 10px;
        background: #e9ecef;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        font-size: 1.8rem;
      }
      .property-info {
        flex: 1;
        min-width: 0;
      }
      .property-title {
        font-size: 1rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0 0 0.25rem;
      }
      .property-meta {
        font-size: 0.85rem;
        color: #6c757d;
        margin: 0 0 0.4rem;
      }
      .property-owner {
        font-size: 0.82rem;
        color: #888;
        margin: 0 0 0.5rem;
      }
      .property-stats {
        display: flex;
        gap: 1rem;
        flex-wrap: wrap;
        align-items: center;
      }
      .status-badge {
        display: inline-block;
        padding: 0.2rem 0.65rem;
        border-radius: 20px;
        font-size: 0.78rem;
        font-weight: 500;
      }
      .status-active {
        background: #d1fae5;
        color: #065f46;
      }
      .status-inactive {
        background: #fee2e2;
        color: #991b1b;
      }
      .stat-chip {
        font-size: 0.82rem;
        color: #555;
      }
      .new-badge {
        background: #dbeafe;
        color: #1e40af;
        padding: 0.15rem 0.5rem;
        border-radius: 10px;
        font-size: 0.75rem;
        font-weight: 600;
        margin-left: 4px;
      }
      .actions {
        display: flex;
        gap: 0.5rem;
        flex-shrink: 0;
        flex-wrap: wrap;
      }
      .btn {
        padding: 0.45rem 0.9rem;
        border-radius: 8px;
        font-size: 0.83rem;
        font-weight: 500;
        cursor: pointer;
        border: none;
        text-decoration: none;
      }
      .btn-primary {
        background: #2d6a4f;
        color: #fff;
      }
      .btn-primary:hover {
        background: #1b4332;
      }
      .btn-outline {
        background: #fff;
        color: #2d6a4f;
        border: 1.5px solid #2d6a4f;
      }
      .btn-outline:hover {
        background: #f0faf5;
      }
      .skeleton {
        background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
        background-size: 200% 100%;
        animation: shimmer 1.5s infinite;
        border-radius: 8px;
      }
      @keyframes shimmer {
        0% {
          background-position: 200% 0;
        }
        100% {
          background-position: -200% 0;
        }
      }
      .empty {
        text-align: center;
        padding: 3rem;
        color: #aaa;
      }
      .empty-icon {
        font-size: 3rem;
        margin-bottom: 1rem;
      }
      .empty p {
        font-size: 0.95rem;
      }
    `,
  ],
  template: `
    <div class="page">
      <div class="page-header">
        <h1>My Assigned Properties ({{ filtered.length }})</h1>
        <div class="filters">
          <select [(ngModel)]="statusFilter" (ngModelChange)="applyFilter()">
            <option value="all">All Status</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>
        </div>
      </div>

      <!-- Skeletons -->
      @if (loading) {
        @for (i of [1, 2, 3]; track i) {
          <div class="property-card" style="margin-bottom: 1rem;">
            <div class="skeleton" style="width:72px; height:72px; border-radius:10px;"></div>
            <div style="flex:1;">
              <div class="skeleton" style="height:18px; width:55%; margin-bottom:8px;"></div>
              <div class="skeleton" style="height:14px; width:40%; margin-bottom:8px;"></div>
              <div class="skeleton" style="height:14px; width:30%;"></div>
            </div>
          </div>
        }

        <!-- Empty -->
      } @else if (filtered.length === 0) {
        <div class="empty">
          <div class="empty-icon">🏠</div>
          <p>No properties found.</p>
        </div>

        <!-- List -->
      } @else {
        <div class="property-list">
          @for (prop of filtered; track prop.id) {
            <div class="property-card">
              <div class="thumb">🏠</div>
              <div class="property-info">
                <p class="property-title">{{ prop.title }}</p>
                <p class="property-meta">
                  {{ prop.price }} · {{ prop.location }} · {{ prop.beds }}BR/{{ prop.baths }}BA
                </p>
                <p class="property-owner">Owner: {{ prop.owner }}</p>
                <div class="property-stats">
                  <span
                    class="status-badge"
                    [class.status-active]="prop.status === 'active'"
                    [class.status-inactive]="prop.status === 'inactive'"
                  >
                    {{ prop.status === 'active' ? '🟢 Active' : '🔴 Inactive' }}
                  </span>
                  <span class="stat-chip">{{ prop.views }} views</span>
                  <span class="stat-chip">
                    {{ prop.inquiries }} inquiries
                    @if (prop.newInquiries > 0) {
                      <span class="new-badge">{{ prop.newInquiries }} new</span>
                    }
                  </span>
                </div>
              </div>
              <div class="actions">
                <button class="btn btn-outline">View</button>
                <button class="btn btn-outline">Edit</button>
                <button class="btn btn-primary">Inquiries</button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class PropertiesComponent implements OnInit {
  private brokerService = inject(BrokerDataService);
  private destroyRef = inject(DestroyRef);

  properties: Property[] = [];
  filtered: Property[] = [];
  statusFilter = 'all';
  loading = true;

  ngOnInit() {
    this.brokerService
      .getProperties()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((props) => {
        this.properties = props;
        this.applyFilter();
        this.loading = false;
      });
  }

  applyFilter() {
    this.filtered =
      this.statusFilter === 'all'
        ? this.properties
        : this.properties.filter((p) => p.status === this.statusFilter);
  }
}

import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-landlord-properties',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  styles: [`
    * { box-sizing: border-box; margin: 0; padding: 0; }
    .page { font-family: 'Inter', 'Segoe UI', sans-serif; }

    .page-header {
      display: flex; align-items: center; justify-content: space-between;
      margin-bottom: 24px;
    }
    .page-header h1 { font-size: 28px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px; }
    .page-header p { font-size: 14px; color: #64748b; margin-top: 4px; }
    .btn-primary {
      display: flex; align-items: center; gap: 8px;
      padding: 10px 20px; background: #2563eb; border: none;
      border-radius: 10px; font-size: 14px; font-weight: 600;
      color: white; cursor: pointer; transition: all 0.2s; text-decoration: none;
    }
    .btn-primary:hover { background: #1d4ed8; transform: translateY(-1px); }

    .filters {
      display: flex; align-items: center; gap: 12px;
      margin-bottom: 24px; padding: 16px 20px;
      background: white; border-radius: 12px;
      border: 1px solid #f1f5f9;
      box-shadow: 0 2px 8px rgba(0,0,0,0.04);
    }
    .filter-label { font-size: 13px; font-weight: 600; color: #374151; }
    .filter-select {
      padding: 8px 14px; border: 1.5px solid #e2e8f0;
      border-radius: 8px; font-size: 13px; color: #374151;
      background: white; cursor: pointer; outline: none;
      transition: border-color 0.2s;
    }
    .filter-select:focus { border-color: #2563eb; }
    .view-toggle { margin-left: auto; display: flex; gap: 6px; }
    .toggle-btn {
      width: 34px; height: 34px; border-radius: 8px;
      border: 1.5px solid #e2e8f0; background: white;
      display: flex; align-items: center; justify-content: center;
      cursor: pointer; font-size: 14px; transition: all 0.2s;
    }
    .toggle-btn.active { background: #eff6ff; border-color: #2563eb; }

    .properties-list { display: flex; flex-direction: column; gap: 16px; }
    .property-card {
      background: white; border-radius: 16px;
      border: 1px solid #f1f5f9; overflow: hidden;
      display: flex; align-items: stretch;
      box-shadow: 0 2px 8px rgba(0,0,0,0.04);
      transition: all 0.3s;
    }
    .property-card:hover { box-shadow: 0 8px 24px rgba(0,0,0,0.08); transform: translateY(-2px); }
    .card-img {
      width: 200px; min-height: 160px; position: relative;
      flex-shrink: 0; overflow: hidden;
    }
    .card-img img { width: 100%; height: 100%; object-fit: cover; }
    .status-badge {
      position: absolute; top: 12px; left: 12px;
      padding: 4px 10px; border-radius: 6px;
      font-size: 11px; font-weight: 700; text-transform: uppercase;
    }
    .badge-active { background: #22c55e; color: white; }
    .badge-pending { background: #f59e0b; color: white; }
    .badge-rented { background: #3b82f6; color: white; }
    .badge-rejected { background: #ef4444; color: white; }
    .badge-draft { background: #94a3b8; color: white; }

    .card-body { flex: 1; padding: 20px 24px; display: flex; flex-direction: column; justify-content: space-between; }
    .card-top { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 8px; }
    .card-title { font-size: 17px; font-weight: 700; color: #0f172a; }
    .card-price { font-size: 17px; font-weight: 700; color: #2563eb; }
    .card-ref { font-size: 11px; color: #94a3b8; }
    .card-location { font-size: 13px; color: #64748b; margin-bottom: 12px; display: flex; align-items: center; gap: 4px; }
    .card-meta { display: flex; gap: 20px; margin-bottom: 16px; }
    .card-meta span { font-size: 13px; color: #64748b; display: flex; align-items: center; gap: 4px; }
    .card-stats { display: flex; gap: 20px; margin-bottom: 16px; }
    .stat { display: flex; flex-direction: column; }
    .stat-label { font-size: 11px; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.05em; }
    .stat-value { font-size: 16px; font-weight: 700; color: #0f172a; }
    .card-actions { display: flex; gap: 8px; }
    .btn-action {
      padding: 7px 16px; border-radius: 8px; font-size: 13px; font-weight: 600;
      cursor: pointer; transition: all 0.2s; display: flex; align-items: center; gap: 6px;
    }
    .btn-edit { background: white; border: 1.5px solid #e2e8f0; color: #374151; }
    .btn-edit:hover { border-color: #2563eb; color: #2563eb; }
    .btn-view { background: white; border: 1.5px solid #e2e8f0; color: #374151; }
    .btn-view:hover { border-color: #2563eb; color: #2563eb; }
    .btn-rented { background: #2563eb; border: none; color: white; }
    .btn-rented:hover { background: #1d4ed8; }
    .btn-relist { background: #2563eb; border: none; color: white; }
    .btn-relist:hover { background: #1d4ed8; }

    .pagination {
      display: flex; align-items: center; justify-content: space-between;
      margin-top: 24px; padding-top: 20px;
      border-top: 1px solid #f1f5f9;
    }
    .pagination-info { font-size: 13px; color: #64748b; }
    .pagination-btns { display: flex; gap: 6px; }
    .page-btn {
      width: 36px; height: 36px; border-radius: 8px;
      border: 1.5px solid #e2e8f0; background: white;
      font-size: 13px; font-weight: 600; color: #374151;
      cursor: pointer; transition: all 0.2s;
      display: flex; align-items: center; justify-content: center;
    }
    .page-btn.active { background: #2563eb; border-color: #2563eb; color: white; }
    .page-btn:hover:not(.active) { border-color: #2563eb; color: #2563eb; }
  `],
  template: `
    <div class="page">

      <div class="page-header">
        <div>
          <h1>My Properties</h1>
          <p>Manage and track your active real estate portfolio.</p>
        </div>
        <a class="btn-primary" routerLink="/properties/new">+ New Property</a>
      </div>

      <div class="filters">
        <span class="filter-label">Status:</span>
        <select class="filter-select" [(ngModel)]="selectedStatus" (change)="filterProperties()">
          <option value="all">All Status</option>
          <option value="active">Active</option>
          <option value="pending">Pending</option>
          <option value="rented">Rented</option>
          <option value="rejected">Rejected</option>
          <option value="draft">Draft</option>
        </select>
        <span class="filter-label">Sort:</span>
        <select class="filter-select">
          <option>Newest First</option>
          <option>Oldest First</option>
          <option>Price: High to Low</option>
          <option>Price: Low to High</option>
        </select>
        <div class="view-toggle">
          <button class="toggle-btn active">▦</button>
          <button class="toggle-btn">☰</button>
        </div>
      </div>

      <div class="properties-list">
        @for (property of filteredProperties(); track property.id) {
          <div class="property-card">
            <div class="card-img">
              <img [src]="property.image" [alt]="property.title" />
              <span class="status-badge" [class]="'badge-' + property.status">
                {{ property.status }}
              </span>
            </div>
            <div class="card-body">
              <div>
                <div class="card-top">
                  <div>
                    <div class="card-title">{{ property.title }}</div>
                    <div class="card-ref">Ref: {{ property.ref }}</div>
                  </div>
                  <div style="text-align:right">
                    <div class="card-price">{{ property.price }}</div>
                  </div>
                </div>
                <div class="card-location">📍 {{ property.location }}</div>
                <div class="card-meta">
                  <span>🛏 {{ property.beds }} Beds</span>
                  <span>🚿 {{ property.baths }} Baths</span>
                  <span>📐 {{ property.area }}</span>
                </div>
                <div class="card-stats">
                  <div class="stat">
                    <span class="stat-label">Views</span>
                    <span class="stat-value">{{ property.views }}</span>
                  </div>
                  <div class="stat">
                    <span class="stat-label">Inquiries</span>
                    <span class="stat-value">{{ property.inquiries }}</span>
                  </div>
                </div>
              </div>
              <div class="card-actions">
                <button class="btn-action btn-edit">✏️ Edit</button>
                <button class="btn-action btn-view">👁 View</button>
                @if (property.status === 'active' || property.status === 'pending') {
                  <button class="btn-action btn-rented">✓ Mark Rented</button>
                }
                @if (property.status === 'rented') {
                  <button class="btn-action btn-relist">🔄 Re-list</button>
                }
              </div>
            </div>
          </div>
        }
      </div>

      <div class="pagination">
        <span class="pagination-info">Showing 3 of 24 properties</span>
        <div class="pagination-btns">
          <button class="page-btn">‹</button>
          <button class="page-btn active">1</button>
          <button class="page-btn">2</button>
          <button class="page-btn">3</button>
          <button class="page-btn">›</button>
        </div>
      </div>

    </div>
  `,
})
export class LandlordPropertiesComponent {
  selectedStatus = 'all';

  properties = signal([
    {
      id: 1,
      title: 'Skyline Modern Heights',
      ref: '#EP-9942',
      location: 'Sheikh Zayed, Cairo',
      price: '4,500 EGP/mo',
      status: 'active',
      beds: 3, baths: 2, area: '180 sqm',
      views: 1248, inquiries: 32,
      image: 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400&auto=format&fit=crop'
    },
    {
      id: 2,
      title: 'Oakwood Residential Suite',
      ref: '#EP-8821',
      location: 'Maadi, Cairo',
      price: '3,200 EGP/mo',
      status: 'pending',
      beds: 2, baths: 2, area: '120 sqm',
      views: 854, inquiries: 14,
      image: 'https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=400&auto=format&fit=crop'
    },
    {
      id: 3,
      title: 'Brick & Iron Studio',
      ref: '#EP-4410',
      location: 'New Cairo, Cairo',
      price: '2,800 EGP/mo',
      status: 'rented',
      beds: 1, baths: 1, area: '75 sqm',
      views: 2105, inquiries: 82,
      image: 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400&auto=format&fit=crop'
    },
  ]);

  filteredProperties = signal(this.properties());

  filterProperties() {
    if (this.selectedStatus === 'all') {
      this.filteredProperties.set(this.properties());
    } else {
      this.filteredProperties.set(
        this.properties().filter(p => p.status === this.selectedStatus)
      );
    }
  }
}
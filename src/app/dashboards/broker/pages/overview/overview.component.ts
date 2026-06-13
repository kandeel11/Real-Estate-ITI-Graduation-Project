import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BrokerDataService } from '../../services/broker-data.service';
import { BrokerStats, BrokerTask, BrokerActivity, BrokerProfile } from '../../models/broker.models';

@Component({
  selector: 'app-broker-overview',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  styles: [
    `
      .overview {
        padding: 2rem;
        max-width: 1100px;
        margin: 0 auto;
      }
      .welcome {
        margin-bottom: 2rem;
      }
      .welcome h1 {
        font-size: 1.6rem;
        font-weight: 600;
        margin: 0 0 0.25rem;
        color: #1a1a2e;
      }
      .welcome p {
        color: #6c757d;
        margin: 0;
      }
      .ranking-badge {
        display: inline-block;
        margin-top: 0.5rem;
        background: #fff3cd;
        color: #856404;
        padding: 0.3rem 0.75rem;
        border-radius: 20px;
        font-size: 0.85rem;
        font-weight: 500;
      }
      .stats-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 1rem;
        margin-bottom: 2rem;
      }
      .stat-card {
        background: #fff;
        border-radius: 12px;
        padding: 1.25rem;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
        text-align: center;
      }
      .stat-card .value {
        font-size: 2rem;
        font-weight: 700;
        color: #2d6a4f;
        margin-bottom: 0.25rem;
      }
      .stat-card .label {
        font-size: 0.85rem;
        color: #6c757d;
      }
      .bottom-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1.5rem;
        margin-bottom: 2rem;
      }
      @media (max-width: 768px) {
        .bottom-grid {
          grid-template-columns: 1fr;
        }
      }
      .card {
        background: #fff;
        border-radius: 12px;
        padding: 1.25rem;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
      }
      .card h3 {
        margin: 0 0 1rem;
        font-size: 1rem;
        font-weight: 600;
        color: #1a1a2e;
      }
      .task-item {
        display: flex;
        align-items: flex-start;
        gap: 0.75rem;
        padding: 0.5rem 0;
        border-bottom: 1px solid #f0f0f0;
      }
      .task-item:last-child {
        border-bottom: none;
      }
      .task-item input[type='checkbox'] {
        margin-top: 3px;
        accent-color: #2d6a4f;
        width: 16px;
        height: 16px;
      }
      .task-item label {
        font-size: 0.9rem;
        color: #333;
        cursor: pointer;
      }
      .activity-item {
        display: flex;
        gap: 0.75rem;
        align-items: flex-start;
        padding: 0.5rem 0;
        border-bottom: 1px solid #f0f0f0;
      }
      .activity-item:last-child {
        border-bottom: none;
      }
      .activity-dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: #2d6a4f;
        margin-top: 6px;
        flex-shrink: 0;
      }
      .activity-text {
        font-size: 0.9rem;
        color: #333;
        flex: 1;
      }
      .activity-time {
        font-size: 0.78rem;
        color: #aaa;
        white-space: nowrap;
      }
      .quick-actions {
        display: flex;
        gap: 1rem;
        flex-wrap: wrap;
      }
      .btn {
        padding: 0.6rem 1.25rem;
        border-radius: 8px;
        font-size: 0.9rem;
        font-weight: 500;
        cursor: pointer;
        border: none;
        text-decoration: none;
        display: inline-block;
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
      .skeleton-text {
        height: 16px;
        margin-bottom: 8px;
      }
    `,
  ],
  template: `
    <div class="overview">
      <!-- Welcome -->
      <div class="welcome" style="margin-bottom: 2rem;">
        @if (loading) {
          <div class="skeleton" style="width: 260px; height: 28px; margin-bottom: 8px;"></div>
          <div class="skeleton" style="width: 180px; height: 16px;"></div>
        } @else {
          <h1>Welcome, {{ profile?.name }} 👋</h1>
          <p>Working at {{ profile?.companyName }}</p>
          <span class="ranking-badge">
            You're ranked #{{ profile?.rankInCompany }} in your company this month 🥈
          </span>
        }
      </div>

      <!-- Stats Cards -->
      <div class="stats-grid">
        @if (loading) {
          @for (i of [1, 2, 3, 4]; track i) {
            <div class="stat-card">
              <div class="skeleton" style="height: 48px; margin-bottom: 8px;"></div>
              <div class="skeleton" style="height: 14px; width: 80%;"></div>
            </div>
          }
        } @else {
          <div class="stat-card">
            <div class="value">{{ stats?.assignedProperties }}</div>
            <div class="label">Assigned Properties</div>
          </div>
          <div class="stat-card">
            <div class="value">{{ stats?.newInquiries }}</div>
            <div class="label">New Inquiries</div>
          </div>
          <div class="stat-card">
            <div class="value">{{ stats?.closedDeals }}</div>
            <div class="label">Closed Deals</div>
          </div>
          <div class="stat-card">
            <div class="value">⭐ {{ stats?.rating }}</div>
            <div class="label">Rating</div>
          </div>
        }
      </div>

      <!-- Tasks + Activity -->
      <div class="bottom-grid">
        <div class="card">
          <h3>Today's Tasks</h3>
          @if (loading) {
            @for (i of [1, 2, 3]; track i) {
              <div class="skeleton skeleton-text"></div>
            }
          } @else if (tasks.length === 0) {
            <p style="color:#aaa; font-size:0.9rem;">No tasks for today.</p>
          } @else {
            @for (task of tasks; track task.id) {
              <div class="task-item">
                <input type="checkbox" [id]="'task-' + task.id" [(ngModel)]="task.done" />
                <label
                  [for]="'task-' + task.id"
                  [style.textDecoration]="task.done ? 'line-through' : 'none'"
                  [style.color]="task.done ? '#aaa' : '#333'"
                >
                  {{ task.text }}
                </label>
              </div>
            }
          }
        </div>

        <div class="card">
          <h3>Recent Activity</h3>
          @if (loading) {
            @for (i of [1, 2, 3]; track i) {
              <div class="skeleton skeleton-text"></div>
            }
          } @else if (activity.length === 0) {
            <p style="color:#aaa; font-size:0.9rem;">No recent activity.</p>
          } @else {
            @for (item of activity; track item.id) {
              <div class="activity-item">
                <div class="activity-dot"></div>
                <span class="activity-text">{{ item.text }}</span>
                <span class="activity-time">{{ item.timeAgo }}</span>
              </div>
            }
          }
        </div>
      </div>

      <!-- Quick Actions -->
      <div class="card">
        <h3>Quick Actions</h3>
        <div class="quick-actions">
          <a routerLink="../leads" class="btn btn-primary">View Pipeline</a>
          <a routerLink="../inquiries" class="btn btn-outline">Check Inquiries</a>
          <a routerLink="../properties" class="btn btn-outline">My Properties</a>
        </div>
      </div>
    </div>
  `,
})
export class OverviewComponent implements OnInit {
  private brokerService = inject(BrokerDataService);
  private destroyRef = inject(DestroyRef);

  profile: BrokerProfile | null = null;
  stats: BrokerStats | null = null;
  tasks: BrokerTask[] = [];
  activity: BrokerActivity[] = [];
  loading = true;

  ngOnInit() {
    this.brokerService
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((p) => (this.profile = p));

    this.brokerService
      .getStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((s) => (this.stats = s));

    this.brokerService
      .getTasks()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((t) => (this.tasks = t));

    this.brokerService
      .getRecentActivity()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((a) => {
        this.activity = a;
        this.loading = false;
      });
  }
}

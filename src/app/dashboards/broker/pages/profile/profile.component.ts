import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BrokerDataService } from '../../services/broker-data.service';
import { BrokerProfile } from '../../models/broker.models';

interface DaySchedule {
  day: string;
  enabled: boolean;
  from: string;
  to: string;
}

@Component({
  selector: 'app-broker-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styles: [
    `
      .page {
        padding: 2rem;
        max-width: 900px;
        margin: 0 auto;
      }
      .page-header {
        margin-bottom: 1.5rem;
      }
      .page-header h1 {
        font-size: 1.4rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0;
      }
      .card {
        background: #fff;
        border-radius: 12px;
        padding: 1.5rem;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
        margin-bottom: 1.5rem;
      }
      .card h3 {
        font-size: 1rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0 0 1.25rem;
        padding-bottom: 0.75rem;
        border-bottom: 1px solid #f0f0f0;
      }
      .photo-section {
        display: flex;
        align-items: center;
        gap: 1.5rem;
        margin-bottom: 1.5rem;
        flex-wrap: wrap;
      }
      .avatar {
        width: 80px;
        height: 80px;
        border-radius: 50%;
        background: #e9ecef;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 2rem;
        flex-shrink: 0;
        overflow: hidden;
      }
      .avatar img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }
      .photo-actions {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }
      .photo-actions label {
        font-size: 0.85rem;
        color: #6c757d;
      }
      .form-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
      }
      @media (max-width: 600px) {
        .form-grid {
          grid-template-columns: 1fr;
        }
      }
      .form-group {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
      }
      .form-group.full {
        grid-column: 1 / -1;
      }
      .form-group label {
        font-size: 0.82rem;
        font-weight: 500;
        color: #555;
      }
      .form-group input,
      .form-group textarea,
      .form-group select {
        padding: 0.55rem 0.85rem;
        border: 1.5px solid #dee2e6;
        border-radius: 8px;
        font-size: 0.88rem;
        font-family: inherit;
        color: #333;
        transition: border-color 0.15s;
      }
      .form-group input:focus,
      .form-group textarea:focus {
        outline: none;
        border-color: #2d6a4f;
      }
      .form-group textarea {
        resize: vertical;
        min-height: 80px;
      }
      .tags-input {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
        padding: 0.45rem;
        border: 1.5px solid #dee2e6;
        border-radius: 8px;
        min-height: 42px;
        cursor: text;
      }
      .tags-input:focus-within {
        border-color: #2d6a4f;
      }
      .tag {
        background: #d1fae5;
        color: #065f46;
        padding: 0.2rem 0.6rem;
        border-radius: 20px;
        font-size: 0.8rem;
        display: flex;
        align-items: center;
        gap: 0.3rem;
      }
      .tag button {
        background: none;
        border: none;
        cursor: pointer;
        color: #065f46;
        font-size: 0.9rem;
        padding: 0;
        line-height: 1;
      }
      .tags-input input {
        border: none;
        outline: none;
        font-size: 0.85rem;
        flex: 1;
        min-width: 120px;
      }
      .schedule-table {
        width: 100%;
        border-collapse: collapse;
      }
      .schedule-table th {
        text-align: left;
        font-size: 0.8rem;
        color: #888;
        font-weight: 500;
        padding: 0.4rem 0.5rem;
        border-bottom: 1px solid #f0f0f0;
      }
      .schedule-table td {
        padding: 0.6rem 0.5rem;
        border-bottom: 1px solid #f8f8f8;
        vertical-align: middle;
      }
      .schedule-table tr:last-child td {
        border-bottom: none;
      }
      .day-label {
        font-size: 0.88rem;
        font-weight: 500;
        color: #333;
        min-width: 90px;
      }
      .time-input {
        padding: 0.35rem 0.6rem;
        border: 1.5px solid #dee2e6;
        border-radius: 6px;
        font-size: 0.85rem;
        color: #333;
      }
      .time-input:focus {
        outline: none;
        border-color: #2d6a4f;
      }
      .time-input:disabled {
        background: #f8f9fa;
        color: #aaa;
      }
      .toggle {
        position: relative;
        display: inline-block;
        width: 36px;
        height: 20px;
      }
      .toggle input {
        opacity: 0;
        width: 0;
        height: 0;
      }
      .slider {
        position: absolute;
        cursor: pointer;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: #dee2e6;
        border-radius: 20px;
        transition: 0.3s;
      }
      .slider:before {
        position: absolute;
        content: '';
        height: 14px;
        width: 14px;
        left: 3px;
        bottom: 3px;
        background: #fff;
        border-radius: 50%;
        transition: 0.3s;
      }
      input:checked + .slider {
        background: #2d6a4f;
      }
      input:checked + .slider:before {
        transform: translateX(16px);
      }
      .form-actions {
        display: flex;
        gap: 0.75rem;
        margin-top: 1.25rem;
        flex-wrap: wrap;
      }
      .btn {
        padding: 0.55rem 1.25rem;
        border-radius: 8px;
        font-size: 0.88rem;
        font-weight: 500;
        cursor: pointer;
        border: none;
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
      .save-toast {
        position: fixed;
        bottom: 2rem;
        right: 2rem;
        background: #2d6a4f;
        color: #fff;
        padding: 0.75rem 1.25rem;
        border-radius: 10px;
        font-size: 0.88rem;
        font-weight: 500;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        z-index: 999;
        animation: slideUp 0.3s ease;
      }
      @keyframes slideUp {
        from {
          transform: translateY(20px);
          opacity: 0;
        }
        to {
          transform: translateY(0);
          opacity: 1;
        }
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
    `,
  ],
  template: `
    <div class="page">
      <div class="page-header">
        <h1>My Profile</h1>
      </div>

      @if (loading) {
        <div class="card">
          <div
            class="skeleton"
            style="height:80px; width:80px; border-radius:50%; margin-bottom:1rem;"
          ></div>
          @for (i of [1, 2, 3, 4]; track i) {
            <div class="skeleton" style="height:40px; margin-bottom:12px;"></div>
          }
        </div>
      } @else if (profile) {
        <!-- Personal Info -->
        <div class="card">
          <h3>Personal Information</h3>

          <div class="photo-section">
            <div class="avatar">
              @if (photoPreview) {
                <img [src]="photoPreview" alt="Profile photo" />
              } @else {
                👤
              }
            </div>
            <div class="photo-actions">
              <label>Profile Photo</label>
              <input type="file" accept="image/*" (change)="onPhotoChange($event)" />
            </div>
          </div>

          <div class="form-grid">
            <div class="form-group">
              <label>Full Name</label>
              <input type="text" [(ngModel)]="profile.name" />
            </div>
            <div class="form-group">
              <label>Email</label>
              <input type="email" [(ngModel)]="profile.email" />
            </div>
            <div class="form-group">
              <label>Phone</label>
              <input type="tel" [(ngModel)]="profile.phone" />
            </div>
            <div class="form-group">
              <label>License Number</label>
              <input type="text" [(ngModel)]="profile.license" />
            </div>
            <div class="form-group full">
              <label>Bio</label>
              <textarea [(ngModel)]="profile.bio"></textarea>
            </div>
            <div class="form-group">
              <label>Languages (press Enter to add)</label>
              <div class="tags-input" (click)="langInput.focus()">
                @for (lang of profile.languages; track lang) {
                  <span class="tag">
                    {{ lang }}
                    <button (click)="removeTag('languages', lang)">×</button>
                  </span>
                }
                <input
                  #langInput
                  type="text"
                  placeholder="Add language..."
                  (keydown.enter)="addTag('languages', langInput)"
                />
              </div>
            </div>
            <div class="form-group">
              <label>Specializations (press Enter to add)</label>
              <div class="tags-input" (click)="specInput.focus()">
                @for (spec of profile.specializations; track spec) {
                  <span class="tag">
                    {{ spec }}
                    <button (click)="removeTag('specializations', spec)">×</button>
                  </span>
                }
                <input
                  #specInput
                  type="text"
                  placeholder="Add specialization..."
                  (keydown.enter)="addTag('specializations', specInput)"
                />
              </div>
            </div>
          </div>

          <div class="form-actions">
            <button class="btn btn-primary" (click)="saveProfile()">Save Changes</button>
            <button class="btn btn-outline" (click)="changePassword()">Change Password</button>
          </div>
        </div>

        <!-- Availability Schedule -->
        <div class="card">
          <h3>Availability Schedule</h3>
          <table class="schedule-table">
            <thead>
              <tr>
                <th>Day</th>
                <th>Available</th>
                <th>From</th>
                <th>To</th>
              </tr>
            </thead>
            <tbody>
              @for (day of schedule; track day.day) {
                <tr>
                  <td class="day-label">{{ day.day }}</td>
                  <td>
                    <label class="toggle">
                      <input type="checkbox" [(ngModel)]="day.enabled" />
                      <span class="slider"></span>
                    </label>
                  </td>
                  <td>
                    <input
                      class="time-input"
                      type="time"
                      [(ngModel)]="day.from"
                      [disabled]="!day.enabled"
                    />
                  </td>
                  <td>
                    <input
                      class="time-input"
                      type="time"
                      [(ngModel)]="day.to"
                      [disabled]="!day.enabled"
                    />
                  </td>
                </tr>
              }
            </tbody>
          </table>
          <div class="form-actions">
            <button class="btn btn-primary" (click)="saveSchedule()">Save Schedule</button>
          </div>
        </div>
      }
    </div>

    @if (showToast) {
      <div class="save-toast">✅ {{ toastMessage }}</div>
    }
  `,
})
export class ProfileComponent implements OnInit {
  private brokerService = inject(BrokerDataService);
  private destroyRef = inject(DestroyRef);

  profile: BrokerProfile | null = null;
  loading = true;
  photoPreview: string | null = null;
  showToast = false;
  toastMessage = '';

  schedule: DaySchedule[] = [
    { day: 'Monday', enabled: true, from: '09:00', to: '17:00' },
    { day: 'Tuesday', enabled: true, from: '09:00', to: '17:00' },
    { day: 'Wednesday', enabled: true, from: '09:00', to: '17:00' },
    { day: 'Thursday', enabled: true, from: '09:00', to: '17:00' },
    { day: 'Friday', enabled: true, from: '09:00', to: '14:00' },
    { day: 'Saturday', enabled: false, from: '10:00', to: '14:00' },
    { day: 'Sunday', enabled: false, from: '10:00', to: '14:00' },
  ];

  ngOnInit() {
    this.brokerService
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((p) => {
        this.profile = { ...p };
        this.loading = false;
      });
  }

  onPhotoChange(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => (this.photoPreview = reader.result as string);
    reader.readAsDataURL(file);
  }

  addTag(field: 'languages' | 'specializations', input: HTMLInputElement) {
    const val = input.value.trim();
    if (!val || !this.profile) return;
    if (!this.profile[field].includes(val)) {
      this.profile[field] = [...this.profile[field], val];
    }
    input.value = '';
  }

  removeTag(field: 'languages' | 'specializations', val: string) {
    if (!this.profile) return;
    this.profile[field] = this.profile[field].filter((t) => t !== val);
  }

  saveProfile() {
    this.toast('Profile saved successfully!');
  }

  saveSchedule() {
    this.toast('Schedule saved successfully!');
  }

  changePassword() {
    alert('Change password flow — connect to auth service.');
  }

  private toast(msg: string) {
    this.toastMessage = msg;
    this.showToast = true;
    setTimeout(() => (this.showToast = false), 3000);
  }
}

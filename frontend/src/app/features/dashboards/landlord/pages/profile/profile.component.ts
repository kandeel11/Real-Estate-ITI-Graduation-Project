import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../../../core/services/auth.service';

@Component({
  selector: 'app-landlord-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  styles: [`
    * { box-sizing: border-box; margin: 0; padding: 0; }
    .page { font-family: 'Inter', 'Segoe UI', sans-serif; }

    .page-header { margin-bottom: 28px; }
    .page-header h1 { font-size: 28px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px; }
    .page-header p { font-size: 14px; color: #64748b; margin-top: 4px; }

    .grid { display: grid; grid-template-columns: 300px 1fr; gap: 20px; }

    .card {
      background: white; border-radius: 16px; padding: 24px;
      border: 1px solid #f1f5f9;
      box-shadow: 0 2px 8px rgba(0,0,0,0.04);
    }

    /* AVATAR CARD */
    .avatar-card { text-align: center; }
    .avatar {
      width: 96px; height: 96px; border-radius: 50%;
      background: linear-gradient(135deg, #2563eb, #1e40af);
      color: white; font-size: 36px; font-weight: 700;
      display: flex; align-items: center; justify-content: center;
      margin: 0 auto 16px;
    }
    .user-name { font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 4px; }
    .user-role {
      display: inline-block; background: #eff6ff; color: #2563eb;
      padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600;
      margin-bottom: 20px;
    }
    .btn-upload {
      width: 100%; padding: 10px; border: 1.5px dashed #e2e8f0;
      border-radius: 10px; font-size: 13px; font-weight: 600;
      color: #64748b; background: #f8fafc; cursor: pointer;
      transition: all 0.2s; margin-bottom: 20px;
    }
    .btn-upload:hover { border-color: #2563eb; color: #2563eb; }
    .profile-stats { display: flex; flex-direction: column; gap: 12px; }
    .profile-stat { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #f8fafc; }
    .profile-stat:last-child { border-bottom: none; }
    .stat-key { font-size: 13px; color: #64748b; }
    .stat-val { font-size: 13px; font-weight: 600; color: #0f172a; }

    /* FORM */
    .form-card h3 { font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 20px; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .form-field { display: flex; flex-direction: column; gap: 6px; }
    .form-field.full { grid-column: span 2; }
    .form-field label { font-size: 13px; font-weight: 600; color: #374151; }
    .form-field input, .form-field select, .form-field textarea {
      padding: 11px 14px; border: 1.5px solid #e2e8f0;
      border-radius: 10px; font-size: 14px; color: #0f172a;
      background: #f8fafc; outline: none; transition: all 0.2s;
      font-family: inherit;
    }
    .form-field input:focus, .form-field select:focus, .form-field textarea:focus {
      border-color: #2563eb; background: white;
      box-shadow: 0 0 0 3px rgba(37,99,235,0.08);
    }
    .form-field textarea { resize: vertical; min-height: 80px; }
    .form-divider { grid-column: span 2; height: 1px; background: #f1f5f9; margin: 8px 0; }
    .form-actions { display: flex; gap: 10px; justify-content: flex-end; margin-top: 20px; }
    .btn-save {
      padding: 10px 24px; background: #2563eb; border: none;
      border-radius: 10px; font-size: 14px; font-weight: 600;
      color: white; cursor: pointer; transition: all 0.2s;
    }
    .btn-save:hover { background: #1d4ed8; }
    .btn-discard {
      padding: 10px 20px; background: white; border: 1.5px solid #e2e8f0;
      border-radius: 10px; font-size: 14px; font-weight: 600;
      color: #374151; cursor: pointer; transition: all 0.2s;
    }
    .btn-discard:hover { border-color: #94a3b8; }

    .success-msg {
      background: #f0fdf4; border: 1px solid #bbf7d0; color: #16a34a;
      padding: 12px 16px; border-radius: 10px; font-size: 13px;
      font-weight: 600; margin-bottom: 16px;
    }
  `],
  template: `
    <div class="page">

      <div class="page-header">
        <h1>My Profile</h1>
        <p>Manage your personal information and account settings.</p>
      </div>

      <div class="grid">

        <!-- AVATAR CARD -->
        <div class="card avatar-card">
          <div class="avatar">{{ initial() }}</div>
          <div class="user-name">{{ auth.user()?.fullName }}</div>
          <div class="user-role">Premium Landlord</div>
          <button class="btn-upload">📷 Upload Photo</button>
          <div class="profile-stats">
            <div class="profile-stat">
              <span class="stat-key">Member Since</span>
              <span class="stat-val">Jan 2024</span>
            </div>
            <div class="profile-stat">
              <span class="stat-key">Properties</span>
              <span class="stat-val">12</span>
            </div>
            <div class="profile-stat">
              <span class="stat-key">Total Views</span>
              <span class="stat-val">24,502</span>
            </div>
            <div class="profile-stat">
              <span class="stat-key">Active Deals</span>
              <span class="stat-val">3</span>
            </div>
          </div>
        </div>

        <!-- FORM CARD -->
        <div class="card form-card">
          <h3>Personal Information</h3>

          @if (saved()) {
            <div class="success-msg">✓ Profile updated successfully!</div>
          }

          <form [formGroup]="form" (ngSubmit)="onSave()">
            <div class="form-grid">
              <div class="form-field">
                <label>Full Name</label>
                <input type="text" formControlName="fullName" placeholder="Your full name" />
              </div>
              <div class="form-field">
                <label>Email Address</label>
                <input type="email" formControlName="email" placeholder="your@email.com" />
              </div>
              <div class="form-field">
                <label>Phone Number</label>
                <input type="tel" formControlName="phone" placeholder="+20 1XX XXX XXXX" />
              </div>
              <div class="form-field">
                <label>City</label>
                <select formControlName="city">
                  <option>Cairo</option>
                  <option>Alexandria</option>
                  <option>Giza</option>
                  <option>North Coast</option>
                </select>
              </div>
              <div class="form-field full">
                <label>Address</label>
                <input type="text" formControlName="address" placeholder="Street address" />
              </div>
              <div class="form-field full">
                <label>Bio</label>
                <textarea formControlName="bio" placeholder="Tell tenants about yourself..."></textarea>
              </div>

              <div class="form-divider"></div>

              <div class="form-field">
                <label>New Password</label>
                <input type="password" formControlName="password" placeholder="Leave blank to keep current" />
              </div>
              <div class="form-field">
                <label>Confirm Password</label>
                <input type="password" formControlName="confirmPassword" placeholder="Repeat new password" />
              </div>
            </div>

            <div class="form-actions">
              <button type="button" class="btn-discard" (click)="onDiscard()">Discard</button>
              <button type="submit" class="btn-save">Save Changes</button>
            </div>
          </form>
        </div>

      </div>

    </div>
  `,
})
export class LandlordProfileComponent {
  auth = inject(AuthService);
  private fb = inject(FormBuilder);

  saved = signal(false);

  initial = () => this.auth.user()?.fullName?.[0]?.toUpperCase() ?? 'U';

  form = this.fb.group({
    fullName: [this.auth.user()?.fullName ?? ''],
    email: [this.auth.user()?.email ?? ''],
    phone: [''],
    city: ['Cairo'],
    address: [''],
    bio: [''],
    password: [''],
    confirmPassword: [''],
  });

  onSave() {
    this.saved.set(true);
    setTimeout(() => this.saved.set(false), 3000);
  }

  onDiscard() {
    this.form.reset();
  }
}
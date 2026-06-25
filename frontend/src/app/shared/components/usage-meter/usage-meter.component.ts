/**
 * Usage Meter
 * Reusable subscription usage progress card. 999999+ means "unlimited"
 * (backend convention for plan limits).
 */

import { Component, input, computed } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-usage-meter',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="card p-3 h-100">
      <div class="d-flex justify-content-between mb-2">
        <span class="fw-semibold small">{{ label() }}</span>
        <span class="small" [class.text-danger]="percent() >= 80">
          {{ used() }} / {{ isUnlimited() ? '∞' : max() }}
        </span>
      </div>
      <div class="progress" style="height: 6px;">
        <div
          class="progress-bar"
          [class.bg-success]="percent() < 70"
          [class.bg-warning]="percent() >= 70 && percent() < 90"
          [class.bg-danger]="percent() >= 90"
          [style.width.%]="percent()"
        ></div>
      </div>
      @if (percent() >= 80 && !isUnlimited()) {
        <small class="text-muted mt-2 d-block">
          Approaching limit —
          <a routerLink="/dashboard/company/subscriptions">upgrade →</a>
        </small>
      }
    </div>
  `,
})
export class UsageMeterComponent {
  label = input.required<string>();
  used = input.required<number>();
  max = input.required<number>();

  isUnlimited = computed(() => this.max() >= 999999);
  percent = computed(() =>
    this.isUnlimited() ? 0 : Math.min((this.used() / this.max()) * 100, 100)
  );
}

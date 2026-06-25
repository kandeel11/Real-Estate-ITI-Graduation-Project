/**
 * Over-Limit Banner
 * Shown when current usage exceeds a plan limit — the "grandfather" state
 * after a downgrade. Existing items stay active; new ones are paused until
 * usage drops back under the limit (or the plan is upgraded).
 *
 *   <app-over-limit-banner [used]="totalProperties" [max]="maxProperties" label="properties" />
 *
 * Pass max as 999999+ (the unlimited sentinel) or anything ≥ used to hide it.
 */

import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-over-limit-banner',
  standalone: true,
  imports: [RouterLink],
  template: `
    @if (isOver()) {
      <div class="alert alert-warning d-flex justify-content-between align-items-center flex-wrap gap-2">
        <span>
          <i class="fa-solid fa-triangle-exclamation me-2"></i>
          You're over your plan's {{ label() }} limit —
          <strong>{{ used() }} of {{ max() }}</strong>.
          Your existing {{ label() }} stay active, but new ones are paused until
          you're back under {{ max() }}.
        </span>
        @if (showUpgrade()) {
          <a [routerLink]="upgradeRoute()" class="btn btn-dark btn-sm text-nowrap">
            <i class="fa-solid fa-crown me-1"></i>Upgrade
          </a>
        }
      </div>
    }
  `,
})
export class OverLimitBannerComponent {
  used = input.required<number>();
  max = input.required<number>();
  label = input.required<string>();
  upgradeRoute = input('/dashboard/company/subscriptions');
  showUpgrade = input(true);

  /** 999999 = unlimited sentinel; never "over" on unlimited plans. */
  isOver = computed(() => this.max() < 999999 && this.used() > this.max());
}

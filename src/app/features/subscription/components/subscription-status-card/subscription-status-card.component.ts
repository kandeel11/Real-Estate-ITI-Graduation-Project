import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-subscription-status-card',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './subscription-status-card.component.html',
  styleUrl: './subscription-status-card.component.css',
})
export class SubscriptionStatusCardComponent {
  // Hardcoded until a "get my subscription" endpoint exists
  readonly planName = 'Free Tenant';
  readonly planTier = 'FreeTenant';
  readonly status = 'Active';
  readonly aiRequestsUsed = null;
  readonly aiRequestsTotal = 20;
}

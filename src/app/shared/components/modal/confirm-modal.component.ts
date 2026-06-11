/**
 * Confirm Modal
 * Bootstrap-styled replacement for native confirm(). Render it conditionally —
 * the parent owns visibility:
 *
 *   @if (pendingDelete(); as item) {
 *     <app-confirm-modal
 *       title="Delete item"
 *       [message]="'Delete ' + item.name + '?'"
 *       confirmText="Delete"
 *       (confirmed)="doDelete(item)"
 *       (cancelled)="pendingDelete.set(null)" />
 *   }
 */

import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  template: `
    <div class="modal d-block" tabindex="-1" role="dialog" (click)="cancelled.emit()">
      <div class="modal-dialog modal-dialog-centered" (click)="$event.stopPropagation()">
        <div class="modal-content">
          <div class="modal-header border-0 pb-0">
            <h5 class="modal-title">
              @if (variant() === 'danger') {
                <i class="fa-solid fa-triangle-exclamation text-danger me-2"></i>
              }
              {{ title() }}
            </h5>
            <button type="button" class="btn-close" (click)="cancelled.emit()"></button>
          </div>
          <div class="modal-body text-muted">{{ message() }}</div>
          <div class="modal-footer border-0 pt-0">
            <button type="button" class="btn btn-outline-secondary" (click)="cancelled.emit()">
              {{ cancelText() }}
            </button>
            <button
              type="button"
              class="btn"
              [class.btn-danger]="variant() === 'danger'"
              [class.btn-dark]="variant() !== 'danger'"
              (click)="confirmed.emit()"
            >
              {{ confirmText() }}
            </button>
          </div>
        </div>
      </div>
    </div>
    <div class="modal-backdrop show"></div>
  `,
  styles: [
    `
      .modal.d-block {
        background: transparent;
        z-index: 1060;
      }
      .modal-backdrop {
        z-index: 1055;
      }
    `,
  ],
})
export class ConfirmModalComponent {
  title = input.required<string>();
  message = input.required<string>();
  confirmText = input('Confirm');
  cancelText = input('Cancel');
  variant = input<'danger' | 'dark'>('danger');

  confirmed = output<void>();
  cancelled = output<void>();
}

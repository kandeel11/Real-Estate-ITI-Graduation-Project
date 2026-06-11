/**
 * Confirm Modal
 * Bootstrap-styled replacement for native confirm(). Always render it and
 * control visibility through [show]:
 *
 *   <app-confirm-modal
 *     [show]="pendingDelete() !== null"
 *     title="Delete item"
 *     [message]="deleteMessage()"
 *     confirmLabel="Delete"
 *     confirmClass="btn-danger"
 *     [loading]="deleting()"
 *     (confirmed)="doDelete()"
 *     (cancelled)="pendingDelete.set(null)" />
 */

import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  templateUrl: './confirm-modal.component.html',
  styleUrl: './confirm-modal.component.css',
})
export class ConfirmModalComponent {
  @Input() show = false;
  @Input() title = 'Are you sure?';
  @Input() message = '';
  @Input() confirmLabel = 'Confirm';
  @Input() confirmClass = 'btn-primary';
  @Input() loading = false;

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();
}

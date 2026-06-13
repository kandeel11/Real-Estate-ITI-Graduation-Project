import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  show(type: ToastType, message: string, duration = 3500): void {
    const id = crypto.randomUUID();
    this._toasts.update(list => [...list, { id, type, message }]);
    setTimeout(() => this.dismiss(id), duration);
  }

  dismiss(id: string): void {
    this._toasts.update(list => list.filter(t => t.id !== id));
  }

  success(message: string) { this.show('success', message); }
  error(message: string)   { this.show('error', message); }
  warning(message: string) { this.show('warning', message); }
  info(message: string)    { this.show('info', message); }
}

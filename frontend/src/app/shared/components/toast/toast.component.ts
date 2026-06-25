import { Component, inject } from '@angular/core';
import { ToastService, Toast } from './toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.css',
})
export class ToastComponent {
  protected readonly toastService = inject(ToastService);

  icon(type: Toast['type']): string {
    const map: Record<Toast['type'], string> = {
      success: 'fa-solid fa-circle-check',
      error:   'fa-solid fa-circle-xmark',
      warning: 'fa-solid fa-triangle-exclamation',
      info:    'fa-solid fa-circle-info',
    };
    return map[type];
  }
}

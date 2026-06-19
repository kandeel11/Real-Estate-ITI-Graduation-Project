import { Component, Input, Output, EventEmitter, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { PropertyService } from '../../../../../features/properties/services/property.service';
import { PropertyMedia } from '../../../../../features/properties/models/property.model';
import { ToastService } from '../../../../../shared/components/toast/toast.service';

/**
 * Manage the photos of a single property: upload new images, choose the primary
 * (the one shown on cards / listing pages), and delete. Mirrors the wizard's
 * photo step but reusable from the properties table.
 */
@Component({
  selector: 'app-owner-manage-images-modal',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './manage-images-modal.component.html',
  styleUrl: './manage-images-modal.component.css',
})
export class ManageImagesModalComponent implements OnInit {
  @Input({ required: true }) propertyId!: string;
  @Input() propertyTitle = '';
  @Output() closed = new EventEmitter<void>();
  /** Emitted whenever media changed, so the parent can refresh its list/thumbnails. */
  @Output() changed = new EventEmitter<void>();

  private readonly svc = inject(PropertyService);
  private readonly toast = inject(ToastService);

  readonly media = signal<PropertyMedia[]>([]);
  readonly loading = signal(true);
  readonly uploading = signal(false);
  readonly busyId = signal<string | null>(null);
  private dirty = false;

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.svc.getById(this.propertyId).subscribe({
      next: d => {
        this.media.set([...(d.media ?? [])].sort((a, b) => a.sortOrder - b.sortOrder));
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.toast.error('Could not load images.'); },
    });
  }

  onFileChange(e: Event): void {
    const input = e.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (files.length === 0) return;
    this.uploadNext(files, 0);
    input.value = '';
  }

  private uploadNext(files: File[], index: number): void {
    if (index >= files.length) { this.uploading.set(false); return; }
    this.uploading.set(true);
    const sortOrder = this.media().length + index;
    this.svc.uploadMedia(this.propertyId, files[index], sortOrder).subscribe({
      next: () => { this.dirty = true; this.uploadNext(files, index + 1); },
      error: (err) => {
        this.uploading.set(false);
        this.toast.error(err?.error?.error || 'Upload failed.');
        if (this.dirty) this.load();
      },
      complete: () => {
        if (index === files.length - 1) this.load();
      },
    });
  }

  setPrimary(m: PropertyMedia): void {
    if (m.isPrimary || this.busyId()) return;
    this.busyId.set(m.id);
    this.svc.setPrimaryMedia(this.propertyId, m.id).subscribe({
      next: () => {
        this.dirty = true;
        this.media.update(list => list.map(x => ({ ...x, isPrimary: x.id === m.id })));
        this.busyId.set(null);
        this.toast.success('Primary image updated.');
      },
      error: (err) => { this.busyId.set(null); this.toast.error(err?.error?.error || 'Could not set primary image.'); },
    });
  }

  remove(m: PropertyMedia): void {
    if (this.busyId()) return;
    this.busyId.set(m.id);
    this.svc.deleteMedia(this.propertyId, m.id).subscribe({
      next: () => {
        this.dirty = true;
        this.media.update(list => list.filter(x => x.id !== m.id));
        this.busyId.set(null);
      },
      error: (err) => { this.busyId.set(null); this.toast.error(err?.error?.error || 'Could not delete image.'); },
    });
  }

  close(): void {
    if (this.dirty) this.changed.emit();
    this.closed.emit();
  }
}

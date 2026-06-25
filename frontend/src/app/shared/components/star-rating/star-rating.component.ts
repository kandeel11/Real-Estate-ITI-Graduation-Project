import { Component, Input, Output, EventEmitter, signal } from '@angular/core';

@Component({
  selector: 'app-star-rating',
  standalone: true,
  imports: [],
  templateUrl: './star-rating.component.html',
  styleUrl: './star-rating.component.css',
})
export class StarRatingComponent {
  @Input() value = 0;
  @Input() interactive = false;
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Output() valueChange = new EventEmitter<number>();

  protected hoveredStar = signal(0);
  protected readonly stars = [1, 2, 3, 4, 5];

  protected starIcon(index: number): string {
    const displayVal = this.interactive
      ? (this.hoveredStar() || this.value)
      : this.value;
    if (index <= Math.floor(displayVal)) return 'fa-solid fa-star';
    if (!this.interactive && index === Math.ceil(displayVal) && displayVal % 1 >= 0.5) {
      return 'fa-solid fa-star-half-stroke';
    }
    return 'fa-regular fa-star';
  }

  protected select(star: number): void {
    if (!this.interactive) return;
    this.valueChange.emit(star);
  }

  protected hover(star: number): void {
    if (!this.interactive) return;
    this.hoveredStar.set(star);
  }

  protected leave(): void {
    this.hoveredStar.set(0);
  }
}

import { Directive, ElementRef, Input, OnDestroy, OnInit } from '@angular/core';

@Directive({
  selector: '[inView]',
  standalone: true,
  host: { class: 'in-view' }
})
export class InViewDirective implements OnInit, OnDestroy {
  @Input() inViewDelay = 0;
  private observer!: IntersectionObserver;

  constructor(private el: ElementRef) {}

  ngOnInit() {
    const el = this.el.nativeElement as HTMLElement;
    if (this.inViewDelay) {
      el.style.transitionDelay = `${this.inViewDelay}ms`;
    }
    this.observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          el.classList.add('visible');
          // Clear the stagger delay once the animation is done so hover effects aren't affected
          const clearDelay = this.inViewDelay + 600;
          setTimeout(() => (el.style.transitionDelay = ''), clearDelay);
          this.observer.disconnect();
        }
      },
      { threshold: 0.1 }
    );
    this.observer.observe(el);
  }

  ngOnDestroy() {
    this.observer?.disconnect();
  }
}

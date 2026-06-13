import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { OverLimitBannerComponent } from './over-limit-banner.component';

describe('OverLimitBannerComponent', () => {
  let fixture: ComponentFixture<OverLimitBannerComponent>;
  let component: OverLimitBannerComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OverLimitBannerComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(OverLimitBannerComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('label', 'properties');
  });

  it('should render only when used exceeds max', () => {
    fixture.componentRef.setInput('used', 12);
    fixture.componentRef.setInput('max', 10);
    fixture.detectChanges();
    expect(component.isOver()).toBe(true);
    expect(fixture.nativeElement.querySelector('.alert')).not.toBeNull();
  });

  it('should stay hidden at or under the limit', () => {
    fixture.componentRef.setInput('used', 10);
    fixture.componentRef.setInput('max', 10);
    fixture.detectChanges();
    expect(component.isOver()).toBe(false);
    expect(fixture.nativeElement.querySelector('.alert')).toBeNull();
  });

  it('should stay hidden for unlimited plans', () => {
    fixture.componentRef.setInput('used', 5000);
    fixture.componentRef.setInput('max', 999999);
    fixture.detectChanges();
    expect(component.isOver()).toBe(false);
  });
});

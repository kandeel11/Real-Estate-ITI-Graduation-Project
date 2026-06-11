import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { UsageMeterComponent } from './usage-meter.component';

describe('UsageMeterComponent', () => {
  let fixture: ComponentFixture<UsageMeterComponent>;
  let component: UsageMeterComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UsageMeterComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(UsageMeterComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('label', 'Properties');
    fixture.componentRef.setInput('used', 147);
    fixture.componentRef.setInput('max', 200);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should compute percent from used/max', () => {
    expect(component.percent()).toBeCloseTo(73.5);
    expect(component.isUnlimited()).toBe(false);
  });

  it('should treat 999999 as unlimited', () => {
    fixture.componentRef.setInput('max', 999999);
    fixture.detectChanges();
    expect(component.isUnlimited()).toBe(true);
    expect(component.percent()).toBe(0);
  });
});

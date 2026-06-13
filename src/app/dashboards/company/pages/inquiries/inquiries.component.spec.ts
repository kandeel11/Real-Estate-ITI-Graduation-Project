import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { InquiriesComponent } from './inquiries.component';

describe('Company InquiriesComponent', () => {
  let component: InquiriesComponent;
  let fixture: ComponentFixture<InquiriesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InquiriesComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(InquiriesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should filter by status', () => {
    component.inquiries.set([
      { id: '1', status: 'Pending' } as never,
      { id: '2', status: 'Accepted' } as never,
    ]);
    component.statusFilter.set('Accepted');
    expect(component.filtered().length).toBe(1);
    expect(component.counts().all).toBe(2);
  });
});

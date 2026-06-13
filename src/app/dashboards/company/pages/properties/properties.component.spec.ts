import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { PropertiesComponent } from './properties.component';

describe('Company PropertiesComponent', () => {
  let component: PropertiesComponent;
  let fixture: ComponentFixture<PropertiesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PropertiesComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertiesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should filter by status', () => {
    component.properties.set([
      { id: '1', status: 'Active', title: 'A' } as never,
      { id: '2', status: 'Draft', title: 'B' } as never,
    ]);
    component.statusFilter.set('Active');
    expect(component.filtered().length).toBe(1);
    expect(component.counts().all).toBe(2);
  });
});

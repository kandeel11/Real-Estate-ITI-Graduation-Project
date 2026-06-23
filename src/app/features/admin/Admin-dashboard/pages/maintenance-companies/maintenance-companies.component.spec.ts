import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MaintenanceCompaniesComponent } from './maintenance-companies.component';

describe('MaintenanceCompaniesComponent', () => {
  let component: MaintenanceCompaniesComponent;
  let fixture: ComponentFixture<MaintenanceCompaniesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MaintenanceCompaniesComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MaintenanceCompaniesComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

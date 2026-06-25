import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MaintenanceSidebarComponent } from './maintenance-sidebar.component';

describe('MaintenanceSidebarComponent', () => {
  let component: MaintenanceSidebarComponent;
  let fixture: ComponentFixture<MaintenanceSidebarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MaintenanceSidebarComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MaintenanceSidebarComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

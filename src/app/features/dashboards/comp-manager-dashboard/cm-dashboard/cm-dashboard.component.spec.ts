import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CmDashboardComponent } from './cm-dashboard.component';

describe('CmDashboardComponent', () => {
  let component: CmDashboardComponent;
  let fixture: ComponentFixture<CmDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CmDashboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CmDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

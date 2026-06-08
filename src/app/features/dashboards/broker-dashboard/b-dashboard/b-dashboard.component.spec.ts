import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BDashboardComponent } from './b-dashboard.component';

describe('BDashboardComponent', () => {
  let component: BDashboardComponent;
  let fixture: ComponentFixture<BDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BDashboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

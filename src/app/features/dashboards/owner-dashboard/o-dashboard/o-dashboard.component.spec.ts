import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ODashboardComponent } from './o-dashboard.component';

describe('ODashboardComponent', () => {
  let component: ODashboardComponent;
  let fixture: ComponentFixture<ODashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ODashboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ODashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

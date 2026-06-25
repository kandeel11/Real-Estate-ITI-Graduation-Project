import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { BrokersComponent } from './brokers.component';

describe('Company BrokersComponent', () => {
  let component: BrokersComponent;
  let fixture: ComponentFixture<BrokersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrokersComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(BrokersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not submit an invalid form', () => {
    component.submit();
    expect(component.form.touched).toBe(true);
    expect(component.submitting()).toBe(false);
  });
});

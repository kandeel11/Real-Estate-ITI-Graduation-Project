import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { CreatePropertyComponent } from './create-property.component';

describe('CreatePropertyComponent', () => {
  let component: CreatePropertyComponent;
  let fixture: ComponentFixture<CreatePropertyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreatePropertyComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(CreatePropertyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should start on step 1 and block next() on an invalid form', () => {
    expect(component.step()).toBe(1);
    component.next();
    expect(component.step()).toBe(1);
    expect(component.form.touched).toBe(true);
  });

  it('should not require ownership fields for non-owner roles', () => {
    // No auth_user in localStorage → role null → broker-style flow (no docs)
    expect(component.isOwner()).toBe(false);
    // ownerFullName stays optional, so an empty value is valid
    expect(component.form.get('ownerFullName')!.valid).toBe(true);
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { MyListingsComponent } from './my-listings.component';

describe('MyListingsComponent', () => {
  let component: MyListingsComponent;
  let fixture: ComponentFixture<MyListingsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyListingsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(MyListingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should count drafts', () => {
    component.properties.set([
      { id: '1', status: 'Draft', title: 'a', createdAt: '2026-01-01' } as never,
      { id: '2', status: 'Active', title: 'b', createdAt: '2026-01-02' } as never,
    ]);
    expect(component.draftCount()).toBe(1);
  });
});

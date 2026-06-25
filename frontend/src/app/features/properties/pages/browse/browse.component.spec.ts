import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { BrowseComponent } from './browse.component';

describe('BrowseComponent', () => {
  let component: BrowseComponent;
  let fixture: ComponentFixture<BrowseComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrowseComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(BrowseComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should filter by type (multi-select)', () => {
    component.all.set([
      { id: '1', propertyType: 'Villa', city: 'Cairo', title: 'a', titleAr: '', district: 'M', bedrooms: 3, price: 5000 } as never,
      { id: '2', propertyType: 'Apartment', city: 'Giza', title: 'b', titleAr: '', district: 'D', bedrooms: 2, price: 3000 } as never,
    ]);
    component.toggleType('Villa');
    expect(component.filtered().length).toBe(1);
    expect(component.cityOptions().length).toBe(2);
  });

  it('should match bedrooms with N+ thresholds and surface chips', () => {
    component.all.set([
      { id: '1', propertyType: 'Villa', city: 'Cairo', title: 'a', titleAr: '', district: 'M', bedrooms: 3, price: 5000 } as never,
      { id: '2', propertyType: 'Apartment', city: 'Giza', title: 'b', titleAr: '', district: 'D', bedrooms: 1, price: 3000 } as never,
    ]);
    component.toggleBeds(2);
    expect(component.filtered().length).toBe(1);
    expect(component.chips().length).toBe(1);
    component.removeChip(component.chips()[0]);
    expect(component.filtered().length).toBe(2);
  });
});

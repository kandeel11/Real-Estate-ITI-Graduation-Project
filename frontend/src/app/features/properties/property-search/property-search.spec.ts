import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PropertySearch } from './property-search';

describe('PropertySearch', () => {
  let component: PropertySearch;
  let fixture: ComponentFixture<PropertySearch>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PropertySearch],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertySearch);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

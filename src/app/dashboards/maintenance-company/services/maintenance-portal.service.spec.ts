import { TestBed } from '@angular/core/testing';

import { MaintenancePortalService } from './maintenance-portal.service';

describe('MaintenancePortalService', () => {
  let service: MaintenancePortalService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MaintenancePortalService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

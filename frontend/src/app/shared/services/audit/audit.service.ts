import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { AuditEntry, AuditFilters } from './audit.types';

@Service()
export class AuditService {
  apiService = inject(ApiService);

  /**
   * The security trail, as reactive state. Admin only; the API answers 403
   * for everyone else.
   *
   * @param filters Returns the query parameters.
   * @returns The resource holding the page.
   */
  getAuditResource = (filters: () => AuditFilters = () => ({})) => {
    return this.apiService.getPagedResource<AuditEntry>('/Audit', filters);
  }
}

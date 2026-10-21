import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { CreateLeaveRequest, Leave, LeaveFilters, UpdateLeaveRequest } from './leave.types';

@Service()
export class LeaveService {
  apiService = inject(ApiService);

  /**
   * Get Leaves from Http Resource
   *
   * @param filters
   * @returns
   */
  getLeavesResource = (filters: () => LeaveFilters = () => ({})) => this.apiService.getPagedResource<Leave>("/Leaves", filters);

  /**
   * Create Leave
   *
   * @param body
   * @returns
   */
  createLeave = (body: CreateLeaveRequest) => this.apiService.post<Leave>("/Leaves", body);

  /**
   * Approve Leave
   *
   * @param id Leave id
   * @returns
   */
  approveLeave = (id: number) => this.apiService.patch(`/Leaves/approve/${id}`);

  /**
   * Reject Leave
   *
   * @param id Leave id
   * @returns
   */
  rejectLeave = (id: number) => this.apiService.patch(`/Leaves/reject/${id}`);
}

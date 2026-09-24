import { computed, inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { WorkingDay } from './working-days.types';

@Service()
export class WorkingDaysService {
  apiService = inject(ApiService);

  readonly DAY_NAMES = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];

  workingDaysResource = this.apiService.getPagedResource<WorkingDay>('/WorkingDays');

  workingDays = computed(() => this.workingDaysResource.value().items);
  isLoading = computed(() => this.workingDaysResource.isLoading);
  hasFailed = computed(() => !!this.workingDaysResource.error());

  /**
   * @param date Any day.
   * @returns The clinic's row for that weekday, or undefined before the list loads.
   */
  findByDate = (date: Date): WorkingDay | undefined => this.workingDays().find((day) => day.workingDay.toLowerCase() === this.DAY_NAMES[date.getDay()]);

  /**
   * @param date Any day.
   * @returns Whether the clinic opens then.
   */
  isWorkingDay = (date: Date): boolean => this.findByDate(date)?.isWorkingDay ?? true;

  /**
   * Refetch working days from API
   */
  reload = () => this.workingDaysResource.reload();
}

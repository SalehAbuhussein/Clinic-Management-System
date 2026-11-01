import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { dayRange } from '@core/utils/day.utils';

import { Stats } from './stats.types';

@Service()
export class StatsService {
  apiService = inject(ApiService);

  /**
   * The stats screen's numbers for a range of days.
   *
   * Every boundary is one of this browser's midnights, the same as every other
   * day-scoped call, so the server never has to decide where a day starts.
   *
   * @param from Returns the first day of the chart.
   * @param to Returns the last day, inclusive; the call sends the midnight after
   * it, because the range the API wants is half-open.
   * @returns The resource holding the stats.
   */
  getStatsResource = (from: () => Date, to: () => Date) => {
    return this.apiService.getResource<Stats>('/Stats', () => ({
      todayFrom: dayRange(new Date()).from,
      from: dayRange(from()).from,
      to: dayRange(to()).to,
    }));
  }
}

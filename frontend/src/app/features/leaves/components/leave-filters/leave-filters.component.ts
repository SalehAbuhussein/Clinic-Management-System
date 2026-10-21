import { ChangeDetectionStrategy, Component, computed, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatOption, MatSelect } from '@angular/material/select';

import { LeaveFilters, LeaveStatus } from '@shared/services/leave/leave.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatFormField, MatLabel, MatOption, MatSelect],
  selector: 'app-leave-filters',
  styleUrl: './leave-filters.component.scss',
  templateUrl: './leave-filters.component.html',
})
export class LeaveFiltersComponent {
  filters = model<LeaveFilters>({});

  status = signal('all');

  hasValue = computed(() => this.status() !== 'all');

  /**
   * Filter by status. The codes match the API, where 0 is pending, 1 approved
   * and 2 rejected.
   *
   * @param value A status code, or `all` for no filter.
   */
  setStatus(value: `${LeaveStatus}` | 'all'): void {
    this.status.set(value);
    this.filters.set(value === 'all' ? {} : { status: value });
  }
}

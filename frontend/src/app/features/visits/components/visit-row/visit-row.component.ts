import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';

import { Visit } from '@shared/services/visit/visit.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatIcon],
  selector: 'app-visit-row',
  styleUrl: './visit-row.component.scss',
  templateUrl: './visit-row.component.html',
})
export class VisitRowComponent {
  visit = input.required<Visit>();

  open = output<Visit>();

  /** A visit is open until it is checked out; there is no other state to read. */
  isOpen = computed(() => !this.visit().checkoutTime);
}

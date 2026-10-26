import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIcon],
  selector: 'app-appointments-list-empty-state',
  styleUrl: './appointments-list-empty-state.component.scss',
  templateUrl: './appointments-list-empty-state.component.html',
})
export class AppointmentsListEmptyStateComponent {
  day = input<string>('');
  canBook = input(true);

  book = output<void>();
}

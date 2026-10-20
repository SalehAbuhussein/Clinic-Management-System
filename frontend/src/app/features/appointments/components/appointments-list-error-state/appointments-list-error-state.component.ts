import { Component, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

@Component({
  host: {
    role: 'alert',
  },
  imports: [MatButton, MatIcon],
  selector: 'app-appointments-list-error-state',
  styleUrl: './appointments-list-error-state.component.scss',
  templateUrl: './appointments-list-error-state.component.html',
})
export class AppointmentsListErrorStateComponent {
  message = input.required<string>();
  retry = output<void>();
}

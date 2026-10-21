import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'aria-hidden': 'true',
  },
  imports: [],
  selector: 'app-appointment-card-shimmer',
  styleUrl: './appointment-card-shimmer.component.scss',
  templateUrl: './appointment-card-shimmer.component.html',
})
export class AppointmentCardShimmerComponent {}

import { Component, input } from '@angular/core';

@Component({
  host: {
    'aria-hidden': 'true',
  },
  imports: [],
  selector: 'app-appointment-card-skeleton',
  styleUrl: './appointment-card-skeleton.component.scss',
  templateUrl: './appointment-card-skeleton.component.html',
})
export class AppointmentCardSkeletonComponent {
  count = input(3);

  readonly widths = [
    { patient: '46%', meta: '30%' },
    { patient: '34%', meta: '38%' },
    { patient: '52%', meta: '26%' },
    { patient: '40%', meta: '33%' },
  ];
}

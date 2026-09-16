import { Component, computed, input } from '@angular/core';

@Component({
  host: {
    'aria-hidden': 'true',
  },
  imports: [],
  selector: 'app-appointment-card-shimmer',
  styleUrl: './appointment-card-shimmer.component.scss',
  templateUrl: './appointment-card-shimmer.component.html',
})
export class AppointmentCardShimmerComponent {
  count = input(3);

  rows = computed(() => Array.from({ length: this.count() }));
}

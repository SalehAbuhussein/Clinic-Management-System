import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';

import { Person } from '@shared/services/person/person.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton],
  selector: 'app-person-card',
  styleUrl: './person-card.component.scss',
  templateUrl: './person-card.component.html',
})
export class PersonCardComponent {
  person = input.required<Person>();

  open = output<Person>();
  edit = output<Person>();
  remove = output<Person>();
}

import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';

import { Medicine } from '@shared/services/medicine/medicine.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton],
  selector: 'app-medicine-card',
  styleUrl: './medicine-card.component.scss',
  templateUrl: './medicine-card.component.html',
})
export class MedicineCardComponent {
  medicine = input.required<Medicine>();

  open = output<Medicine>();
  edit = output<Medicine>();
  remove = output<Medicine>();
}

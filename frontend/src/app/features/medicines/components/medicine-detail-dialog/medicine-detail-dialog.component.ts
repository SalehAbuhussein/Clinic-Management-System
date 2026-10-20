import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';

import { Medicine } from '@shared/services/medicine/medicine.types';

@Component({
  imports: [MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle],
  selector: 'app-medicine-detail-dialog',
  styleUrl: './medicine-detail-dialog.component.scss',
  templateUrl: './medicine-detail-dialog.component.html',
})
export class MedicineDetailDialogComponent {
  medicine = inject<Medicine>(MAT_DIALOG_DATA);
}

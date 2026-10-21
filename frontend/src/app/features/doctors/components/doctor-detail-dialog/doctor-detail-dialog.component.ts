import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { Doctor } from '@shared/services/doctor/doctor.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon],
  selector: 'app-doctor-detail-dialog',
  styleUrl: './doctor-detail-dialog.component.scss',
  templateUrl: './doctor-detail-dialog.component.html',
})
export class DoctorDetailDialogComponent {
  doctor = inject<Doctor>(MAT_DIALOG_DATA);
}

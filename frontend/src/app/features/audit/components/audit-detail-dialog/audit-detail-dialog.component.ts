import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';

import { AuditRow } from '@features/audit/types/audit.types';

@Component({
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle],
  selector: 'app-audit-detail-dialog',
  styleUrl: './audit-detail-dialog.component.scss',
  templateUrl: './audit-detail-dialog.component.html',
})
export class AuditDetailDialogComponent {
  entry = inject<AuditRow>(MAT_DIALOG_DATA);
}

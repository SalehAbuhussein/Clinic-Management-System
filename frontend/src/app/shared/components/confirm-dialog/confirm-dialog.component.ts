import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';

import { ConfirmDialogData } from './confirm-dialog.types';

/**
 * Asks before something that cannot be undone.
 *
 * Closes with `true` when confirmed and `undefined` otherwise, so a caller can
 * treat dismiss, Escape and the backdrop as one case.
 */
@Component({
  imports: [MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle],
  selector: 'app-confirm-dialog',
  styleUrl: './confirm-dialog.component.scss',
  templateUrl: './confirm-dialog.component.html',
})
export class ConfirmDialogComponent {
  data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}

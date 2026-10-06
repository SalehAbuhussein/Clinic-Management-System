import { Component } from '@angular/core';
import { MatAnchor } from '@angular/material/button';
import { MatDialogActions, MatDialogContent, MatDialogTitle, MatDialogClose } from '@angular/material/dialog';

@Component({
  imports: [
    MatAnchor,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogContent,
    MatDialogTitle,
    MatDialogTitle,
  ],
  selector: 'app-leave-detail-dialog',
  styleUrl: './leave-detail-dialog.component.scss',
  templateUrl: './leave-detail-dialog.component.html',
})
export class LeaveDetailDialogComponent {}

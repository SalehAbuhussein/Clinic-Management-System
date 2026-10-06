import { Component, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';

import { LeaveDetailDialogComponent } from './components/leave-detail-dialog/leave-detail-dialog.component';
import { LeavesListComponent } from './components/leaves-list/leaves-list.component';

@Component({
  imports: [
    LeavesListComponent,
  ],
  selector: 'app-leaves',
  styleUrl: './leaves.component.scss',
  templateUrl: './leaves.component.html',
})
export class LeavesComponent {
  dialog = inject(MatDialog);

  /**
   * Open Leave Details Dialog
   */
  openLeaveDetailsDialog = () => {
    this.dialog.open(LeaveDetailDialogComponent, { width: '560px' });
  }
}

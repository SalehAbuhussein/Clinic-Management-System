import { Component, input, output } from '@angular/core';

import { LeaveCardComponent } from '../leave-card/leave-card.component';

import { Leave } from '@shared/services/leave/leave.types';

@Component({
  imports: [LeaveCardComponent],
  selector: 'app-leaves-list',
  styleUrl: './leaves-list.component.scss',
  templateUrl: './leaves-list.component.html',
})
export class LeavesListComponent {
  items = input.required<Leave[]>();

  open = output<Leave>();
  approve = output<Leave>();
  reject = output<Leave>();
}

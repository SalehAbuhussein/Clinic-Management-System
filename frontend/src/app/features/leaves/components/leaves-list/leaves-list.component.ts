import { Component, output } from '@angular/core';

import { LeaveCardComponent } from '../leave-card/leave-card.component';

@Component({
  imports: [LeaveCardComponent],
  selector: 'app-leaves-list',
  styleUrl: './leaves-list.component.scss',
  templateUrl: './leaves-list.component.html',
})
export class LeavesListComponent {
  open = output<void>();
}

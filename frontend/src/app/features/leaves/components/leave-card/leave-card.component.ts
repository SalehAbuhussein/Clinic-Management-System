import { Component, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';

@Component({
  imports: [DatePipe, MatIcon],
  selector: 'app-leave-card',
  styleUrl: './leave-card.component.scss',
  templateUrl: './leave-card.component.html',
})
export class LeaveCardComponent {
  open = output<void>();
}

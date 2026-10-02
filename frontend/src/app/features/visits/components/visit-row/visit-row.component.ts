import { Component, computed, input, output, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';

import { Visit } from '@shared/services/visit/visit.types';

@Component({
  imports: [DatePipe, MatIcon, MatIconButton, MatMenu, MatMenuItem, MatMenuTrigger],
  selector: 'app-visit-row',
  styleUrl: './visit-row.component.scss',
  templateUrl: './visit-row.component.html',
})
export class VisitRowComponent {
  visit = input.required<Visit>();

  edit = output<Visit>();
  checkout = output<Visit>();
  remove = output<Visit>();

  menuTrigger = viewChild(MatMenuTrigger);

  /**
   * A visit is open until it is checked out. Checking out also locks the row,
   * and the API then refuses every edit and the delete, so this one flag
   * decides both the chip and whether there is a menu at all.
   */
  isOpen = computed(() => !this.visit().checkoutTime);

  /** Opens the menu from anywhere on the row, anchored to the icon button. */
  openMenu = () => this.menuTrigger()?.openMenu();
}

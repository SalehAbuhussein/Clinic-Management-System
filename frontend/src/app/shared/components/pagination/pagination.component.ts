import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIcon],
  selector: 'app-pagination',
  styleUrl: './pagination.component.scss',
  templateUrl: './pagination.component.html',
})
export class PaginationComponent {
  page = model.required<number>();
  total = input.required<number>();
  pageSize = input(10);

  totalPages = computed(() => Math.ceil(this.total() / this.pageSize()));

  hasPrevious = computed(() => this.page() > 1);
  hasNext = computed(() => this.page() < this.totalPages());

  /**
   * Go to previous page
   */
  goToPrevious = () => this.page.update((current) => Math.max(1, current - 1));

  /**
   * Go to next page
   */
  goToNext = () => this.page.update((current) => current + 1);
}

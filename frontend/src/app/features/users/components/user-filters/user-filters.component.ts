import { ChangeDetectionStrategy, Component, computed, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormField, MatLabel, MatPrefix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';

import { UserFilters } from '@shared/services/user/user.types';
import { UserFilterField } from '../../types/users.types';

/**
 * Filters the user list by one field at a time.
 *
 * Picking the field swaps the control beside it, so the screen never shows
 * three empty boxes for a search that only uses one.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatFormField, MatIcon, MatIconButton, MatInput, MatLabel, MatOption, MatPrefix, MatSelect],
  selector: 'app-user-filters',
  styleUrl: './user-filters.component.scss',
  templateUrl: './user-filters.component.html',
})
export class UserFiltersComponent {
  filters = model<UserFilters>({});

  field = signal<UserFilterField>('fullname');

  text = signal('');

  active = signal('all');

  label = computed(() => this.field() === 'email' ? 'Email' : 'Name');

  hasValue = computed(() => this.field() === 'active' ? this.active() !== 'all' : !!this.text());

  /**
   * Switch fields and drop whatever was already there, so a name left in the
   * box cannot filter an email search.
   *
   * @param field The field that was picked.
   */
  selectField(field: UserFilterField): void {
    this.field.set(field);
    this.text.set('');
    this.active.set('all');
    this.filters.set({});
  }

  /**
   * Search the chosen field for some text.
   *
   * @param value What was typed. The screen debounces it, so every keystroke
   * arriving here is fine.
   */
  setText(value: string): void {
    this.text.set(value);
    this.filters.set({ [this.field()]: value.trim() });
  }

  /**
   * Filter by status.
   *
   * @param value `true`, `false`, or `all` for no filter. Strings rather than
   * booleans, so Material has something to mark as chosen and a false survives
   * the trip to the query string.
   */
  setActive(value: string): void {
    this.active.set(value);
    this.filters.set(value === 'all' ? {} : { active: value });
  }

  /** Empty the filter, keeping the chosen field. */
  clear(): void {
    this.selectField(this.field());
  }
}

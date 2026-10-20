import { ChangeDetectionStrategy, Component, computed, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormField, MatLabel, MatPrefix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';

import { PersonFilterField } from '@features/people/types/people.types';
import { PersonFilters } from '@shared/services/person/person.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatFormField, MatIcon, MatIconButton, MatInput, MatLabel, MatOption, MatPrefix, MatSelect],
  selector: 'app-person-filters',
  styleUrl: './person-filters.component.scss',
  templateUrl: './person-filters.component.html',
})
export class PersonFiltersComponent {
  filters = model<PersonFilters>({});

  field = signal<PersonFilterField>('fullname');

  text = signal('');

  labels: Record<PersonFilterField, string> = {
    fullname: 'Name',
    nationalNo: 'National number',
    phone: 'Phone',
  };

  label = computed(() => this.labels[this.field()]);

  hasValue = computed(() => !!this.text());

  /**
   * Switch fields and drop whatever was already there, so a name left in the
   * box cannot filter a phone search.
   *
   * @param field The field that was picked.
   */
  selectField(field: PersonFilterField): void {
    this.field.set(field);
    this.text.set('');
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

  /** Empty the filter, keeping the chosen field. */
  clear(): void {
    this.selectField(this.field());
  }
}

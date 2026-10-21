import { Component, computed, inject, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatAutocomplete, MatAutocompleteTrigger, MatOption } from '@angular/material/autocomplete';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { MedicineService } from '@shared/services/medicine/medicine.service';

import { Medicine } from '@shared/services/medicine/medicine.types';

@Component({
  imports: [
    FormsModule,
    MatAutocomplete,
    MatAutocompleteTrigger,
    MatFormField,
    MatInput,
    MatLabel,
    MatOption,
    MatProgressSpinner,
  ],
  selector: 'app-medicine-selector-input',
  styleUrl: './medicine-selector-input.component.scss',
  templateUrl: './medicine-selector-input.component.html',
})
export class MedicineSelectorInputComponent {
  medicineService = inject(MedicineService);

  medicine = model<Medicine | null>(null);

  term = signal('');

  /**
   * The typed text, held back until typing pauses. Signals carry no notion of
   * time, so the delay is an RxJS operator and the value crosses back.
   */
  debouncedTerm = toSignal(
    toObservable(this.term).pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: '' },
  );

  searchTerm = computed(() => this.debouncedTerm().trim());

  medicinesResource = this.medicineService.getMedicineResource(() => ({ name: this.searchTerm() }));

  medicines = computed(() => this.medicinesResource.value().items);

  hasNoResults = computed(() => !this.medicinesResource.isLoading()
    && !this.medicinesResource.error()
    && this.medicines().length === 0);

  /**
   * What the input shows once a medicine is chosen. Without it the field would
   * render the bound object.
   */
  showName = (medicine: Medicine | null) => medicine ? `${medicine.genericname} ${medicine.strength}` : '';

  /**
   * Typing replaces the search term but never the selection: the two are
   * separate, so the field can hold a medicine while the text is being edited.
   */
  onInput = (value: string) => this.term.set(value);
}

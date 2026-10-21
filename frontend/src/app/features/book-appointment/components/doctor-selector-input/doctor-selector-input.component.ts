import { ChangeDetectionStrategy, Component, computed, inject, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatAutocomplete, MatAutocompleteTrigger, MatOption } from '@angular/material/autocomplete';
import { MatFormField, MatHint, MatLabel, MatPrefix, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { DoctorService } from '@shared/services/doctor/doctor.service';

import { Doctor } from '@shared/services/doctor/doctor.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatAutocomplete,
    MatAutocompleteTrigger,
    MatFormField,
    MatHint,
    MatIcon,
    MatIconButton,
    MatInput,
    MatLabel,
    MatOption,
    MatPrefix,
    MatProgressSpinner,
    MatSuffix,
  ],
  selector: 'app-doctor-selector-input',
  styleUrl: './doctor-selector-input.component.scss',
  templateUrl: './doctor-selector-input.component.html',
})
export class DoctorSelectorInputComponent {
  doctorService = inject(DoctorService);

  doctor = model<Doctor | null>(null);

  term = signal('');

  /**
   * The typed text, held back until typing pauses. Signals carry no notion of
   * time, so the delay is an RxJS operator and the value crosses back.
   */
  debouncedTerm = toSignal(
    toObservable(this.term).pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: '' },
  );

  /** An empty term lists the first page, which is the roster a clinic starts from. */
  searchTerm = computed(() => this.debouncedTerm().trim());

  doctorsResource = this.doctorService.getDoctorResource(() => ({ fullname: this.searchTerm() }));

  doctors = computed(() => (this.doctorsResource.hasValue() ? this.doctorsResource.value().items : []));

  hasNoResults = computed(() => !this.doctorsResource.isLoading() && this.doctors().length === 0);

  /**
   * What the input shows once a doctor is chosen. Without it the field would
   * render the bound object.
   */
  showName = (doctor: Doctor | null) => doctor?.fullname ?? '';

  /**
   * Typing replaces the search term but never the selection: the two are
   * separate, so the field can hold a doctor while the text is being edited.
   */
  onInput = (value: string) => this.term.set(value);

  /**
   * Clear both selected doctor and Search term
   */
  clear = () => {
    this.doctor.set(null);
    this.term.set('');
  };
}

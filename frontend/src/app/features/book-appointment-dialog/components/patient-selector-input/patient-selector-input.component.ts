import { Component, computed, inject, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatAutocomplete, MatAutocompleteTrigger, MatOption } from '@angular/material/autocomplete';
import { MatFormField, MatHint, MatLabel, MatPrefix, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { PatientService } from '@shared/services/patient/patient.service';

import { Patient } from '@shared/services/patient/patient.types';

@Component({
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
  selector: 'app-patient-selector-input',
  styleUrl: './patient-selector-input.component.scss',
  templateUrl: './patient-selector-input.component.html',
})
export class PatientSelectorInputComponent {
  patientService = inject(PatientService);

  patient = model<Patient | null>(null);
  term = signal('');

  /**
   * The typed text, held back until typing pauses. Signals carry no notion of
   * time, so the delay is an RxJS operator and the value crosses back.
   */
  debouncedTerm = toSignal(
    toObservable(this.term).pipe(debounceTime(300), distinctUntilChanged()), { initialValue: '' },
  );

  /** Below two characters the query is a full table scan for no useful result. */
  searchTerm = computed(() => {
    const term = this.debouncedTerm().trim();
    return term.length >= 2 ? term : '';
  });

  patientsResource = this.patientService.getPatientResource(() => ({ fullname: this.searchTerm() }));

  patients = computed(() => (this.patientsResource.hasValue() ? this.patientsResource.value().items : []));
  hasNoResults = computed(() => !!this.searchTerm() && !this.patientsResource.isLoading && this.patients().length === 0);

  /**
   * What the input shows once a patient is chosen. Without it the field would
   * render the bound object.
   */
  showName = (patient: Patient | null) => patient?.fullname ?? '';

  /**
   * Typing replaces the search term but never the selection: the two are
   * separate, so the field can hold a patient while the text is being edited.
   */
  onInput = (value: string) => this.term.set(value);

  clear = () => {
    this.patient.set(null);
    this.term.set('');
  };
}

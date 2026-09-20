import { Component, computed, inject, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormField, MatHint, MatLabel, MatPrefix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatOption, MatSelect } from '@angular/material/select';

import { DoctorService } from '@shared/services/doctor/doctor.service';

import { Doctor } from '@shared/services/doctor/doctor.types';

@Component({
  imports: [FormsModule, MatFormField, MatHint, MatIcon, MatLabel, MatOption, MatPrefix, MatSelect],
  selector: 'app-doctor-selector-input',
  styleUrl: './doctor-selector-input.component.scss',
  templateUrl: './doctor-selector-input.component.html',
})
export class DoctorSelectorInputComponent {
  doctorService = inject(DoctorService);

  doctor = model<Doctor | null>(null);

  doctors = computed(() => (this.doctorsResource.hasValue() ? this.doctorsResource.value().items : []));

  doctorsResource = this.doctorService.getDoctorResource();

  compareById = (left: Doctor | null, right: Doctor | null) => left?.id === right?.id;
}

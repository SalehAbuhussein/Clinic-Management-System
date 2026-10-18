import { Component, computed, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { PersonService } from '@shared/services/person/person.service';

import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { PersonFormDialogComponent } from './components/person-form-dialog/person-form-dialog.component';

import { ApiError } from '@core/auth/types/auth.types';
import { Person } from '@shared/services/person/person.types';

@Component({
  imports: [MatButton, MatIcon, MatProgressSpinner, PaginationComponent],
  selector: 'app-people',
  styleUrl: './people.component.scss',
  templateUrl: './people.component.html',
})
export class PeopleComponent {
  dialog = inject(MatDialog);
  personService = inject(PersonService);
  snackbar = inject(MatSnackBar);

  page = signal(1);

  peopleResource = this.personService.getPersonResource(() => ({ page: this.page() }));

  people = computed(() => this.peopleResource.value().items);
  total = computed(() => this.peopleResource.value().total);
  perPage = computed(() => this.peopleResource.value().perPage || 10);

  /**
   * Correct someone's details. The API refuses another admin's.
   *
   * @param person The row that was picked.
   */
  openForm(person: Person): void {
    this.dialog.open(PersonFormDialogComponent, { width: '640px', maxWidth: '100vw', data: person })
      .afterClosed()
      .subscribe((saved: boolean | undefined) => {
        if (!saved) {
          return;
        }

        this.peopleResource.reload();
      });
  }

  /**
   * Remove a person. The API refuses one a user or a patient belongs to, which
   * is almost all of them.
   *
   * @param person The row that was picked.
   */
  deletePerson = (person: Person) => {
    this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete this person?',
        message: `${person.firstname} ${person.lastname} is removed. Nothing here can recreate them.`,
        confirmLabel: 'Delete',
      },
    })
      .afterClosed()
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) {
          return;
        }

        this.personService.deletePerson(person.id).subscribe({
          next: () => {
            this.snackbar.open('Person deleted', 'Dismiss', { duration: 4000 });
            this.peopleResource.reload();
          },
          error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
        });
      });
  }
}

import { ActivatedRoute, Router } from '@angular/router';
import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { debounceTime, distinctUntilChanged } from 'rxjs';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { PersonService } from '@shared/services/person/person.service';

import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { PersonDetailDialogComponent } from './components/person-detail-dialog/person-detail-dialog.component';
import { PersonFiltersComponent } from './components/person-filters/person-filters.component';
import { PersonFormDialogComponent } from './components/person-form-dialog/person-form-dialog.component';

import { ApiError } from '@core/auth/types/auth.types';
import { Person, PersonFilters } from '@shared/services/person/person.types';
import { PersonDetailAction } from './types/people.types';

import { filtersFromUrl, pageFromUrl, syncUrl } from '@core/utils/url-state.utils';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PersonFiltersComponent, MatButton, MatIcon, MatProgressSpinner, PaginationComponent],
  selector: 'app-people',
  styleUrl: './people.component.scss',
  templateUrl: './people.component.html',
})
export class PeopleComponent {
  dialog = inject(MatDialog);
  personService = inject(PersonService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);

  filters = signal<PersonFilters>(filtersFromUrl(this.route));

  debouncedFilters = toSignal(
    toObservable(this.filters).pipe(
      debounceTime(300),
      distinctUntilChanged((one, other) => JSON.stringify(one) === JSON.stringify(other)),
    ),
    { initialValue: this.filters() },
  );

  page = linkedSignal<PersonFilters, number>({
    source: this.debouncedFilters,
    computation: (_, previous) => previous ? 1 : pageFromUrl(this.route),
  });

  peopleResource = this.personService.getPersonResource(() => ({ ...this.debouncedFilters(), page: this.page() }));

  constructor() {
    syncUrl(this.router, this.route, () => ({ ...this.filters(), page: this.page() }));
  }

  /**
   * Correct someone's details. The API refuses another admin's.
   *
   * @param person The row that was picked.
   */
  openDetail(person: Person): void {
    this.dialog.open(PersonDetailDialogComponent, { width: '440px', data: person })
      .afterClosed()
      .subscribe((action: PersonDetailAction | undefined) => {
        if (action === 'edit') {
          this.openForm(person);
        }
      });
  }

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

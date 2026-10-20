import { ActivatedRoute, Router } from '@angular/router';

import { debounceTime, distinctUntilChanged } from 'rxjs';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { Observable } from 'rxjs';

import { UserService } from '@shared/services/user/user.service';

import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { UserDetailDialogComponent } from './components/user-detail-dialog/user-detail-dialog.component';
import { UserFormDialogComponent } from './components/user-form-dialog/user-form-dialog.component';
import { UserFiltersComponent } from './components/user-filters/user-filters.component';
import { UserRowComponent } from './components/user-row/user-row.component';

import { ApiError } from '@core/auth/types/auth.types';
import { ConfirmDialogData } from '@shared/components/confirm-dialog/confirm-dialog.types';
import { User, UserFilters } from '@shared/services/user/user.types';
import { UserDetailAction } from './types/users.types';

@Component({
  imports: [MatButton, MatIcon, MatProgressSpinner, PaginationComponent, UserFiltersComponent, UserRowComponent],
  selector: 'app-users',
  styleUrl: './users.component.scss',
  templateUrl: './users.component.html',
})
export class UsersComponent {
  dialog = inject(MatDialog);
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);
  userService = inject(UserService);

  filters = signal<UserFilters>({});

  /**
   * The filters, held back until typing pauses. Signals carry no notion of
   * time, so the delay is an RxJS operator and the value crosses back.
   *
   * Signals compare by reference, so every keystroke and every change of the
   * field dropdown hands over a fresh object. Comparing the contents stops a
   * dropdown change on an empty box from refetching a list that would come
   * back identical.
   */
  debouncedFilters = toSignal(
    toObservable(this.filters).pipe(
      debounceTime(300),
      distinctUntilChanged((one, other) => JSON.stringify(one) === JSON.stringify(other)),
    ),
    { initialValue: this.filters() },
  );

  // Resets to the first page on a new search, but takes the URL's page on the
  // first run so a reload or a shared link lands where it left off.
  page = linkedSignal<UserFilters, number>({
    source: this.debouncedFilters,
    computation: (_, previous) => previous ? 1 : this.pageFromUrl(),
  });

  usersResource = this.userService.getUserResource(() => ({ ...this.debouncedFilters(), page: this.page() }));

  users = computed(() => this.usersResource.value().items);
  total = computed(() => this.usersResource.value().total);
  perPage = computed(() => this.usersResource.value().perPage || 10);

  constructor() {
    effect(() => {
      this.router.navigate([], {
        queryParams: { page: this.page() },
        relativeTo: this.route,
        replaceUrl: true,
      });
    });
  }

  /**
   * @returns The page from the URL, or the first one.
   */
  pageFromUrl(): number {
    const page = Number(this.route.snapshot.queryParamMap.get('page'));

    return Number.isInteger(page) && page > 0 ? page : 1;
  }

  /**
   * Show everything on a user, and act on whatever the dialog closes with.
   *
   * @param user The row that was picked.
   */
  openUser = (user: User) => {
    this.dialog.open(UserDetailDialogComponent, { width: '440px', data: user })
      .afterClosed()
      .subscribe((action: UserDetailAction | undefined) => {
        switch (action) {
          case 'edit':
            this.openFormDialog(user);
            break;
          case 'activate':
            this.activateUser(user);
            break;
          case 'deactivate':
            this.deactivateUser(user);
            break;
        }
      });
  }

  openNewUserDialog = () => this.openFormDialog(null);

  /**
   * Opens the form and refreshes the list when it saved.
   *
   * @param user The user to edit, or null to create one.
   */
  openFormDialog = (user: User | null) => {
    this.dialog.open(UserFormDialogComponent, { width: '560px', data: user })
      .afterClosed()
      .subscribe((saved: boolean | undefined) => {
        if (!saved) {
          return;
        }

        this.usersResource.reload();
      });
  }

  /**
   * Switch an account off. This is the delete: a user owns audit history, so
   * the API has no DELETE at all.
   *
   * @param user The row that was picked.
   */
  deactivateUser = (user: User) => this.confirmThen(
    {
      title: `Deactivate ${user.fullname}?`,
      message: 'They keep their history but cannot sign in until an admin turns the account back on.',
      confirmLabel: 'Deactivate',
      dismissLabel: 'Keep active',
      destructive: true,
    },
    this.userService.deactivateUser(user.id),
    `${user.fullname} deactivated`,
  );

  /**
   * Switch an account back on.
   *
   * @param user The row that was picked.
   */
  activateUser = (user: User) => this.confirmThen(
    {
      title: `Activate ${user.fullname}?`,
      message: 'They will be able to sign in again straight away.',
      confirmLabel: 'Activate',
    },
    this.userService.activateUser(user.id),
    `${user.fullname} activated`,
  );

  /**
   * Asks before a change, then runs it and refreshes the list.
   *
   * @param data What the dialog should say.
   * @param request The call to make once confirmed.
   * @param message What to say when it succeeds.
   */
  confirmThen(data: ConfirmDialogData, request: Observable<unknown>, message: string): void {
    this.dialog.open(ConfirmDialogComponent, { width: '400px', data })
      .afterClosed()
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) {
          return;
        }

        request.subscribe({
          next: () => {
            this.snackbar.open(message, 'Dismiss', { duration: 4000 });
            this.usersResource.reload();
          },
          error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
        });
      });
  }
}

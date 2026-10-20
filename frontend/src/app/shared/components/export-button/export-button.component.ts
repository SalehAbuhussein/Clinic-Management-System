import { booleanAttribute, ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { HttpResponse } from '@angular/common/http';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Params } from '@angular/router';

import { ApiService } from '@core/services/api/api.service';

import { ExportRangeDialogComponent } from '@shared/components/export-range-dialog/export-range-dialog.component';

import { ApiError } from '@core/auth/types/auth.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIcon, MatProgressSpinner],
  selector: 'app-export-button',
  styleUrl: './export-button.component.scss',
  templateUrl: './export-button.component.html',
})
export class ExportButtonComponent {
  apiService = inject(ApiService);
  dialog = inject(MatDialog);
  snackbar = inject(MatSnackBar);

  path = input.required<string>();
  filters = input<Params>({});
  label = input('Export');
  rangeLabel = input('Range');

  /**
   * Whether to ask for a date range first.
   *
   * On by default for screens that show one day at a time, because a day is a
   * viewing choice and rarely the range anyone wants out. Off for screens whose
   * filter is already the export anyone would ask for, such as a patient search.
   */
  askForRange = input(false, { transform: booleanAttribute });

  isExporting = signal(false);

  /**
   * Fetch the file and hand it to the browser.
   *
   * The button disables itself while a request is in flight, so a second click
   * cannot start a second download of the same thing.
   */
  exportFile(): void {
    if (!this.askForRange()) {
      this._download(this.filters());
      return;
    }

    this.dialog.open(ExportRangeDialogComponent, { width: '380px', data: { filters: this.filters(), label: this.rangeLabel() } })
      .afterClosed()
      .subscribe((range: Params | undefined) => {
        if (!range) {
          return;
        }

        this._download({ ...this.filters(), ...range });
      });
  }

  /**
   * Fetch the file for one set of query parameters.
   *
   * @param params What to send, already resolved.
   */
  _download(params: Params): void {
    this.isExporting.set(true);

    this.apiService.download(this.path(), params).subscribe({
      next: (response) => {
        this.isExporting.set(false);

        if (response.body) {
          this._save(response.body, this._fileNameFrom(response));
        }
      },
      error: (error: ApiError) => {
        this.isExporting.set(false);
        this.snackbar.open(error.message, 'Dismiss', { duration: 8000 });
      },
    });
  }

  /**
   * The name the server chose, so the saved file matches what the API called it.
   *
   * ASP.NET writes a plain `filename` and, for non-ASCII, a `filename*` as well.
   * Either shape is accepted; a missing header falls back to a sensible name.
   */
  _fileNameFrom(response: HttpResponse<Blob>): string {
    const disposition = response.headers.get('Content-Disposition') ?? '';
    const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);

    return match ? decodeURIComponent(match[1]) : 'export.xlsx';
  }

  /**
   * Hand a blob to the browser as a download.
   *
   * The object URL is revoked straight after the click. Without that, every
   * export holds its file in memory for as long as the tab is open.
   */
  _save(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');

    anchor.href = url;
    anchor.download = fileName;
    anchor.click();

    URL.revokeObjectURL(url);
  }
}

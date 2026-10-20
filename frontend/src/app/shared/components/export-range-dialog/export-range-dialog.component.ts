import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { Params } from '@angular/router';

import { dayRange } from '@core/utils/day.utils';

import { ExportRangeDialogData } from './export-range-dialog.types';

/**
 * Picks the range an export covers.
 *
 * It opens on whatever the screen is currently showing, so accepting the
 * default gives the same file the button alone used to. The point of the dialog
 * is widening: a screen shows one day because that is how the desk works, while
 * an export is usually a month.
 *
 * Closes with the query parameters to send, or `undefined` when dismissed.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatButton,
    MatDatepickerModule,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    MatFormField,
    MatHint,
    MatLabel,
  ],
  selector: 'app-export-range-dialog',
  styleUrl: './export-range-dialog.component.scss',
  templateUrl: './export-range-dialog.component.html',
})
export class ExportRangeDialogComponent {
  dialogRef = inject<MatDialogRef<ExportRangeDialogComponent>>(MatDialogRef);
  data = inject<ExportRangeDialogData>(MAT_DIALOG_DATA);

  screenFilters = this.data.filters;

  maxDate = new Date();

  from = signal(this._seedFrom());
  to = signal(this._seedTo());

  /**
   * Hand back the range as the API wants it: an inclusive first day and an
   * exclusive end, which is what dayRange already produces for one day.
   *
   * An empty box means no bound on that side, so clearing both exports
   * everything the caller is allowed to see.
   */
  confirm(): void {
    const from = this.from();
    const to = this.to();

    this.dialogRef.close({
      from: from ? dayRange(from).from : undefined,
      to: to ? dayRange(to).to : undefined,
    });
  }

  /**
   * @returns The screen's start date, or null when it filters by something else.
   */
  _seedFrom(): Date | null {
    const value = this.screenFilters['from'];

    return value ? new Date(value) : null;
  }

  /**
   * The screen sends an exclusive end, one millisecond past midnight of the day
   * after. Step back so the box shows the last day the export actually covers,
   * rather than tomorrow.
   *
   * @returns The screen's last day, or null when it has no upper bound.
   */
  _seedTo(): Date | null {
    const value = this.screenFilters['to'];

    return value ? new Date(new Date(value).getTime() - 1) : null;
  }
}

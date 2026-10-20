import { ChangeDetectionStrategy, Component, linkedSignal, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormField, MatLabel } from '@angular/material/form-field';

import { RangePreset } from './date-range-picker.types';

/**
 * Picks the days a screen covers, either from a preset or by hand.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatButtonToggle, MatButtonToggleGroup, MatDatepickerModule, MatFormField, MatLabel],
  selector: 'app-date-range-picker',
  styleUrl: './date-range-picker.component.scss',
  templateUrl: './date-range-picker.component.html',
})
export class DateRangePickerComponent {
  from = model.required<Date>();
  to = model.required<Date>();

  preset = linkedSignal<RangePreset>(() => this.detectPreset());

  /**
   * Move both ends to the chosen window. Custom only opens the date field.
   *
   * @param preset The button that was pressed.
   */
  selectPreset(preset: RangePreset): void {
    this.preset.set(preset);

    if (preset === 'custom') {
      return;
    }

    this.from.set(this.windowStart(Number(preset)));
    this.to.set(new Date());
  }

  /**
   * Set the first day.
   *
   * @param value The picked day. The range input clears the end between the two
   * clicks that set it, so a null is a half-finished pick and gets ignored.
   */
  setFrom(value: Date | null): void {
    if (value) {
      this.from.set(value);
    }
  }

  /**
   * Set the last day.
   *
   * @param value The picked day, or null mid-pick.
   */
  setTo(value: Date | null): void {
    if (value) {
      this.to.set(value);
    }
  }

  /**
   * Work out which preset the current dates match.
   *
   * @returns That preset, or `custom` when the range is anything else.
   */
  detectPreset(): RangePreset {
    if (!this.isSameDay(this.to(), new Date())) {
      return 'custom';
    }

    const spans: RangePreset[] = ['1', '7', '30'];

    return spans.find((span) => this.isSameDay(this.from(), this.windowStart(Number(span)))) ?? 'custom';
  }

  /**
   * The first day of a window that ends today.
   *
   * Built from date parts rather than by subtracting hours, so a
   * daylight-saving change cannot shift it.
   *
   * @param days How many days the window covers, counting today.
   * @returns That first day.
   */
  windowStart(days: number): Date {
    const today = new Date();

    return new Date(today.getFullYear(), today.getMonth(), today.getDate() - (days - 1));
  }

  isSameDay = (one: Date, other: Date) => one.toDateString() === other.toDateString();
}

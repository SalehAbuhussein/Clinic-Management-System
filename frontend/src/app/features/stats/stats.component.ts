import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { formatDate } from '@angular/common';

import { StatsService } from '@shared/services/stats/stats.service';

/** The ranges worth one click. `custom` reveals the date range field instead. */
export type StatsPreset = '7' | '14' | '30' | 'custom';

@Component({
  imports: [
    FormsModule,
    MatButtonToggle,
    MatButtonToggleGroup,
    MatDatepickerModule,
    MatFormField,
    MatHint,
    MatIcon,
    MatLabel,
    MatProgressSpinner,
  ],
  selector: 'app-stats',
  styleUrl: './stats.component.scss',
  templateUrl: './stats.component.html',
})
export class StatsComponent {
  statsService = inject(StatsService);

  /** The span StatsController clamps to. The picker greys out the rest. */
  readonly MAX_DAYS = 92;

  /** Nothing to count in the future, and nothing past the API's cap. */
  maxDate = new Date();
  minDate = this.daysAgo(this.MAX_DAYS - 1);

  preset = signal<StatsPreset>('14');

  /** The last day shown, inclusive. Both presets and the picker write these. */
  from = signal(this.daysAgo(13));
  to = signal(new Date());

  statsResource = this.statsService.getStatsResource(this.from, this.to);

  stats = computed(() => this.statsResource.value());

  /** What the caption says, so the chart always states its own range. */
  rangeLabel = computed(() => `${formatDate(this.from(), 'd MMM', 'en-US')} to ${formatDate(this.to(), 'd MMM', 'en-US')}`);

  /**
   * The five tiles. One list rather than five blocks of markup, because they
   * differ only in their wording, number, icon and colour role.
   *
   * The roles are Material's own container pairs, so every tile's text is
   * guaranteed to contrast with its own background in both themes.
   *
   * These are always about today, whatever range the chart shows.
   */
  tiles = computed(() => {
    const stats = this.stats();

    if (!stats) {
      return [];
    }

    return [
      { label: 'In clinic now', value: stats.inClinic, icon: 'hourglass_top', role: 'tertiary' },
      { label: 'Appointments today', value: stats.appointmentsToday, icon: 'today', role: 'primary' },
      { label: 'Attended today', value: stats.attendedToday, icon: 'check_circle', role: 'attended' },
      { label: 'No-shows today', value: stats.noShowToday, icon: 'person_off', role: 'error' },
      { label: 'Patients on file', value: stats.patients, icon: 'groups', role: 'secondary' },
    ];
  });

  /**
   * One bar per day. Heights are a percentage of the busiest day, so the chart
   * fills its box whatever the clinic's volume is.
   *
   * `max` floors at 1 so a range of zeroes divides cleanly, and any day with
   * appointments gets at least 4% so a single one is still visible.
   */
  chart = computed(() => {
    const series = this.stats()?.appointmentsByDay ?? [];
    const max = Math.max(1, ...series);
    const start = this.from();

    return series.map((total, index) => {
      const day = new Date(start.getFullYear(), start.getMonth(), start.getDate() + index);

      return {
        index,
        total,
        height: total ? Math.max(4, (total / max) * 100) : 0,
        isBusiest: total === max && total > 0,
        tick: formatDate(day, 'd', 'en-US'),
        // Shown on hover and read out by a screen reader, so the numbers are
        // never carried by bar height alone.
        label: `${formatDate(day, 'EEE d MMM', 'en-US')}: ${total}`,
      };
    });
  });

  /** A range of zeroes draws an empty box, so it says so instead. */
  hasAppointments = computed(() => this.chart().some((day) => day.total > 0));

  /** Ticks crowd once a range runs long, so only every nth one is drawn. */
  tickEvery = computed(() => Math.ceil(this.chart().length / 14));

  /**
   * Moves the range to one of the presets. `custom` changes nothing and leaves
   * whatever is already chosen in the picker.
   *
   * @param preset The button that was pressed.
   */
  selectPreset = (preset: StatsPreset) => {
    this.preset.set(preset);

    if (preset === 'custom') {
      return;
    }

    this.from.set(this.daysAgo(Number(preset) - 1));
    this.to.set(new Date());
  }

  /**
   * Takes the first click of a range.
   *
   * Material clears the end date while a new range is being picked, so the
   * range is briefly half-chosen. Dragging the end along with a later start
   * keeps it valid in the meantime, which is what the chart reads.
   *
   * @param date The chosen start, or null while the picker is between clicks.
   */
  setFrom = (date: Date | null) => {
    if (!date) {
      return;
    }

    this.from.set(date);

    if (date > this.to()) {
      this.to.set(date);
    }
  }

  /**
   * Takes the second click of a range. A null is the picker clearing the end
   * before the user gets there, and the old one stays until they do.
   *
   * @param date The chosen end, or null.
   */
  setTo = (date: Date | null) => {
    if (date) {
      this.to.set(date);
    }
  }

  /**
   * @param days How far back to go.
   * @returns That day at local midnight.
   */
  daysAgo(days: number): Date {
    const today = new Date();

    return new Date(today.getFullYear(), today.getMonth(), today.getDate() - days);
  }
}

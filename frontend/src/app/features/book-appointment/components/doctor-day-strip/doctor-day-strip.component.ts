import { Component, computed, effect, ElementRef, inject, input, model, untracked, viewChild } from '@angular/core';

import { AppointmentService } from '@shared/services/appointment/appointment.service';
import { WorkingDaysService } from '@shared/services/working-days/working-days.service';

/** Start times snap to a quarter hour, matching the time input's step. */
const STEP_MINUTES = 15;

/** How far apart the labels under the track sit. */
const TICK_STEP_MINUTES = 2 * 60;

/** One booked appointment, reduced to the two numbers the strip draws. */
type BusySlot = { startMinutes: number; endMinutes: number };

/**
 * Draws a doctor's day as a horizontal bar, so a clash shows up while the
 * receptionist is choosing instead of arriving as a 409 after they submit.
 *
 * Almost everything here is derived. The dialog owns the doctor, the day, the
 * duration and the start time; the strip reads those, writes the start time
 * back, and keeps only the drag offset of its own.
 */
@Component({
  selector: 'app-doctor-day-strip',
  styleUrl: './doctor-day-strip.component.scss',
  templateUrl: './doctor-day-strip.component.html',
})
export class DoctorDayStripComponent {
  appointmentService = inject(AppointmentService);
  workingDaysService = inject(WorkingDaysService);

  /** The chosen doctor. Null dims the strip and sends no request. */
  doctorId = input<number | null>(null);

  /** The day being booked. */
  date = input<Date | null>(null);

  /** How long the appointment runs, set by the duration chips. */
  durationMinutes = input(30);

  /**
   * The appointment being rescheduled, which must not count as busy against
   * itself. Null when booking something new.
   */
  excludeAppointmentId = input<number | null>(null);

  /** The start time as `HH:mm`, shared with the Time field so the two agree. */
  time = model.required<string>();

  /**
   * Where the proposed appointment starts. Falls back to the clinic's opening
   * time while the Time field holds an incomplete value, so the block stays on
   * the track instead of disappearing mid-keystroke.
   */
  startMinutes = computed(() => {
    const minutes = this.minutesFromTimeText(this.time());

    return Number.isNaN(minutes) ? this.dayStartMinutes() : minutes;
  });

  /** Where the proposed appointment ends: its start plus the chosen duration. */
  endMinutes = computed(() => this.startMinutes() + this.durationMinutes());

  /** The start time, for the slider's `aria-valuetext`. */
  startLabel = computed(() => this.toTimeText(this.startMinutes()));

  /** The proposed range, shown above the track. */
  rangeLabel = computed(() => `${this.startLabel()} – ${this.toTimeText(this.endMinutes())}`);

  /**
   * The doctor's booked slots for the day. Stays idle until a doctor is
   * chosen, because the id sits in the path and there is no URL without it.
   */
  schedulesResource = this.appointmentService.getDoctorSchedulesResource(
    () => this.doctorId(),
    () => this.date(),
  );

  /** The clinic's row for the weekday being booked, once the list has loaded. */
  workingDay = computed(() => {
    const date = this.date();
    return date ? this.workingDaysService.findByDate(date) : null;
  });

  // The window the track spans. The literals are a fallback for the moment
  // before the working days arrive, not the clinic's real hours.
  dayStartMinutes = computed(() => this.minutesFromTimeText(this.workingDay()?.startTime ?? '08:00'));
  dayEndMinutes = computed(() => this.minutesFromTimeText(this.workingDay()?.endTime ?? '16:00'));
  daySpanMinutes = computed(() => this.dayEndMinutes() - this.dayStartMinutes());

  /**
   * The labels under the track, every two hours from the clinic's opening time
   * to its closing time. The first one is rounded up to a whole hour, so a day
   * starting at 08:30 is labelled from 09:00 rather than on the half hour.
   */
  ticks = computed(() => {
    const labels: string[] = [];
    const firstHour = Math.ceil(this.dayStartMinutes() / 60) * 60;

    for (let minutes = firstHour; minutes <= this.dayEndMinutes(); minutes += TICK_STEP_MINUTES) {
      labels.push(this.toTimeText(minutes));
    }

    return labels;
  });

  /** True until a doctor is chosen. Dims the track and hides the block. */
  isDisabled = computed(() => !this.doctorId());

  /**
   * The doctor's booked slots in minutes, without the one being rescheduled:
   * an appointment must not clash with itself.
   *
   * Sorted by start, because `firstFreeStartMinutes` walks them in order and
   * gives a wrong answer if they arrive unsorted.
   */
  busySlots = computed<BusySlot[]>(() => {
    if (!this.schedulesResource.hasValue()) {
      return [];
    }

    return this.schedulesResource
      .value()
      .filter((schedule) => schedule.appointmentID !== this.excludeAppointmentId())
      .map((schedule) => ({
        startMinutes: this.minutesFromDate(new Date(schedule.startAt)),
        endMinutes: this.minutesFromDate(new Date(schedule.endAt)),
      }))
      .sort((left, right) => left.startMinutes - right.startMinutes);
  });

  /** True when the proposed appointment overlaps one the doctor already has. */
  clash = computed(() => this.busySlots().some((slot) => this.overlaps(slot)));

  /** The track element, measured to turn a pointer position into a time. */
  track = viewChild.required<ElementRef<HTMLElement>>('track');

  /** How far into the block the current drag started, in minutes. */
  grabOffsetMinutes = 0;

  constructor() {
    // A new doctor or a new day means a new set of gaps, so the suggestion moves
    // to the first that fits. The duration is read untracked: changing a chip
    // should resize the block, not send it back to the top of the day.
    //
    // Rescheduling is exempt. The caller has already set the time to where the
    // appointment is now, and suggesting a slot would throw that away the
    // moment the schedules arrived.
    effect(() => {
      this.busySlots();

      if (!this.doctorId() || this.excludeAppointmentId()) {
        return;
      }

      this.time.set(this.toTimeText(untracked(() => this.firstFreeStartMinutes())));
    });
  }

  /**
   * Starts a drag and records where inside the block the pointer landed, so
   * the block moves with the pointer instead of jumping its left edge under
   * the cursor.
   *
   * @param event The pointerdown on the block.
   */
  onPointerDown = (event: PointerEvent): void => {
    if (!this.doctorId()) {
      return;
    }

    const target = event.target as HTMLElement;

    target.setPointerCapture(event.pointerId);
    this.grabOffsetMinutes = this.minutesAtPointer(event) - this.startMinutes();
  }

  /**
   * Moves the block while a drag is running. Pointer capture sends moves here
   * even when the cursor leaves the block, so a fast gesture still tracks.
   *
   * @param event The pointermove on the block.
   */
  onPointerMove = (event: PointerEvent): void => {
    if (!this.doctorId()) {
      return;
    }

    const target = event.target as HTMLElement;

    if (!target.hasPointerCapture(event.pointerId)) {
      return;
    }

    this.moveTo(this.minutesAtPointer(event) - this.grabOffsetMinutes);
  }

  /**
   * Jumps the block to where the track was clicked, centred on the pointer.
   *
   * @param event The click on the track.
   */
  onTrackClick = (event: PointerEvent): void => {
    if (!this.doctorId()) {
      return;
    }
    this.moveTo(this.minutesAtPointer(event) - this.durationMinutes() / 2);
  }

  /**
   * Keeps the whole appointment inside the drawn day.
   *
   * @param minutes A start time in minutes.
   * @returns That time, or the nearest one the appointment fits into.
   */
  clampToDay = (minutes: number): number => {
    const latestStart = this.dayEndMinutes() - this.durationMinutes();
    return Math.max(this.dayStartMinutes(), Math.min(latestStart, minutes));
  }

  /**
   * Finds the earliest gap the chosen duration fits into. Each booked slot
   * pushes the candidate to the next quarter hour after that slot ends.
   *
   * @returns That gap's start, or the start of the day when nothing fits.
   */
  firstFreeStartMinutes = (): number => {
    let candidate = this.dayStartMinutes();

    for (const slot of this.busySlots()) {
      if (candidate + this.durationMinutes() <= slot.startMinutes) {
        return candidate;
      }

      candidate = Math.max(candidate, Math.ceil(slot.endMinutes / STEP_MINUTES) * STEP_MINUTES);
    }

    return candidate + this.durationMinutes() <= this.dayEndMinutes() ? candidate : this.dayStartMinutes();
  }

  /**
   * Reads the Time field's value. A time input reports an empty string while
   * it is being edited or after it is cleared, so the result can be NaN and
   * every caller has to expect it.
   *
   * @param time A time as `HH:mm`, possibly incomplete.
   * @returns Minutes from midnight, or NaN when the text is not a full time.
   */
  minutesFromTimeText = (time: string): number => {
    const [hours, minutes] = time.split(':').map(Number);

    return Number.isFinite(hours) && Number.isFinite(minutes) ? hours * 60 + minutes : NaN;
  }

  /**
   * Reads the time under the pointer by measuring the track.
   *
   * @param event A pointer event on the track or the block.
   * @returns The time at that position, in minutes, unsnapped.
   */
  minutesAtPointer = (event: PointerEvent): number => {
    const track = this.track().nativeElement.getBoundingClientRect();
    const ratio = (event.clientX - track.left) / track.width;

    return this.dayStartMinutes() + ratio * this.daySpanMinutes();
  }

  /**
   * Tests the proposed appointment against one booked slot. The API runs this
   * same comparison before it accepts a booking.
   *
   * @param slot A slot the doctor already has.
   * @returns True when the two overlap.
   */
  overlaps = (slot: BusySlot): boolean => this.startMinutes() < slot.endMinutes && this.endMinutes() > slot.startMinutes;

  /**
   * Moves the block by whole steps, which is what the arrow keys do.
   *
   * @param steps How many quarter hours to move. Negative moves earlier.
   */
  shift = (steps: number): void => this.moveTo(this.startMinutes() + steps * STEP_MINUTES);

  /**
   * Writes a new start time. Dragging, clicking and the arrow keys all come
   * through here, so the snap and the clamp are applied in one place.
   *
   * @param minutes The wanted start, before snapping and clamping.
   */
  moveTo = (minutes: number): void => this.time.set(this.toTimeText(this.clampToDay(this.snapToStep(minutes))));

  /**
   * @param minutes A time in minutes.
   * @returns The same time rounded to the nearest quarter hour.
   */
  snapToStep = (minutes: number) => Math.round(minutes / STEP_MINUTES) * STEP_MINUTES;

  /**
   * Reads the clock time out of an appointment's timestamp, in local time.
   *
   * @param date A parsed timestamp from the API.
   * @returns Minutes from midnight.
   */
  minutesFromDate = (date: Date) => date.getHours() * 60 + date.getMinutes();

  /**
   * @param totalMinutes Minutes from midnight.
   * @returns The time as `HH:mm`, zero padded.
   */
  toTimeText = (totalMinutes: number) => `${String(Math.floor(totalMinutes / 60)).padStart(2, '0')}:${String(totalMinutes % 60).padStart(2, '0')}`;

  /**
   * Places a time across the track.
   *
   * @param minutes A time as minutes from midnight.
   * @returns The CSS `left` value, as a percentage of the track.
   */
  leftPercent = (minutes: number): number => ((minutes - this.dayStartMinutes()) / this.daySpanMinutes()) * 100;

  /**
   * Sizes a length of time across the track.
   *
   * @param minutes A duration in minutes.
   * @returns The CSS `width` value, as a percentage of the track.
   */
  widthPercent = (minutes: number): number => (minutes / this.daySpanMinutes()) * 100;
}

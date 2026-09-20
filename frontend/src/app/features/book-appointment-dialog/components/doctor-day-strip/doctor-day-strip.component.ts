import { ChangeDetectionStrategy, Component, computed, ElementRef, input, model, viewChild } from '@angular/core';

const DAY_START = 8 * 60;
const DAY_END = 16 * 60;
const DAY_SPAN = DAY_END - DAY_START;
const STEP = 15;

type Slot = { start: number; end: number };

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-doctor-day-strip',
  styleUrl: './doctor-day-strip.component.scss',
  templateUrl: './doctor-day-strip.component.html',
})
export class DoctorDayStripComponent {
  /** Placeholder until the doctor's real day is wired up. */
  readonly busy: Slot[] = [
    { start: 8 * 60, end: 9 * 60 },
    { start: 10 * 60, end: 11 * 60 },
    { start: 13 * 60, end: 15 * 60 },
  ];

  /** Minutes from midnight. A model so the dialog can bind the chosen time later. */
  start = model(9 * 60 + 30);
  minutes = input(30);

  private track = viewChild.required<ElementRef<HTMLElement>>('track');

  private grabOffset = 0;

  end = computed(() => this.start() + this.minutes());

  clash = computed(() => this.busy.some((slot) => this.start() < slot.end && this.end() > slot.start));

  rangeLabel = computed(() => `${label(this.start())} – ${label(this.end())}`);

  left = (minutes: number) => ((minutes - DAY_START) / DAY_SPAN) * 100;

  width = (minutes: number) => (minutes / DAY_SPAN) * 100;

  startLabel = computed(() => label(this.start()));

  /**
   * Remembers where inside the block the drag began, so the block moves with
   * the pointer instead of jumping its left edge under the cursor.
   */
  onPointerDown(event: PointerEvent): void {
    const target = event.target as HTMLElement;

    target.setPointerCapture(event.pointerId);
    this.grabOffset = this.atPointer(event) - this.start();
  }

  onPointerMove(event: PointerEvent): void {
    const target = event.target as HTMLElement;

    if (!target.hasPointerCapture(event.pointerId)) {
      return;
    }

    this.moveTo(this.atPointer(event) - this.grabOffset);
  }

  /** Clicking the track jumps the block there, centred on the pointer. */
  onTrackClick(event: PointerEvent): void {
    this.moveTo(this.atPointer(event) - this.minutes() / 2);
  }

  shift(steps: number): void {
    this.moveTo(this.start() + steps * STEP);
  }

  private moveTo(minutes: number): void {
    const snapped = Math.round(minutes / STEP) * STEP;

    this.start.set(Math.max(DAY_START, Math.min(DAY_END - this.minutes(), snapped)));
  }

  private atPointer(event: PointerEvent): number {
    const rect = this.track().nativeElement.getBoundingClientRect();
    const ratio = (event.clientX - rect.left) / rect.width;

    return DAY_START + ratio * DAY_SPAN;
  }
}

const label = (total: number) =>
  `${String(Math.floor(total / 60)).padStart(2, '0')}:${String(total % 60).padStart(2, '0')}`;

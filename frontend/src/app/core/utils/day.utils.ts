import { formatDate } from '@angular/common';

/**
 * The half-open instant range covering one local day.
 *
 * The API compares these against stored instants, so only the browser can say
 * what midnight means here. A bare date would widen to UTC midnight in SQL
 * Server and cut the day at the wrong hour.
 *
 * Both ends are built from local date parts rather than by adding 24 hours, so
 * a daylight-saving day stays the right length.
 *
 * @param day Any moment on the wanted day; the time of day is ignored.
 * @returns `from` at that day's midnight and `to` at the next day's, both with
 * this browser's offset.
 */
export const dayRange = (day: Date) => {
  const format = "yyyy-MM-dd'T'HH:mm:ssZZZZZ";

  const from = new Date(day.getFullYear(), day.getMonth(), day.getDate());
  const to = new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1);

  return {
    from: formatDate(from, format, 'en-US'),
    to: formatDate(to, format, 'en-US'),
  };
};

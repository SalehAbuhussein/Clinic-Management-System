/**
 * The half-open instant range covering one local day, as UTC.
 *
 * The API compares these against stored instants, so only the browser can say
 * what midnight means here. A bare date would widen to UTC midnight in SQL
 * Server and cut the day at the wrong hour.
 *
 * Both ends are built from local date parts rather than by adding 24 hours, so
 * a daylight-saving day stays the right length.
 *
 * @param day Any moment on the wanted day; the time of day is ignored.
 * @returns This day's midnight and the next one's, as UTC instants.
 */
export const dayRange = (day: Date) => {
  const from = new Date(day.getFullYear(), day.getMonth(), day.getDate());
  const to = new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1);

  return {
    from: from.toISOString(),
    to: to.toISOString(),
  };
};

/**
 * The same half-open range, spanning several days.
 *
 * @param from The first day to include.
 * @param to The last day to include.
 * @returns The first day's midnight and the day after the last one's, as UTC.
 */
export const rangeBetween = (from: Date, to: Date) => ({
  from: dayRange(from).from,
  to: dayRange(to).to,
});

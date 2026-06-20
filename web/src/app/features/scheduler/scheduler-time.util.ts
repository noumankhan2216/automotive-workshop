/** Wall-clock helpers for the scheduler (always shop-local, never UTC getters). */

export function localStartOfDay(day: Date): Date {
  return new Date(day.getFullYear(), day.getMonth(), day.getDate(), 0, 0, 0, 0);
}

export function localEndOfDay(day: Date): Date {
  return new Date(day.getFullYear(), day.getMonth(), day.getDate(), 23, 59, 59, 999);
}

export function sameLocalDay(a: Date, b: Date): boolean {
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}

export function overlapsLocalDay(start: Date, end: Date, day: Date): boolean {
  const dayStart = localStartOfDay(day);
  const dayEnd = localEndOfDay(day);
  return start < dayEnd && end > dayStart;
}

export function minutesSinceLocalMidnight(d: Date): number {
  return d.getHours() * 60 + d.getMinutes();
}

/** Build a local Date on the same calendar day as `day` at hour:minute. */
export function localDateTimeOnDay(day: Date, hour: number, minute: number): Date {
  return new Date(day.getFullYear(), day.getMonth(), day.getDate(), hour, minute, 0, 0);
}

export function localDateTimeFromTotalMinutes(day: Date, totalMinutesFromMidnight: number): Date {
  const hour = Math.floor(totalMinutesFromMidnight / 60);
  const minute = totalMinutesFromMidnight % 60;
  return localDateTimeOnDay(day, hour, minute);
}

export function formatLocalTimeRange(startIso: string, endIso: string): string {
  const fmt = (d: Date) =>
    d.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit', hour12: true });
  const start = parseScheduleInstant(startIso);
  const end = parseScheduleInstant(endIso);
  return `${fmt(start)} – ${fmt(end)}`;
}

export function parseScheduleInstant(iso: string): Date {
  return new Date(iso);
}

const pad2 = (n: number) => String(n).padStart(2, '0');

/**
 * ISO 8601 with local offset (e.g. 2026-06-05T10:00:00+02:00).
 * Matches wall-clock times on the scheduler grid — unlike toISOString() which always shows Z/UTC.
 */
export function toApiScheduleTime(d: Date): string {
  const y = d.getFullYear();
  const m = pad2(d.getMonth() + 1);
  const day = pad2(d.getDate());
  const h = pad2(d.getHours());
  const min = pad2(d.getMinutes());
  const sec = pad2(d.getSeconds());
  const offsetMin = -d.getTimezoneOffset();
  const sign = offsetMin >= 0 ? '+' : '-';
  const abs = Math.abs(offsetMin);
  const offH = pad2(Math.floor(abs / 60));
  const offM = pad2(abs % 60);
  return `${y}-${m}-${day}T${h}:${min}:${sec}${sign}${offH}:${offM}`;
}

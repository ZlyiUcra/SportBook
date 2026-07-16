/**
 * All backend timestamps are UTC by project convention (plan.md), and court operating hours are
 * defined on the same clock - so times are shown as-is from the ISO string rather than shifted
 * to the browser timezone, keeping displayed slots consistent with operating hours.
 */
export function formatTime(iso: string): string {
  return iso.slice(11, 16)
}

export function formatDate(iso: string): string {
  return iso.slice(0, 10)
}

export function formatDateTime(iso: string): string {
  return `${formatDate(iso)} ${formatTime(iso)}`
}

/** yyyy-MM-dd for tomorrow - the default availability lookup date. */
export function tomorrowIsoDate(): string {
  const date = new Date()
  date.setDate(date.getDate() + 1)
  return date.toISOString().slice(0, 10)
}

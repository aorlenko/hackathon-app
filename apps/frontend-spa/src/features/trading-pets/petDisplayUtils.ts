/** Human-readable age from API fractional years (e.g. 1.5 → "1 year 6 months"). */
export function formatPetAgeYearsMonths(ageYears: number): string {
  if (!Number.isFinite(ageYears) || ageYears < 0) {
    return "—";
  }
  const totalMonths = Math.round(ageYears * 12);
  const years = Math.floor(totalMonths / 12);
  const months = totalMonths % 12;

  const parts: string[] = [];
  if (years > 0) {
    parts.push(`${years} ${years === 1 ? "year" : "years"}`);
  }
  if (months > 0) {
    parts.push(`${months} ${months === 1 ? "month" : "months"}`);
  }
  if (parts.length === 0) {
    return "under 1 month";
  }
  return parts.join(" ");
}

/** Stable short display for a pet UUID (no new server fields). */
export function formatShortPetId(id: string): string {
  const trimmed = id.trim();
  if (!trimmed) {
    return "—";
  }
  const parts = trimmed.split("-");
  const lastSegment = parts[parts.length - 1] ?? trimmed;
  const core =
    lastSegment.length > 8 ? lastSegment.slice(-8) : lastSegment.length >= 4 ? lastSegment : trimmed;
  return core.toUpperCase();
}

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

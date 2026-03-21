import type { TraderSnapshotDto } from "./tradingPetsApi";

export const PET_TRADER_SNAPSHOT_UPDATED = "pet-trader-snapshot-updated";

export function emitPetTraderSnapshotUpdated(detail: TraderSnapshotDto): void {
  window.dispatchEvent(
    new CustomEvent(PET_TRADER_SNAPSHOT_UPDATED, { detail }),
  );
}

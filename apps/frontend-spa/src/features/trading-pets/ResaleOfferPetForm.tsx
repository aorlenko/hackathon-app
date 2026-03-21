import { Link } from "react-router-dom";
import { useState } from "react";
import { createListing, type PetSummaryDto } from "./tradingPetsApi";
import { formatShortPetId } from "./petDisplayUtils";

type Props = {
  traderId: string;
  accessToken?: string;
  inventory: PetSummaryDto[];
  onListed: () => void | Promise<void>;
};

export const ResaleOfferPetForm = ({
  traderId,
  accessToken,
  inventory,
  onListed,
}: Props) => {
  const [petId, setPetId] = useState("");
  const [ask, setAsk] = useState(50);
  const [localError, setLocalError] = useState<string | null>(null);

  const listPet = async () => {
    setLocalError(null);
    const trimmedPetId = petId.trim();
    if (!trimmedPetId) {
      setLocalError("Choose a pet from your inventory before posting it for sale.");
      return;
    }
    try {
      await createListing({ traderId, petId: trimmedPetId, askingPrice: ask }, accessToken);
      await onListed();
    } catch (e) {
      setLocalError(e instanceof Error ? e.message : "Couldn’t post this pet for sale");
    }
  };

  return (
    <div className="trading-pets-form">
      <h3 className="trading-pets-subheading">Offer a pet for sale</h3>
      <label className="trading-pets-field">
        <span>Pet from your inventory</span>
        <select value={petId} onChange={(e) => setPetId(e.target.value)}>
          <option value="">Select…</option>
          {inventory.map((p) => (
            <option key={p.id} value={p.id}>
              {p.breedName} · {formatShortPetId(p.id)}
            </option>
          ))}
        </select>
      </label>
      <label className="trading-pets-field">
        <span>Asking price</span>
        <input
          type="number"
          min={1}
          value={ask}
          onChange={(e) => setAsk(Number(e.target.value) || 1)}
        />
      </label>
      <button
        type="button"
        className="primary-button"
        disabled={!petId.trim() || inventory.length === 0}
        onClick={() => void listPet()}
      >
        Post for sale
      </button>
      {inventory.length === 0 ? (
        <p className="muted small">
          Add pets from{" "}
          <Link to="/pets/primary-supply" className="trading-pets-text-link">
            Primary supply market
          </Link>
          , or open{" "}
          <Link to="/pets/my-pets" className="trading-pets-text-link">
            My pets
          </Link>{" "}
          to confirm your inventory — then choose a pet here to post for sale.
        </p>
      ) : null}
      {localError ? <p className="trading-pets-error trading-pets-error--soft">{localError}</p> : null}
    </div>
  );
};

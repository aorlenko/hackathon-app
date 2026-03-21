import { Link } from "react-router-dom";
import { useMyPetTrader } from "./MyPetTraderContext";
import { formatShortPetId } from "./petDisplayUtils";

export const MyPetsPage = () => {
  const { snapshot } = useMyPetTrader();
  const pets = snapshot?.pets ?? [];

  return (
    <div className="trading-pets-workspace trading-pets-page--my-pets">
      <header className="trading-pets-page__header">
        <h1>My pets</h1>
        <p className="muted trading-pets-page__intro trading-pets-page__intro--full">
          Inventory from your pet trader account — one row per pet. Buy new pets on{" "}
          <Link to="/pets/primary-supply" className="trading-pets-text-link">
            Primary supply market
          </Link>
          ; offer one for sale from the{" "}
          <Link to="/pets/resale" className="trading-pets-text-link">
            Resale marketplace
          </Link>
          .
        </p>
      </header>

      <section className="trading-pets-card" aria-labelledby="my-pets-list-title">
        <header className="trading-pets-card__header">
          <h2 id="my-pets-list-title">Your inventory</h2>
          <p className="muted small">{pets.length} pet{pets.length === 1 ? "" : "s"}</p>
        </header>
        <ul className="trading-pets-list trading-pets-owned-list">
          {pets.length === 0 ? (
            <li className="trading-pets-empty">
              You do not own any pets yet. Open{" "}
              <Link to="/pets/primary-supply" className="trading-pets-text-link">
                Primary supply market
              </Link>{" "}
              to buy from primary supply.
            </li>
          ) : (
            pets.map((p) => (
              <li key={p.id} className="trading-pets-owned-row trading-pets-owned-row--readonly">
                <div className="trading-pets-owned-row__main">
                  <div>
                    <strong>{p.breedName}</strong>
                    <span className="muted small"> · Pet ID {formatShortPetId(p.id)}</span>
                  </div>
                  <div className="muted small">
                    Age {p.ageYears.toFixed(2)}y · health {p.health.toFixed(0)}% · desirability{" "}
                    {p.currentDesirability}
                  </div>
                  <div className="muted small">
                    Intrinsic ${p.intrinsicValue.toFixed(2)} · maintenance ${p.maintenanceCost.toFixed(2)}
                    {p.isExpired ? " · expired" : ""}
                  </div>
                  <div className="trading-pets-owned-row__analysis">
                    <Link className="trading-pets-text-link" to={`/pets/analysis/${p.id}`}>
                      Open analysis
                    </Link>
                  </div>
                </div>
              </li>
            ))
          )}
        </ul>
      </section>
    </div>
  );
};

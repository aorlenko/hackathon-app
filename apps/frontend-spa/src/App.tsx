import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { FundsPanel } from "./features/account/FundsPanel";
import { useTradingAuth } from "./features/auth/AuthProvider";
import { ProtectedRoute } from "./features/auth/ProtectedRoute";

/** Must not navigate away from `/` until Auth0 has consumed `?code=` / `?error=` on the callback URL. */
const RootIndex = () => {
  const auth = useTradingAuth();
  if (auth.isLoading) {
    return (
      <section className="card">
        <h2>Starting app</h2>
        <p className="muted">Completing sign-in…</p>
      </section>
    );
  }
  return <Navigate to="/pets/workspace" replace />;
};
import { SettlementHistoryPage } from "./features/history/SettlementHistoryPage";
import { TradeHistoryPage } from "./features/history/TradeHistoryPage";
import {
  LeaderboardPage,
  MarketListingsPage,
  MyPetTraderProvider,
  PetAnalysisPage,
  TraderWorkspacePage,
} from "./features/trading-pets";

export const Header = () => {
  const auth = useTradingAuth();

  return (
    <header className="app-header">
      <div className="app-header__top">
        <div className="app-header__brand">
          <h1>Pet Ledger</h1>
          <p className="muted">
            Pet marketplace — primary supply, resale listings, and settlement history.
          </p>
        </div>
        <div className="auth-panel">
          {auth.authError ? (
            <p className="trading-pets-error small" role="alert">
              {auth.authError}
            </p>
          ) : null}
          {auth.isAuthenticated ? (
            <>
              <div className="auth-panel__summary">
                <div className="auth-panel__identity">
                  <strong className="auth-panel__name">{auth.displayName}</strong>
                  <p className="muted small">
                    {`Authenticated via ${auth.mode === "auth0" ? "Auth0" : "demo mode"}`}
                  </p>
                </div>
                <FundsPanel />
              </div>
              <button
                className="secondary-button auth-panel__signout"
                onClick={auth.logout}
              >
                Sign out
              </button>
            </>
          ) : (
            <button className="primary-button" onClick={() => void auth.login()}>
              {auth.mode === "auth0" ? "Sign in" : "Demo sign-in"}
            </button>
          )}
        </div>
      </div>
      <nav className="nav-links" aria-label="Primary">
        <NavLink to="/pets/workspace">Pet trading</NavLink>
        <NavLink to="/pets/market">Listings</NavLink>
        <NavLink to="/pets/leaderboard">Leaderboard</NavLink>
        <NavLink to="/history/trades">Trade history</NavLink>
        <NavLink to="/history/settlements">Settlement history</NavLink>
      </nav>
    </header>
  );
};

export const App = () => {
  return (
    <div className="app-shell">
      <Header />
      <main className="app-content">
        <Routes>
          <Route path="/" element={<RootIndex />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/history/trades" element={<TradeHistoryPage />} />
            <Route
              path="/history/settlements"
              element={<SettlementHistoryPage />}
            />
            <Route element={<MyPetTraderProvider />}>
              <Route path="/pets/workspace" element={<TraderWorkspacePage />} />
              <Route path="/pets/market" element={<MarketListingsPage />} />
              <Route path="/pets/leaderboard" element={<LeaderboardPage />} />
              <Route path="/pets/analysis/:petId" element={<PetAnalysisPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/pets/workspace" replace />} />
        </Routes>
      </main>
    </div>
  );
};

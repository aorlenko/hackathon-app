import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { FundsPanel } from "./features/account/FundsPanel";
import { useTradingAuth } from "./features/auth/AuthProvider";
import { ProtectedRoute } from "./features/auth/ProtectedRoute";
import { MarketDetailPage } from "./features/market/MarketDetailPage";
import { MarketOverviewPage } from "./features/market/MarketOverviewPage";
import { SettlementHistoryPage } from "./features/history/SettlementHistoryPage";
import { TradeHistoryPage } from "./features/history/TradeHistoryPage";

export const Header = () => {
  const auth = useTradingAuth();

  return (
    <header className="app-header">
      <div className="app-header__top">
        <div className="app-header__brand">
          <h1>Trading Lifecycle Demo</h1>
          <p className="muted">
            Order, trade, and settlement flows aligned to the platform contracts.
          </p>
        </div>
        <div className="auth-panel">
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
        <NavLink to="/">Markets</NavLink>
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
          <Route path="/" element={<MarketOverviewPage />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/markets/:symbol" element={<MarketDetailPage />} />
            <Route path="/history/trades" element={<TradeHistoryPage />} />
            <Route
              path="/history/settlements"
              element={<SettlementHistoryPage />}
            />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
};

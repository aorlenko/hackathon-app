import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { useTradingAuth } from "./features/auth/AuthProvider";
import { ProtectedRoute } from "./features/auth/ProtectedRoute";
import { MarketDetailPage } from "./features/market/MarketDetailPage";
import { MarketOverviewPage } from "./features/market/MarketOverviewPage";
import { SettlementHistoryPage } from "./features/history/SettlementHistoryPage";
import { TradeHistoryPage } from "./features/history/TradeHistoryPage";

const Header = () => {
  const auth = useTradingAuth();

  return (
    <header className="app-header">
      <div>
        <h1>Trading Lifecycle Demo</h1>
        <p className="muted">
          Order, trade, and settlement flows aligned to the platform contracts.
        </p>
      </div>
      <nav className="nav-links" aria-label="Primary">
        <NavLink to="/">Markets</NavLink>
        <NavLink to="/history/trades">Trade history</NavLink>
        <NavLink to="/history/settlements">Settlement history</NavLink>
      </nav>
      <div className="auth-panel">
        <div>
          <strong>{auth.displayName}</strong>
          <p className="muted small">
            {auth.isAuthenticated
              ? `Authenticated via ${auth.mode === "auth0" ? "Auth0" : "demo mode"}`
              : "Not signed in"}
          </p>
        </div>
        {auth.isAuthenticated ? (
          <button className="secondary-button" onClick={auth.logout}>
            Sign out
          </button>
        ) : (
          <button className="primary-button" onClick={() => void auth.login()}>
            {auth.mode === "auth0" ? "Sign in" : "Demo sign-in"}
          </button>
        )}
      </div>
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

import { Outlet, useLocation } from "react-router-dom";
import { useTradingAuth } from "./AuthProvider";

export const ProtectedRoute = () => {
  const auth = useTradingAuth();
  const location = useLocation();

  if (auth.isLoading) {
    return (
      <section className="card">
        <h2>Checking access</h2>
        <p>Restoring your trading session.</p>
      </section>
    );
  }

  if (!auth.isAuthenticated) {
    return (
      <section className="card">
        <h2>Sign in required</h2>
        <p>
          Trading actions require an active session. Sign in to view protected
          routes and place orders.
        </p>
        <button className="primary-button" onClick={() => void auth.login()}>
          {auth.mode === "auth0" ? "Continue with Auth0" : "Use demo sign-in"}
        </button>
        <p className="muted">
          Requested path: <code>{location.pathname}</code>
        </p>
      </section>
    );
  }

  return <Outlet />;
};

import { Outlet } from "react-router-dom";
import { useTradingAuth } from "./AuthProvider";

export const ProtectedRoute = () => {
  const auth = useTradingAuth();

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
        {auth.authError ? (
          <p className="trading-pets-error" role="alert">
            {auth.authError}
          </p>
        ) : null}
        <p>
          Trading actions require an active session. Sign in to view protected
          routes and place orders.
        </p>
        <button className="primary-button" onClick={() => void auth.login()}>
          {auth.mode === "auth0" ? "Continue with Auth0" : "Use demo sign-in"}
        </button>
      </section>
    );
  }

  return <Outlet />;
};

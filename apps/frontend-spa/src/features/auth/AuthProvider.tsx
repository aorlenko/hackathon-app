import {
  Auth0Provider,
  useAuth0,
  type AppState,
  type RedirectLoginOptions,
} from "@auth0/auth0-react";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from "react";
import { env, hasAuth0Config } from "../../config/env";
import { bootstrapDemoAccount } from "./authApi";

const DEMO_USER_STORAGE_KEY = "trading.demo-user-id";

export interface TradingAuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  userId: string | null;
  displayName: string;
  accessToken?: string;
  mode: "auth0" | "demo";
  login: () => Promise<void>;
  logout: () => void;
}

const TradingAuthContext = createContext<TradingAuthState | undefined>(undefined);

const pickFriendlyDisplayName = (user: ReturnType<typeof useAuth0>["user"]) =>
  user?.name ??
  user?.nickname ??
  user?.given_name ??
  user?.email ??
  "Trader";

const Auth0ContextBridge = ({ children }: PropsWithChildren) => {
  const auth0 = useAuth0();
  const [accessToken, setAccessToken] = useState<string>();
  const [isBootstrappingAccount, setIsBootstrappingAccount] = useState(false);
  const [bootstrappedUserId, setBootstrappedUserId] = useState<string | null>(
    null,
  );

  const login = useCallback(async () => {
    const options: RedirectLoginOptions<AppState> = {
      appState: {
        returnTo: window.location.pathname + window.location.search,
      },
      authorizationParams: {
        audience: env.auth0Audience || undefined,
        scope: "openid profile email",
      },
    };

    await auth0.loginWithRedirect(options);
  }, [auth0]);

  const logout = useCallback(() => {
    auth0.logout({
      logoutParams: {
        returnTo: window.location.origin,
      },
    });
  }, [auth0]);

  useEffect(() => {
    if (!auth0.isAuthenticated) {
      setAccessToken(undefined);
      setBootstrappedUserId(null);
      setIsBootstrappingAccount(false);
      return;
    }

    let active = true;

    void auth0
      .getAccessTokenSilently({
        authorizationParams: {
          audience: env.auth0Audience || undefined,
        },
      })
      .then((token) => {
        if (active) {
          setAccessToken(token);
        }
      })
      .catch(() => {
        if (active) {
          setAccessToken(undefined);
        }
      });

    return () => {
      active = false;
    };
  }, [auth0]);

  useEffect(() => {
    const userId = auth0.user?.sub ?? null;
    if (!auth0.isAuthenticated || !accessToken || !userId) {
      setIsBootstrappingAccount(false);
      if (!auth0.isAuthenticated) {
        setBootstrappedUserId(null);
      }
      return;
    }

    if (bootstrappedUserId === userId) {
      return;
    }

    let active = true;
    setIsBootstrappingAccount(true);

    void bootstrapDemoAccount(
      {
        displayName: pickFriendlyDisplayName(auth0.user),
        email: auth0.user?.email,
      },
      accessToken,
    )
      .then(() => {
        if (active) {
          setBootstrappedUserId(userId);
        }
      })
      .catch((error: unknown) => {
        console.error("Failed to bootstrap demo trading account.", error);
      })
      .finally(() => {
        if (active) {
          setIsBootstrappingAccount(false);
        }
      });

    return () => {
      active = false;
    };
  }, [
    accessToken,
    auth0.isAuthenticated,
    auth0.user,
    auth0.user?.email,
    auth0.user?.sub,
    bootstrappedUserId,
  ]);

  const value = useMemo<TradingAuthState>(
    () => ({
      isAuthenticated: auth0.isAuthenticated,
      isLoading: auth0.isLoading || isBootstrappingAccount,
      userId: auth0.user?.sub ?? null,
      displayName: pickFriendlyDisplayName(auth0.user),
      accessToken,
      mode: "auth0",
      login,
      logout,
    }),
    [
      auth0.isAuthenticated,
      auth0.isLoading,
      auth0.user?.email,
      auth0.user?.name,
      auth0.user?.sub,
      accessToken,
      isBootstrappingAccount,
      login,
      logout,
    ],
  );

  return (
    <TradingAuthContext.Provider value={value}>
      {children}
    </TradingAuthContext.Provider>
  );
};

const DemoAuthProvider = ({ children }: PropsWithChildren) => {
  const [userId, setUserId] = useState<string | null>(() => {
    if (typeof window === "undefined") {
      return null;
    }

    const storedUserId = window.localStorage.getItem(DEMO_USER_STORAGE_KEY);
    return storedUserId === "demo-user" ? "user-1" : storedUserId;
  });

  const login = useCallback(async () => {
    const nextUserId = "user-1";
    window.localStorage.setItem(DEMO_USER_STORAGE_KEY, nextUserId);
    setUserId(nextUserId);
  }, []);

  const logout = useCallback(() => {
    window.localStorage.removeItem(DEMO_USER_STORAGE_KEY);
    setUserId(null);
  }, []);

  const value = useMemo<TradingAuthState>(
    () => ({
      isAuthenticated: Boolean(userId),
      isLoading: false,
      userId,
      displayName: userId ? "Demo Trader" : "Guest",
      accessToken: userId ?? undefined,
      mode: "demo",
      login,
      logout,
    }),
    [login, logout, userId],
  );

  return (
    <TradingAuthContext.Provider value={value}>
      {children}
    </TradingAuthContext.Provider>
  );
};

export const TradingAuthProvider = ({ children }: PropsWithChildren) => {
  if (hasAuth0Config) {
    return (
      <Auth0Provider
        domain={env.auth0Domain}
        clientId={env.auth0ClientId}
        authorizationParams={{
          audience: env.auth0Audience || undefined,
          redirect_uri: window.location.origin,
          scope: "openid profile email",
        }}
      >
        <Auth0ContextBridge>{children}</Auth0ContextBridge>
      </Auth0Provider>
    );
  }

  if (!env.enableDemoAuth) {
    return (
      <TradingAuthContext.Provider
        value={{
          isAuthenticated: false,
          isLoading: false,
          userId: null,
          displayName: "Guest",
          accessToken: undefined,
          mode: "demo",
          login: async () => undefined,
          logout: () => undefined,
        }}
      >
        {children}
      </TradingAuthContext.Provider>
    );
  }

  return <DemoAuthProvider>{children}</DemoAuthProvider>;
};

export const useTradingAuth = (): TradingAuthState => {
  const context = useContext(TradingAuthContext);
  if (!context) {
    throw new Error("useTradingAuth must be used within TradingAuthProvider");
  }

  return context;
};

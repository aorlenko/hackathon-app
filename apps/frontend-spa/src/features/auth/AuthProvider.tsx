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
  useRef,
  useState,
  type PropsWithChildren,
} from "react";
import type { AccountSnapshot } from "../../contracts/trading";
import { env, hasAuth0Config } from "../../config/env";
import { bootstrapDemoAccount } from "./authApi";

const DEMO_USER_STORAGE_KEY = "trading.demo-user-id";
const DEMO_ACCOUNT_BOOTSTRAP = {
  displayName: "Demo Trader",
  email: "demo-trader@example.com",
};

export interface TradingAuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  userId: string | null;
  displayName: string;
  accessToken?: string;
  accountSnapshot: AccountSnapshot | null;
  /** Set when Auth0 returns an error (e.g. access_denied) on the callback URL. */
  authError: string | null;
  mode: "auth0" | "demo";
  login: () => Promise<void>;
  logout: () => void;
}

export const TradingAuthContext = createContext<TradingAuthState | undefined>(
  undefined,
);

const pickFriendlyDisplayName = (user: ReturnType<typeof useAuth0>["user"]) =>
  user?.name ??
  user?.nickname ??
  user?.given_name ??
  user?.email ??
  "Trader";

const Auth0ContextBridge = ({ children }: PropsWithChildren) => {
  const auth0 = useAuth0();
  const [accessToken, setAccessToken] = useState<string>();
  const [isResolvingAccessToken, setIsResolvingAccessToken] = useState(false);
  const [accountSnapshot, setAccountSnapshot] = useState<AccountSnapshot | null>(
    null,
  );
  const [isBootstrappingAccount, setIsBootstrappingAccount] = useState(false);
  /** Avoid duplicate bootstraps, but allow a second call when `user.email` arrives after the first (fixes placeholder trader-*@demo.local). */
  const lastSuccessfulBootstrapKeyRef = useRef<string | null>(null);
  /** Ignore stale bootstrap responses when deps change while a request is in flight (out-of-order completion left nonsense emails on the account row). */
  const bootstrapSeqRef = useRef(0);

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
      setIsResolvingAccessToken(false);
      setAccountSnapshot(null);
      setIsBootstrappingAccount(false);
      return;
    }

    let active = true;
    setIsResolvingAccessToken(true);

    void auth0
      .getAccessTokenSilently({
        authorizationParams: {
          audience: env.auth0Audience || undefined,
        },
      })
      .then((token) => {
        if (active) {
          setAccessToken(token);
          setIsResolvingAccessToken(false);
        }
      })
      .catch(() => {
        if (active) {
          setAccessToken(undefined);
          setIsResolvingAccessToken(false);
        }
      });

    return () => {
      active = false;
    };
  }, [auth0]);

  useEffect(() => {
    if (!auth0.isAuthenticated) {
      lastSuccessfulBootstrapKeyRef.current = null;
      setIsBootstrappingAccount(false);
      setAccountSnapshot(null);
      return;
    }

    const userId = auth0.user?.sub ?? null;

    if (auth0.isLoading || !accessToken || !userId) {
      setIsBootstrappingAccount(false);
      return;
    }

    const seq = ++bootstrapSeqRef.current;
    let active = true;
    setAccountSnapshot((current) =>
      current?.userId === userId ? current : null,
    );
    setIsBootstrappingAccount(true);

    void (async () => {
      let email = auth0.user?.email?.trim() ?? "";
      if (!email) {
        try {
          const claims = await auth0.getIdTokenClaims();
          const fromClaims =
            typeof claims?.email === "string" ? claims.email.trim() : "";
          if (fromClaims) {
            email = fromClaims;
          }
        } catch {
          /* ID token not ready yet; a later effect run may pick up email */
        }
      }

      if (!active || seq !== bootstrapSeqRef.current) {
        return;
      }

      const bootstrapKey = `${userId}\0${email}`;
      if (lastSuccessfulBootstrapKeyRef.current === bootstrapKey) {
        setIsBootstrappingAccount(false);
        return;
      }

      try {
        const snapshot = await bootstrapDemoAccount(
          {
            displayName: pickFriendlyDisplayName(auth0.user),
            email: email || undefined,
          },
          accessToken,
        );
        if (!active || seq !== bootstrapSeqRef.current) {
          return;
        }
        setAccountSnapshot(snapshot);
        lastSuccessfulBootstrapKeyRef.current = bootstrapKey;
      } catch (error: unknown) {
        console.error("Failed to bootstrap demo trading account.", error);
      } finally {
        if (active && seq === bootstrapSeqRef.current) {
          setIsBootstrappingAccount(false);
        }
      }
    })();

    return () => {
      active = false;
    };
  }, [
    accessToken,
    auth0,
    auth0.isAuthenticated,
    auth0.isLoading,
    auth0.user,
    auth0.user?.email,
    auth0.user?.sub,
  ]);

  const authError = auth0.error ? auth0.error.message : null;

  const value = useMemo<TradingAuthState>(
    () => ({
      isAuthenticated: auth0.isAuthenticated,
      isLoading:
        auth0.isLoading || isResolvingAccessToken || isBootstrappingAccount,
      userId: auth0.user?.sub ?? null,
      displayName: pickFriendlyDisplayName(auth0.user),
      accessToken,
      accountSnapshot,
      authError,
      mode: "auth0",
      login,
      logout,
    }),
    [
      auth0.isAuthenticated,
      auth0.isLoading,
      auth0.error,
      auth0.user?.email,
      auth0.user?.name,
      auth0.user?.sub,
      accessToken,
      accountSnapshot,
      authError,
      isBootstrappingAccount,
      isResolvingAccessToken,
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
  const [accountSnapshot, setAccountSnapshot] = useState<AccountSnapshot | null>(
    null,
  );
  const [bootstrappedUserId, setBootstrappedUserId] = useState<string | null>(
    null,
  );
  const [isBootstrappingAccount, setIsBootstrappingAccount] = useState(false);

  const login = useCallback(async () => {
    const nextUserId = "user-1";
    window.localStorage.setItem(DEMO_USER_STORAGE_KEY, nextUserId);
    setUserId(nextUserId);
  }, []);

  const logout = useCallback(() => {
    window.localStorage.removeItem(DEMO_USER_STORAGE_KEY);
    setAccountSnapshot(null);
    setBootstrappedUserId(null);
    setIsBootstrappingAccount(false);
    setUserId(null);
  }, []);

  useEffect(() => {
    if (!userId) {
      setAccountSnapshot(null);
      setBootstrappedUserId(null);
      setIsBootstrappingAccount(false);
      return;
    }

    if (bootstrappedUserId === userId) {
      return;
    }

    let active = true;
    setAccountSnapshot((current) => (current?.userId === userId ? current : null));
    setIsBootstrappingAccount(true);

    void bootstrapDemoAccount(DEMO_ACCOUNT_BOOTSTRAP, userId)
      .then((snapshot) => {
        if (active) {
          setAccountSnapshot(snapshot);
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
  }, [bootstrappedUserId, userId]);

  const value = useMemo<TradingAuthState>(
    () => ({
      isAuthenticated: Boolean(userId),
      isLoading: isBootstrappingAccount,
      userId,
      displayName: userId ? "Demo Trader" : "Guest",
      accessToken: userId ?? undefined,
      accountSnapshot,
      authError: null,
      mode: "demo",
      login,
      logout,
    }),
    [accountSnapshot, isBootstrappingAccount, login, logout, userId],
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
        cacheLocation="localstorage"
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
          accountSnapshot: null,
          authError: null,
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

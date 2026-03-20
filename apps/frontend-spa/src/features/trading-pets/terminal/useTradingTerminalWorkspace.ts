import { useCallback, useEffect, useRef, useState } from "react";
import {
  getTerminalMarkets,
  getTerminalWorkspace,
  type TerminalMarketRowDto,
  type TerminalWorkspaceDto,
} from "../tradingPetsApi";

type Args = {
  accessToken?: string;
  /** Bumps from realtime / parent to refetch markets + workspace without losing last-good data on failure. */
  invalidateKey: number;
};

export type TradingTerminalWorkspaceState = {
  markets: TerminalMarketRowDto[];
  selectedMarketEntryId: string | null;
  selectMarket: (marketEntryId: string) => void;
  workspace: TerminalWorkspaceDto | null;
  marketsLoading: boolean;
  workspaceLoading: boolean;
  marketsError: string | null;
  workspaceError: string | null;
  refreshMarkets: () => Promise<void>;
  refreshWorkspace: () => Promise<void>;
};

export const useTradingTerminalWorkspace = ({
  accessToken,
  invalidateKey,
}: Args): TradingTerminalWorkspaceState => {
  const [markets, setMarkets] = useState<TerminalMarketRowDto[]>([]);
  const [selectedMarketEntryId, setSelectedMarketEntryId] = useState<
    string | null
  >(null);
  const [workspace, setWorkspace] = useState<TerminalWorkspaceDto | null>(
    null,
  );
  const [marketsLoading, setMarketsLoading] = useState(false);
  const [workspaceLoading, setWorkspaceLoading] = useState(false);
  const [marketsError, setMarketsError] = useState<string | null>(null);
  const [workspaceError, setWorkspaceError] = useState<string | null>(null);
  const lastWorkspaceMarketIdRef = useRef<string | null>(null);
  const workspaceFetchSeqRef = useRef(0);

  const loadMarkets = useCallback(async () => {
    if (!accessToken) {
      return;
    }
    setMarketsLoading(true);
    setMarketsError(null);
    try {
      const rows = await getTerminalMarkets(accessToken);
      setMarkets(rows);
      setSelectedMarketEntryId((prev) => {
        if (prev && rows.some((r) => r.marketEntryId === prev)) {
          return prev;
        }
        return rows[0]?.marketEntryId ?? null;
      });
    } catch (e) {
      setMarketsError(
        e instanceof Error ? e.message : "Could not load terminal markets",
      );
    } finally {
      setMarketsLoading(false);
    }
  }, [accessToken]);

  const loadWorkspace = useCallback(async () => {
    if (!accessToken || !selectedMarketEntryId) {
      lastWorkspaceMarketIdRef.current = null;
      setWorkspace(null);
      setWorkspaceError(null);
      return;
    }
    const id = selectedMarketEntryId;
    const selectionChanged = lastWorkspaceMarketIdRef.current !== id;
    lastWorkspaceMarketIdRef.current = id;
    setWorkspaceLoading(true);
    setWorkspaceError(null);
    if (selectionChanged) {
      setWorkspace(null);
    }
    const seq = ++workspaceFetchSeqRef.current;
    try {
      const snap = await getTerminalWorkspace(id, accessToken);
      if (seq !== workspaceFetchSeqRef.current) {
        return;
      }
      setWorkspace(snap);
      setWorkspaceError(null);
    } catch (e) {
      if (seq !== workspaceFetchSeqRef.current) {
        return;
      }
      const msg =
        e instanceof Error ? e.message : "Could not load workspace snapshot";
      setWorkspaceError(msg);
      setWorkspace((prev) =>
        prev?.marketEntry.marketEntryId === id ? prev : null,
      );
    } finally {
      if (seq === workspaceFetchSeqRef.current) {
        setWorkspaceLoading(false);
      }
    }
  }, [accessToken, selectedMarketEntryId]);

  useEffect(() => {
    void loadMarkets();
  }, [loadMarkets, invalidateKey]);

  useEffect(() => {
    void loadWorkspace();
  }, [loadWorkspace, invalidateKey]);

  const selectMarket = useCallback((marketEntryId: string) => {
    setSelectedMarketEntryId(marketEntryId);
  }, []);

  return {
    markets,
    selectedMarketEntryId,
    selectMarket,
    workspace,
    marketsLoading,
    workspaceLoading,
    marketsError,
    workspaceError,
    refreshMarkets: loadMarkets,
    refreshWorkspace: loadWorkspace,
  };
};

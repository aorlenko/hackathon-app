import { useCallback, useEffect, useRef, useState } from "react";
import {
  getTerminalMarkets,
  getTerminalWorkspace,
  type TerminalMarketRowDto,
  type TerminalWorkspaceDto,
} from "../tradingPetsApi";
import {
  buildTerminalMarketHighlights,
  buildTerminalWorkspaceHighlights,
  type TerminalMarketHighlight,
  type TerminalWorkspaceHighlights,
} from "./terminalHighlights";

const HIGHLIGHT_MS = 2500;

type Args = {
  accessToken?: string;
  /** Bumps from realtime / parent to refetch markets + workspace without losing last-good data on failure. */
  invalidateKey: number;
};

export type TradingTerminalWorkspaceState = {
  markets: TerminalMarketRowDto[];
  marketHighlights: Record<string, TerminalMarketHighlight>;
  selectedMarketEntryId: string | null;
  selectMarket: (marketEntryId: string) => void;
  workspace: TerminalWorkspaceDto | null;
  newTradeIds: string[];
  bidLevelHighlights: TerminalWorkspaceHighlights["bidLevels"];
  askLevelHighlights: TerminalWorkspaceHighlights["askLevels"];
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
  const [marketHighlights, setMarketHighlights] = useState<
    Record<string, TerminalMarketHighlight>
  >({});
  const [selectedMarketEntryId, setSelectedMarketEntryId] = useState<
    string | null
  >(null);
  const [workspace, setWorkspace] = useState<TerminalWorkspaceDto | null>(
    null,
  );
  const [newTradeIds, setNewTradeIds] = useState<string[]>([]);
  const [bidLevelHighlights, setBidLevelHighlights] = useState<
    TerminalWorkspaceHighlights["bidLevels"]
  >({});
  const [askLevelHighlights, setAskLevelHighlights] = useState<
    TerminalWorkspaceHighlights["askLevels"]
  >({});
  const [marketsLoading, setMarketsLoading] = useState(false);
  const [workspaceLoading, setWorkspaceLoading] = useState(false);
  const [marketsError, setMarketsError] = useState<string | null>(null);
  const [workspaceError, setWorkspaceError] = useState<string | null>(null);
  const marketsRef = useRef<TerminalMarketRowDto[]>([]);
  const workspaceRef = useRef<TerminalWorkspaceDto | null>(null);
  const lastWorkspaceMarketIdRef = useRef<string | null>(null);
  const workspaceFetchSeqRef = useRef(0);
  const marketHighlightTimerRef = useRef<number | undefined>(undefined);
  const workspaceHighlightTimerRef = useRef<number | undefined>(undefined);

  const clearMarketHighlightsLater = useCallback(() => {
    if (marketHighlightTimerRef.current) {
      window.clearTimeout(marketHighlightTimerRef.current);
    }

    marketHighlightTimerRef.current = window.setTimeout(() => {
      setMarketHighlights({});
      marketHighlightTimerRef.current = undefined;
    }, HIGHLIGHT_MS);
  }, []);

  const clearWorkspaceHighlightsLater = useCallback(() => {
    if (workspaceHighlightTimerRef.current) {
      window.clearTimeout(workspaceHighlightTimerRef.current);
    }

    workspaceHighlightTimerRef.current = window.setTimeout(() => {
      setNewTradeIds([]);
      setBidLevelHighlights({});
      setAskLevelHighlights({});
      workspaceHighlightTimerRef.current = undefined;
    }, HIGHLIGHT_MS);
  }, []);

  const loadMarkets = useCallback(async () => {
    if (!accessToken) {
      return;
    }
    setMarketsLoading(true);
    setMarketsError(null);
    try {
      const rows = await getTerminalMarkets(accessToken);
      const nextHighlights = buildTerminalMarketHighlights(
        marketsRef.current,
        rows,
      );

      if (Object.keys(nextHighlights).length > 0) {
        setMarketHighlights(nextHighlights);
        clearMarketHighlightsLater();
      }

      marketsRef.current = rows;
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
      workspaceRef.current = null;
      setWorkspace(null);
      setWorkspaceError(null);
      setNewTradeIds([]);
      setBidLevelHighlights({});
      setAskLevelHighlights({});
      return;
    }
    const id = selectedMarketEntryId;
    const selectionChanged = lastWorkspaceMarketIdRef.current !== id;
    lastWorkspaceMarketIdRef.current = id;
    setWorkspaceLoading(true);
    setWorkspaceError(null);
    if (selectionChanged) {
      workspaceRef.current = null;
      setWorkspace(null);
      setNewTradeIds([]);
      setBidLevelHighlights({});
      setAskLevelHighlights({});
    }
    const seq = ++workspaceFetchSeqRef.current;
    try {
      const snap = await getTerminalWorkspace(id, accessToken);
      if (seq !== workspaceFetchSeqRef.current) {
        return;
      }
      const nextHighlights = buildTerminalWorkspaceHighlights(
        workspaceRef.current,
        snap,
      );

      if (
        nextHighlights.newTradeIds.length > 0 ||
        Object.keys(nextHighlights.bidLevels).length > 0 ||
        Object.keys(nextHighlights.askLevels).length > 0
      ) {
        setNewTradeIds(nextHighlights.newTradeIds);
        setBidLevelHighlights(nextHighlights.bidLevels);
        setAskLevelHighlights(nextHighlights.askLevels);
        clearWorkspaceHighlightsLater();
      }

      workspaceRef.current = snap;
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

  useEffect(
    () => () => {
      if (marketHighlightTimerRef.current) {
        window.clearTimeout(marketHighlightTimerRef.current);
      }

      if (workspaceHighlightTimerRef.current) {
        window.clearTimeout(workspaceHighlightTimerRef.current);
      }
    },
    [],
  );

  const selectMarket = useCallback((marketEntryId: string) => {
    setSelectedMarketEntryId(marketEntryId);
  }, []);

  return {
    markets,
    marketHighlights,
    selectedMarketEntryId,
    selectMarket,
    workspace,
    newTradeIds,
    bidLevelHighlights,
    askLevelHighlights,
    marketsLoading,
    workspaceLoading,
    marketsError,
    workspaceError,
    refreshMarkets: loadMarkets,
    refreshWorkspace: loadWorkspace,
  };
};

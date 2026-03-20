import { useCallback, useState } from "react";
import { useTradingAuth } from "../auth/AuthProvider";
import { useMyPetTrader } from "./MyPetTraderContext";
import {
  TerminalMarketList,
  TerminalOrderBook,
  TerminalTradeFeed,
  TerminalTradingPanel,
  useTradingTerminalWorkspace,
} from "./terminal";
import { useTradingPetsRealtime } from "./useTradingPetsRealtime";

export const TraderWorkspacePage = () => {
  const auth = useTradingAuth();
  const { traderId, snapshot, refresh } = useMyPetTrader();
  const [panelTick, setPanelTick] = useState(0);

  const onRealtimeInvalidate = useCallback(async () => {
    setPanelTick((n) => n + 1);
    try {
      await refresh();
    } catch {
      /* snapshot optional for cross-user listing updates */
    }
  }, [refresh]);

  useTradingPetsRealtime({
    traderId,
    accessToken: auth.accessToken,
    onRefreshSnapshot: onRealtimeInvalidate,
  });

  const terminal = useTradingTerminalWorkspace({
    accessToken: auth.accessToken,
    invalidateKey: panelTick,
  });

  const marketEntry =
    terminal.workspace?.marketEntry ??
    terminal.markets.find(
      (m) => m.marketEntryId === terminal.selectedMarketEntryId,
    ) ??
    null;

  return (
    <div className="trading-pets-workspace trading-pets-workspace--terminal">
      <header className="trading-pets-page__header">
        <h1>Trading terminal</h1>
        <p className="muted">
          Read-only workspace: markets, depth, recent prints, and account context
          for the selected breed market.
        </p>
      </header>

      {snapshot ? (
        <section className="trading-pets-summary" aria-label="Portfolio snapshot">
          <div>
            <div className="muted small">Available cash</div>
            <strong>${snapshot.availableCash.toFixed(2)}</strong>
          </div>
          <div>
            <div className="muted small">Locked cash</div>
            <strong>${snapshot.lockedCash.toFixed(2)}</strong>
          </div>
          <div>
            <div className="muted small">Portfolio total</div>
            <strong>${snapshot.portfolioTotal.toFixed(2)}</strong>
          </div>
        </section>
      ) : null}

      <div className="trading-pets-terminal">
        <TerminalMarketList
          markets={terminal.markets}
          selectedMarketEntryId={terminal.selectedMarketEntryId}
          onSelect={terminal.selectMarket}
          loading={terminal.marketsLoading}
          error={terminal.marketsError}
        />
        <TerminalOrderBook
          marketEntry={marketEntry}
          orderBook={terminal.workspace?.orderBook ?? null}
          loading={terminal.workspaceLoading}
          error={terminal.workspaceError}
        />
        <TerminalTradingPanel
          marketEntry={marketEntry}
          accountSummary={terminal.workspace?.accountSummary ?? null}
          loading={terminal.workspaceLoading}
          error={terminal.workspaceError}
        />
        <TerminalTradeFeed
          marketLabel={marketEntry?.displayName ?? null}
          trades={terminal.workspace?.recentTrades ?? []}
          loading={terminal.workspaceLoading}
          error={terminal.workspaceError}
        />
      </div>
    </div>
  );
};

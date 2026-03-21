import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useEffect, useRef } from "react";
import { env } from "../../config/env";

const POLL_MS = 2000;
const DISCONNECT_POLL_AFTER_MS = 3000;

type Args = {
  traderId: string;
  accessToken?: string;
  onRefreshSnapshot: () => void | Promise<void>;
};

export const useTradingPetsRealtime = ({
  traderId,
  accessToken,
  onRefreshSnapshot,
}: Args) => {
  const refreshRef = useRef(onRefreshSnapshot);
  refreshRef.current = onRefreshSnapshot;

  useEffect(() => {
    if (!traderId || !accessToken) {
      return;
    }

    let pollTimer: number | undefined;
    let disconnectTimer: number | undefined;
    let cancelled = false;

    const startPoll = () => {
      if (cancelled) {
        return;
      }
      if (pollTimer) {
        window.clearInterval(pollTimer);
      }
      void refreshRef.current();
      pollTimer = window.setInterval(() => {
        void refreshRef.current();
      }, POLL_MS);
    };

    const stopPoll = () => {
      if (pollTimer) {
        window.clearInterval(pollTimer);
        pollTimer = undefined;
      }
    };

    const schedulePollFallback = () => {
      if (disconnectTimer) {
        window.clearTimeout(disconnectTimer);
      }
      disconnectTimer = window.setTimeout(() => {
        startPoll();
      }, DISCONNECT_POLL_AFTER_MS);
    };

    const connection = new HubConnectionBuilder()
      .withUrl(env.marketHubUrl, {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    const bind = async () => {
      connection.on("trader.snapshotUpdated", () => void refreshRef.current());
      connection.on("market.listingsUpdated", () => void refreshRef.current());
      connection.on("trader.notificationsAdded", () => void refreshRef.current());
      connection.on("leaderboard.updated", () => void refreshRef.current());
      connection.on("pet.valuationBatch", () => void refreshRef.current());
    };

    void (async () => {
      await bind();
      try {
        await connection.start();
        if (cancelled) {
          return;
        }
        stopPoll();
        if (disconnectTimer) {
          window.clearTimeout(disconnectTimer);
          disconnectTimer = undefined;
        }
        await connection.invoke("SubscribeTradingPetsTrader", traderId);
        await connection.invoke("SubscribeTradingPetsMarket");
        await connection.invoke("SubscribeTradingPetsLeaderboard");
      } catch {
        if (!cancelled) {
          // Hub is up but group subscribe failed — poll now so listings/notifications still converge.
          if (connection.state === HubConnectionState.Connected) {
            startPoll();
          } else {
            schedulePollFallback();
          }
        }
      }
    })();

    connection.onreconnected(() => {
      stopPoll();
      if (disconnectTimer) {
        window.clearTimeout(disconnectTimer);
        disconnectTimer = undefined;
      }
      void (async () => {
        try {
          await connection.invoke("SubscribeTradingPetsTrader", traderId);
          await connection.invoke("SubscribeTradingPetsMarket");
          await connection.invoke("SubscribeTradingPetsLeaderboard");
        } catch {
          if (connection.state === HubConnectionState.Connected) {
            startPoll();
          }
        }
      })();
    });

    connection.onclose(() => {
      if (cancelled) {
        return;
      }
      schedulePollFallback();
    });

    return () => {
      cancelled = true;
      stopPoll();
      if (disconnectTimer) {
        window.clearTimeout(disconnectTimer);
      }
      void connection.stop();
    };
  }, [accessToken, traderId]);
};

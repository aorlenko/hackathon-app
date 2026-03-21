import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useEffect, useRef } from "react";
import { env } from "../../config/env";

const POLL_MS = 5000;
const DISCONNECT_POLL_AFTER_MS = 10_000;

type Args = {
  traderId: string;
  accessToken?: string;
  onRefreshSnapshot: () => void | Promise<void>;
  /** Called when the hub signals new trader notifications (in addition to snapshot refresh). */
  onTraderNotificationsAdded?: () => void | Promise<void>;
};

export const useTradingPetsRealtime = ({
  traderId,
  accessToken,
  onRefreshSnapshot,
  onTraderNotificationsAdded,
}: Args) => {
  const refreshRef = useRef(onRefreshSnapshot);
  refreshRef.current = onRefreshSnapshot;
  const notificationsHintRef = useRef(onTraderNotificationsAdded);
  notificationsHintRef.current = onTraderNotificationsAdded;

  useEffect(() => {
    if (!traderId || !accessToken) {
      return;
    }

    let pollTimer: number | undefined;
    let disconnectTimer: number | undefined;
    let cancelled = false;

    const pullNotifications = () => {
      void notificationsHintRef.current?.();
    };

    const tickPoll = () => {
      void refreshRef.current();
      pullNotifications();
    };

    const startPoll = () => {
      if (pollTimer) {
        window.clearInterval(pollTimer);
      }
      pollTimer = window.setInterval(() => {
        tickPoll();
      }, POLL_MS);
    };

    const subscribeTradingPetsGroups = async () => {
      const inv = connection.invoke.bind(connection);
      try {
        await inv("SubscribeTradingPetsTrader", traderId);
      } catch {
        /* trader group optional for snapshot; market group still useful */
      }
      try {
        await inv("SubscribeTradingPetsMarket");
      } catch {
        /* listings + reject fan-out use this group */
      }
      try {
        await inv("SubscribeTradingPetsLeaderboard");
      } catch {
        /* optional */
      }
    };

    const stopPoll = () => {
      if (pollTimer) {
        window.clearInterval(pollTimer);
        pollTimer = undefined;
      }
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
      connection.on("market.listingsUpdated", () => {
        void refreshRef.current();
        pullNotifications();
      });
      connection.on("trader.notificationsAdded", () => {
        void refreshRef.current();
        pullNotifications();
      });
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
        await subscribeTradingPetsGroups();
      } catch {
        if (!cancelled) {
          // Hub is up but start/subscribe failed — poll so listings, snapshot, and notification toasts still converge.
          if (connection.state === HubConnectionState.Connected) {
            startPoll();
          } else {
            disconnectTimer = window.setTimeout(() => {
              startPoll();
            }, DISCONNECT_POLL_AFTER_MS);
          }
        }
      }
    })();

    connection.onreconnected(() => {
      stopPoll();
      void (async () => {
        try {
          await subscribeTradingPetsGroups();
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
      disconnectTimer = window.setTimeout(() => {
        startPoll();
      }, DISCONNECT_POLL_AFTER_MS);
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

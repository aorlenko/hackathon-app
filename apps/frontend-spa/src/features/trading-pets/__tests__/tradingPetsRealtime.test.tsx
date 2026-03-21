import { act, render } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useTradingPetsRealtime } from "../useTradingPetsRealtime";

const mocks = vi.hoisted(() => {
  const eventHandlers = new Map<string, () => void>();
  let onCloseHandler: (() => void) | undefined;
  let onReconnectedHandler: (() => void) | undefined;

  const connection = {
    state: "Disconnected",
    start: vi.fn(async () => {
      connection.state = "Connected";
    }),
    stop: vi.fn(async () => {}),
    on: vi.fn((eventName: string, handler: () => void) => {
      eventHandlers.set(eventName, handler);
    }),
    onreconnected: vi.fn((handler: () => void) => {
      onReconnectedHandler = handler;
    }),
    onclose: vi.fn((handler: () => void) => {
      onCloseHandler = handler;
    }),
    invoke: vi.fn(async () => {}),
  };

  return {
    connection,
    reset() {
      eventHandlers.clear();
      onCloseHandler = undefined;
      onReconnectedHandler = undefined;
      connection.state = "Disconnected";
      connection.start.mockClear();
      connection.stop.mockClear();
      connection.on.mockClear();
      connection.onreconnected.mockClear();
      connection.onclose.mockClear();
      connection.invoke.mockClear();
    },
    triggerEvent(eventName: string) {
      eventHandlers.get(eventName)?.();
    },
    triggerClose() {
      connection.state = "Disconnected";
      onCloseHandler?.();
    },
    triggerReconnect() {
      connection.state = "Connected";
      onReconnectedHandler?.();
    },
  };
});

vi.mock("@microsoft/signalr", () => {
  const build = {
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    configureLogging: vi.fn().mockReturnThis(),
    build: vi.fn(() => mocks.connection),
  };
  return {
    HubConnectionBuilder: vi.fn(() => build),
    HubConnectionState: { Connected: "Connected" },
    LogLevel: { Warning: 2 },
  };
});

const Probe = ({
  traderId,
  token,
  onRefreshSnapshot,
}: {
  traderId: string;
  token?: string;
  onRefreshSnapshot: () => void;
}) => {
  useTradingPetsRealtime({
    traderId,
    accessToken: token,
    onRefreshSnapshot,
  });
  return null;
};

const flushRealtimeSetup = async () => {
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
};

describe("useTradingPetsRealtime", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    mocks.reset();
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  it("subscribes to the trader, market, and leaderboard groups", async () => {
    render(
      <Probe
        traderId="33333333-3333-3333-3333-000000000001"
        token="test"
        onRefreshSnapshot={vi.fn()}
      />,
    );

    await flushRealtimeSetup();

    expect(mocks.connection.start).toHaveBeenCalledTimes(1);
    expect(mocks.connection.invoke).toHaveBeenCalledWith(
      "SubscribeTradingPetsTrader",
      "33333333-3333-3333-3333-000000000001",
    );
    expect(mocks.connection.invoke).toHaveBeenCalledWith(
      "SubscribeTradingPetsMarket",
    );
    expect(mocks.connection.invoke).toHaveBeenCalledWith(
      "SubscribeTradingPetsLeaderboard",
    );
  });

  it("refreshes immediately when a hub invalidation event arrives", async () => {
    const onRefreshSnapshot = vi.fn();
    render(
      <Probe
        traderId="33333333-3333-3333-3333-000000000001"
        token="test"
        onRefreshSnapshot={onRefreshSnapshot}
      />,
    );

    await flushRealtimeSetup();

    act(() => {
      mocks.triggerEvent("market.listingsUpdated");
      mocks.triggerEvent("trader.snapshotUpdated");
    });

    expect(onRefreshSnapshot).toHaveBeenCalledTimes(2);
  });

  it("starts polling after a short disconnect and refreshes immediately", async () => {
    const onRefreshSnapshot = vi.fn();
    render(
      <Probe
        traderId="33333333-3333-3333-3333-000000000001"
        token="test"
        onRefreshSnapshot={onRefreshSnapshot}
      />,
    );

    await flushRealtimeSetup();

    expect(mocks.connection.onclose).toHaveBeenCalledTimes(1);

    act(() => {
      mocks.triggerClose();
      vi.advanceTimersByTime(2999);
    });
    expect(onRefreshSnapshot).not.toHaveBeenCalled();

    act(() => {
      vi.advanceTimersByTime(1);
    });
    expect(onRefreshSnapshot).toHaveBeenCalledTimes(1);

    act(() => {
      vi.advanceTimersByTime(2000);
    });
    expect(onRefreshSnapshot).toHaveBeenCalledTimes(2);
  });

  it("stops fallback polling after the hub reconnects", async () => {
    const onRefreshSnapshot = vi.fn();
    render(
      <Probe
        traderId="33333333-3333-3333-3333-000000000001"
        token="test"
        onRefreshSnapshot={onRefreshSnapshot}
      />,
    );

    await flushRealtimeSetup();

    expect(mocks.connection.onreconnected).toHaveBeenCalledTimes(1);

    act(() => {
      mocks.triggerClose();
      vi.advanceTimersByTime(3000);
    });
    expect(onRefreshSnapshot).toHaveBeenCalledTimes(1);

    act(() => {
      mocks.triggerReconnect();
    });

    await flushRealtimeSetup();

    expect(mocks.connection.invoke).toHaveBeenCalledTimes(6);

    act(() => {
      vi.advanceTimersByTime(4000);
    });

    expect(onRefreshSnapshot).toHaveBeenCalledTimes(1);
  });
});

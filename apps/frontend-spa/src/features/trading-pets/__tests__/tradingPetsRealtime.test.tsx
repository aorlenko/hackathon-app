import { render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useTradingPetsRealtime } from "../useTradingPetsRealtime";

vi.mock("@microsoft/signalr", () => {
  const build = {
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    configureLogging: vi.fn().mockReturnThis(),
    build: vi.fn(() => ({
      state: "Disconnected",
      start: vi.fn(async () => {}),
      stop: vi.fn(async () => {}),
      on: vi.fn(),
      onreconnected: vi.fn(),
      onclose: vi.fn(),
      invoke: vi.fn(async () => {}),
    })),
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
}: {
  traderId: string;
  token?: string;
}) => {
  useTradingPetsRealtime({
    traderId,
    accessToken: token,
    onRefreshSnapshot: () => {},
  });
  return null;
};

describe("useTradingPetsRealtime", () => {
  it("initializes without throwing when token present", () => {
    expect(() =>
      render(<Probe traderId="33333333-3333-3333-3333-000000000001" token="test" />),
    ).not.toThrow();
  });
});

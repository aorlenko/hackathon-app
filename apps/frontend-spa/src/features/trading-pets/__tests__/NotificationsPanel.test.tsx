import { render, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { NotificationsPanel } from "../NotificationsPanel";
import * as api from "../tradingPetsApi";

vi.mock("../tradingPetsApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../tradingPetsApi")>();
  return {
    ...actual,
    getNotifications: vi.fn(),
  };
});

const getNotifications = vi.mocked(api.getNotifications);

describe("NotificationsPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("sorts notifications by createdAt descending, applies variant styling, and shows empty copy", async () => {
    getNotifications.mockResolvedValue([
      {
        id: "n1",
        type: "TradeCompleted",
        createdAt: "2026-01-02T12:00:00Z",
        petId: "p",
        petName: "Pet A",
        amount: 50,
        counterpartyTraderId: "t",
        counterpartyDisplayName: "Alex",
      },
      {
        id: "n2",
        type: "BidReceived",
        createdAt: "2026-01-03T08:00:00Z",
        petId: "p",
        petName: "Pet B",
        amount: 10,
        counterpartyTraderId: "t",
        counterpartyDisplayName: "Blake",
      },
    ]);

    const { unmount } = render(
      <NotificationsPanel traderId="tid" accessToken="tok" reloadToken={0} />,
    );

    await waitFor(() => expect(getNotifications).toHaveBeenCalled());

    const items = screen.getAllByRole("listitem");
    expect(items).toHaveLength(2);
    expect(items[0]).toHaveClass(/trading-pets-notifications__item--bid/);
    expect(within(items[0]).getByText(/Bid received/)).toBeInTheDocument();
    expect(items[1]).toHaveClass(/trading-pets-notifications__item--trade/);

    unmount();
    getNotifications.mockResolvedValue([]);
    render(<NotificationsPanel traderId="tid" accessToken="tok" reloadToken={1} />);
    await waitFor(() =>
      expect(
        screen.getByText(/No activity yet/i),
      ).toBeInTheDocument(),
    );
  });
});

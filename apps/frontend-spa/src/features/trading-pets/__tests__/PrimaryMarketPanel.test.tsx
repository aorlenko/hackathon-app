import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { PrimaryMarketPanel } from "../PrimaryMarketPanel";
import * as api from "../tradingPetsApi";

vi.mock("../tradingPetsApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../tradingPetsApi")>();
  return {
    ...actual,
    getBreeds: vi.fn(),
    purchasePets: vi.fn(),
  };
});

const getBreeds = vi.mocked(api.getBreeds);
const purchasePets = vi.mocked(api.purchasePets);

describe("PrimaryMarketPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getBreeds.mockResolvedValue([
      {
        id: "b1",
        name: "Aurora Cat",
        category: "c",
        lifespanYears: 10,
        baselineDesirability: 1,
        maintenanceCost: 1,
        retailPrice: 25.5,
        remainingSupply: 3,
      },
      {
        id: "b2",
        name: "Solar Hound",
        category: "working_dog",
        lifespanYears: 14,
        baselineDesirability: 7,
        maintenanceCost: 2.5,
        retailPrice: 40,
        remainingSupply: 5,
      },
    ]);
  });

  it("shows retail price, remaining supply, quantity, and primary purchase control", async () => {
    render(
      <PrimaryMarketPanel
        traderId="t1"
        accessToken="tok"
        reloadToken={0}
        onPurchased={vi.fn()}
      />,
    );

    await waitFor(() => expect(getBreeds).toHaveBeenCalled());

    const dl = document.getElementById("trading-primary-supply-hint");
    expect(dl).toBeTruthy();
    expect(within(dl as HTMLElement).getByText(/Category/i)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/^C$/)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/Expected lifespan/i)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/10 years/i)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/Desirability/i)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/^1$/)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/Maintenance cost/i)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/\$1\.00/)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/Retail price/i)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText("$25.50")).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/Remaining supply/i)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/^3$/)).toBeInTheDocument();
    expect(screen.getByRole("spinbutton", { name: /quantity/i })).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /buy from primary supply/i }),
    ).toBeInTheDocument();

    const qty = screen.getByRole("spinbutton", { name: /quantity/i });
    fireEvent.change(qty, { target: { value: "2" } });
    expect(screen.getByText(/\$51\.00/)).toBeInTheDocument();

    fireEvent.change(screen.getByRole("combobox", { name: /breed/i }), {
      target: { value: "b2" },
    });
    expect(within(dl as HTMLElement).getByText("Working Dog")).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/14 years/i)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/^7$/)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/\$2\.50/)).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText("$40.00")).toBeInTheDocument();
    expect(within(dl as HTMLElement).getByText(/^5$/)).toBeInTheDocument();
  });
});

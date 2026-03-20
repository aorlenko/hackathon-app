import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useResolvedAccountIdentities } from "../useResolvedAccountIdentities";

const mockResolveAccountIdentities = vi.fn();

vi.mock("../accountApi", () => ({
  resolveAccountIdentities: (...args: unknown[]) =>
    mockResolveAccountIdentities(...args),
}));

const Probe = ({ userIds }: { userIds: Array<string | null | undefined> }) => {
  const identities = useResolvedAccountIdentities(userIds, "token");
  return <div>{identities.size}</div>;
};

describe("useResolvedAccountIdentities", () => {
  it("filters out missing participant ids before resolving identities", async () => {
    mockResolveAccountIdentities.mockResolvedValue([
      {
        userId: "buyer-1",
        displayName: "Buyer One",
        email: "buyer1@example.com",
      },
    ]);

    render(
      <Probe
        userIds={[" buyer-1 ", undefined, null, "", "seller-1", "buyer-1"]}
      />,
    );

    await waitFor(() => {
      expect(mockResolveAccountIdentities).toHaveBeenCalledWith(
        ["buyer-1", "seller-1"],
        "token",
      );
    });

    expect(screen.getByText("1")).toBeInTheDocument();
  });
});

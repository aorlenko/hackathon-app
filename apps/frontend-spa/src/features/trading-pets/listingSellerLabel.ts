/** Auth0 / OIDC-style subject ids are not meaningful as a "name" in the UI. */
const looksLikeOpaqueSubject = (name: string) => name.includes("|");

function sellerListingLabel(sellerDisplayName: string, sellerEmail: string | null | undefined): string {
  const name = sellerDisplayName.trim();
  const email = (sellerEmail ?? "").trim();
  const humanName = name && !looksLikeOpaqueSubject(name) ? name : "";
  return humanName || email || name || "Seller";
}

/**
 * When the viewer is the seller, omit seller text — ownership is obvious from actions.
 * Otherwise return a short "Seller: …" clause: real display name when usable, else email, else raw name.
 */
export function listingSellerClause(
  sellerTraderId: string,
  sellerDisplayName: string,
  sellerEmail: string | null | undefined,
  viewerTraderId: string,
): string | null {
  // Only hide seller when we know the viewer is the lister (empty id = still loading).
  if (viewerTraderId && sellerTraderId === viewerTraderId) {
    return null;
  }
  return `Seller: ${sellerListingLabel(sellerDisplayName, sellerEmail)}`;
}

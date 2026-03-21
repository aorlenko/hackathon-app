function sellerListingLabel(sellerDisplayName: string, sellerEmail: string | null | undefined): string {
  const name = sellerDisplayName.trim();
  if (name.length > 0) {
    return name;
  }
  const email = (sellerEmail ?? "").trim();
  return email.length > 0 ? email : "Seller";
}

/**
 * When the viewer is the seller, omit seller text — ownership is obvious from actions.
 * Otherwise return a short "Seller: …" clause: display name, or email when the name is blank.
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

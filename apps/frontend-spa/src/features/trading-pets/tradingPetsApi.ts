import { env } from "../../config/env";
import { createHeaders, fetchJson } from "../../lib/http";

export type BreedDto = {
  id: string;
  name: string;
  category: string;
  lifespanYears: number;
  baselineDesirability: number;
  maintenanceCost: number;
  retailPrice: number;
  remainingSupply: number;
};

export type PetSummaryDto = {
  id: string;
  breedName: string;
  ageYears: number;
  health: number;
  currentDesirability: number;
  intrinsicValue: number;
  isExpired: boolean;
  maintenanceCost: number;
};

export type TraderSnapshotDto = {
  traderId: string;
  displayName: string;
  availableCash: number;
  lockedCash: number;
  portfolioTotal: number;
  pets: PetSummaryDto[];
  myBids: {
    bidId: string;
    listingId: string;
    petId: string;
    amount: number;
    status: string;
  }[];
};

export type MarketListingDto = {
  listingId: string;
  petId: string;
  sellerTraderId: string;
  breedName: string;
  askingPrice: number;
  sellerDisplayName: string;
  /** From linked demo account when the seller has ExternalUserId; use for display when display name is missing or opaque. */
  sellerEmail?: string | null;
  createdAt: string;
  recentTradePriceForBreed: number | null;
  remainingNewSupplyForBreed: number;
  /** Present only on your own listings: active below-ask bid the seller can accept or reject. */
  activeBidAmount?: number | null;
  activeBidBuyerDisplayName?: string | null;
};

export const getBreeds = (accessToken?: string) =>
  fetchJson<BreedDto[]>(`${env.marketApiBaseUrl}/api/pets/breeds`, {
    headers: createHeaders(accessToken),
    cache: "no-store",
  });

export const purchasePets = (
  body: { traderId: string; breedId: string; quantity: number },
  accessToken?: string,
) =>
  fetchJson<{ pets: PetSummaryDto[]; availableCash: number }>(
    `${env.marketApiBaseUrl}/api/pets/purchase`,
    {
      method: "POST",
      headers: createHeaders(accessToken),
      body: JSON.stringify(body),
    },
  );

export const getTraderSnapshot = (traderId: string, accessToken?: string) =>
  fetchJson<TraderSnapshotDto>(
    `${env.marketApiBaseUrl}/api/traders/${traderId}/snapshot`,
    { headers: createHeaders(accessToken) },
  );

/** Resolves (or creates) the pet trader linked to the signed-in Auth0 / OIDC user. */
export const getMyTraderSnapshot = (accessToken?: string) =>
  fetchJson<TraderSnapshotDto>(`${env.marketApiBaseUrl}/api/traders/me/snapshot`, {
    headers: createHeaders(accessToken),
  });

export const getMarketListings = (accessToken?: string) =>
  fetchJson<MarketListingDto[]>(`${env.marketApiBaseUrl}/api/market/listings`, {
    headers: createHeaders(accessToken),
    cache: "no-store",
  });

export const createListing = (
  body: { traderId: string; petId: string; askingPrice: number },
  accessToken?: string,
) =>
  fetchJson<{ listingId: string }>(`${env.marketApiBaseUrl}/api/market/listings`, {
    method: "POST",
    headers: createHeaders(accessToken),
    body: JSON.stringify(body),
  });

export const placeBid = (
  listingId: string,
  body: { traderId: string; amount: number },
  accessToken?: string,
) =>
  fetchJson<unknown>(
    `${env.marketApiBaseUrl}/api/market/listings/${listingId}/bids`,
    {
      method: "POST",
      headers: createHeaders(accessToken),
      body: JSON.stringify(body),
    },
  );

export const withdrawBid = (
  bidId: string,
  body: { traderId: string },
  accessToken?: string,
) =>
  fetchJson<void>(`${env.marketApiBaseUrl}/api/market/bids/${bidId}/withdraw`, {
    method: "POST",
    headers: createHeaders(accessToken),
    body: JSON.stringify(body),
  });

export const withdrawListing = (
  listingId: string,
  body: { traderId: string },
  accessToken?: string,
) =>
  fetchJson<void>(
    `${env.marketApiBaseUrl}/api/market/listings/${listingId}/withdraw`,
    {
      method: "POST",
      headers: createHeaders(accessToken),
      body: JSON.stringify(body),
    },
  );

export const acceptBid = (
  listingId: string,
  body: { traderId: string },
  accessToken?: string,
) =>
  fetchJson<unknown>(
    `${env.marketApiBaseUrl}/api/market/listings/${listingId}/accept`,
    {
      method: "POST",
      headers: createHeaders(accessToken),
      body: JSON.stringify(body),
    },
  );

export const rejectBid = (
  listingId: string,
  body: { traderId: string },
  accessToken?: string,
) =>
  fetchJson<void>(
    `${env.marketApiBaseUrl}/api/market/listings/${listingId}/reject`,
    {
      method: "POST",
      headers: createHeaders(accessToken),
      body: JSON.stringify(body),
    },
  );

export const getPetAnalysis = (
  petId: string,
  viewerTraderId: string | undefined,
  accessToken?: string,
) => {
  const qs =
    viewerTraderId && viewerTraderId.length > 0
      ? `?viewerTraderId=${encodeURIComponent(viewerTraderId)}`
      : "";
  return fetchJson<Record<string, unknown>>(
    `${env.marketApiBaseUrl}/api/pets/${petId}/analysis${qs}`,
    { headers: createHeaders(accessToken) },
  );
};

export type LeaderboardRowDto = {
  traderId: string;
  displayName: string;
  portfolioTotal: number;
  rank: number;
};

export const getLeaderboard = (accessToken?: string) =>
  fetchJson<LeaderboardRowDto[]>(
    `${env.marketApiBaseUrl}/api/traders/leaderboard`,
    { headers: createHeaders(accessToken), cache: "no-store" },
  );

export type NotificationDto = {
  id: string;
  type: string;
  createdAt: string;
  petId: string;
  petName: string;
  amount: number | null;
  counterpartyTraderId: string;
  counterpartyDisplayName: string;
};

export const getNotifications = (
  traderId: string,
  accessToken?: string,
  limit = 50,
) =>
  fetchJson<NotificationDto[]>(
    `${env.marketApiBaseUrl}/api/traders/${traderId}/notifications?limit=${limit}`,
    { headers: createHeaders(accessToken), cache: "no-store" },
  );

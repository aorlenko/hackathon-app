import { useEffect, useMemo, useState } from "react";
import type { AccountIdentity } from "../../contracts/trading";
import { resolveAccountIdentities } from "./accountApi";

const uniqueUserIds = (userIds: Array<string | null | undefined>) =>
  Array.from(
    new Set(
      userIds
        .filter((userId): userId is string => typeof userId === "string")
        .map((userId) => userId.trim())
        .filter(Boolean),
    ),
  ).sort((left, right) => left.localeCompare(right));

const toEmailSuffix = (userId: string) => {
  const identifier = userId.includes("|")
    ? userId.slice(userId.lastIndexOf("|") + 1)
    : userId;
  const normalized = identifier.replace(/[^a-zA-Z0-9]/g, "");

  if (!normalized) {
    return "user";
  }

  return normalized.length <= 8
    ? normalized.toLowerCase()
    : normalized.slice(0, 8).toLowerCase();
};

const toFallbackEmail = (userId: string) =>
  `trader-${toEmailSuffix(userId)}@demo.local`;

const labelFromIdentity = (identity: AccountIdentity | undefined): string | null => {
  if (!identity) {
    return null;
  }
  const displayName = identity.displayName?.trim();
  if (displayName) {
    return displayName;
  }
  const email = identity.email?.trim();
  if (email) {
    return email;
  }
  return null;
};

export const formatParticipantLabel = (
  userId: string | null | undefined,
  identities: Map<string, AccountIdentity>,
  options?: {
    currentUserId?: string | null;
    currentUserEmail?: string | null;
    currentUserDisplayName?: string | null;
    fallbackLabel?: string;
  },
) => {
  const normalizedUserId = typeof userId === "string" ? userId.trim() : "";
  if (!normalizedUserId) {
    return options?.fallbackLabel ?? "Unknown participant";
  }

  const identity = identities.get(normalizedUserId);

  if (options?.currentUserId && normalizedUserId === options.currentUserId) {
    const fromIdentity = labelFromIdentity(identity);
    if (fromIdentity) {
      return fromIdentity;
    }
    const selfName = options.currentUserDisplayName?.trim();
    if (selfName) {
      return selfName;
    }
    const selfEmail = options.currentUserEmail?.trim();
    if (selfEmail) {
      return selfEmail;
    }
  }

  const resolved = labelFromIdentity(identity);
  if (resolved) {
    return resolved;
  }

  return toFallbackEmail(normalizedUserId);
};

export const useResolvedAccountIdentities = (
  userIds: Array<string | null | undefined>,
  accessToken?: string,
) => {
  const [identities, setIdentities] = useState<Map<string, AccountIdentity>>(
    () => new Map(),
  );

  const userIdsKey = userIds.join("|");
  const normalizedUserIds = useMemo(() => uniqueUserIds(userIds), [userIdsKey]);
  const requestKey = normalizedUserIds.join("|");

  useEffect(() => {
    if (normalizedUserIds.length === 0) {
      setIdentities(new Map());
      return;
    }

    let active = true;

    void resolveAccountIdentities(normalizedUserIds, accessToken)
      .then((resolvedIdentities) => {
        if (!active) {
          return;
        }

        setIdentities(
          new Map(
            resolvedIdentities.map((identity) => [identity.userId, identity]),
          ),
        );
      })
      .catch(() => {
        if (active) {
          setIdentities(new Map());
        }
      });

    return () => {
      active = false;
    };
  }, [accessToken, requestKey]);

  return identities;
};

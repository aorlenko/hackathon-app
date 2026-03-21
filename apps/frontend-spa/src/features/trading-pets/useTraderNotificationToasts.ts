import { useCallback, useEffect, useRef, useState } from "react";
import { notificationLabel, notificationVariant } from "./notificationPresentation";
import type { PetTradingToast } from "./TradingPetToastStack";
import { getNotifications } from "./tradingPetsApi";

const TOAST_MS = 6500;

export function useTraderNotificationToasts(
  traderId: string,
  accessToken: string | undefined,
) {
  const [toasts, setToasts] = useState<PetTradingToast[]>([]);
  const seenNotificationIds = useRef(new Set<string>());
  const seedPromiseRef = useRef<Promise<void> | null>(null);
  const seedFailedRef = useRef(false);

  useEffect(() => {
    seenNotificationIds.current.clear();
    seedPromiseRef.current = null;
    seedFailedRef.current = false;
  }, [traderId, accessToken]);

  const ensureSeeded = useCallback(async () => {
    if (!traderId || !accessToken) {
      return;
    }
    if (!seedPromiseRef.current) {
      seedPromiseRef.current = getNotifications(traderId, accessToken)
        .then((rows) => {
          rows.forEach((r) => seenNotificationIds.current.add(r.id));
          seedFailedRef.current = false;
        })
        .catch(() => {
          seedFailedRef.current = true;
        });
    }
    await seedPromiseRef.current;
  }, [traderId, accessToken]);

  useEffect(() => {
    if (!traderId || !accessToken) {
      return;
    }
    void ensureSeeded();
  }, [traderId, accessToken, ensureSeeded]);

  const dismissToast = useCallback((toastInstanceId: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== toastInstanceId));
  }, []);

  const pushToast = useCallback((payload: Omit<PetTradingToast, "id"> & { dedupeKey: string }) => {
    const instanceId = `toast-${payload.dedupeKey}-${Date.now()}`;
    const { dedupeKey: _d, ...rest } = payload;
    setToasts((prev) => [...prev, { ...rest, id: instanceId }]);
    window.setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== instanceId));
    }, TOAST_MS);
  }, []);

  const onTraderNotificationsAdded = useCallback(async () => {
    if (!traderId || !accessToken) {
      return;
    }
    await ensureSeeded();
    if (seedFailedRef.current) {
      seedFailedRef.current = false;
      try {
        const rows = await getNotifications(traderId, accessToken);
        rows.forEach((r) => seenNotificationIds.current.add(r.id));
      } catch {
        seedFailedRef.current = true;
      }
      return;
    }
    let rows: Awaited<ReturnType<typeof getNotifications>>;
    try {
      rows = await getNotifications(traderId, accessToken);
    } catch {
      return;
    }
    const sorted = [...rows].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    );
    for (const r of sorted) {
      if (seenNotificationIds.current.has(r.id)) {
        continue;
      }
      seenNotificationIds.current.add(r.id);
      const title = notificationLabel(r.type);
      const bodyParts = [r.petName];
      if (typeof r.amount === "number") {
        bodyParts.push(`$${r.amount.toFixed(2)}`);
      }
      bodyParts.push(`with ${r.counterpartyDisplayName}`);
      pushToast({
        dedupeKey: r.id,
        title,
        body: bodyParts.join(" · "),
        variant: notificationVariant(r.type),
      });
    }
  }, [traderId, accessToken, ensureSeeded, pushToast]);

  return { toasts, dismissToast, onTraderNotificationsAdded };
}

import type { NotificationVisualVariant } from "./notificationPresentation";

export type PetTradingToast = {
  id: string;
  title: string;
  body: string;
  variant: NotificationVisualVariant;
};

type Props = {
  toasts: PetTradingToast[];
  onDismiss: (toastInstanceId: string) => void;
};

export const TradingPetToastStack = ({ toasts, onDismiss }: Props) => {
  if (toasts.length === 0) {
    return null;
  }

  return (
    <div
      className="trading-pets-toast-stack"
      aria-live="polite"
      aria-relevant="additions text"
    >
      {toasts.map((t) => (
        <div
          key={t.id}
          role="status"
          className={`trading-pets-toast trading-pets-toast--${t.variant}`}
        >
          <button
            type="button"
            className="trading-pets-toast__close"
            aria-label="Dismiss notification"
            onClick={() => onDismiss(t.id)}
          >
            ×
          </button>
          <div className="trading-pets-toast__title">{t.title}</div>
          <div className="trading-pets-toast__body">{t.body}</div>
        </div>
      ))}
    </div>
  );
};

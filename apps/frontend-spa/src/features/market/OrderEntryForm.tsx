import { useState, type FormEvent } from "react";
import type { OrderSide, PlaceOrderResponse } from "../../contracts/trading";

interface OrderEntryFormProps {
  symbol: string;
  disabled?: boolean;
  onSubmit: (input: {
    side: OrderSide;
    price: number;
    quantity: number;
  }) => Promise<PlaceOrderResponse>;
}

export const OrderEntryForm = ({
  symbol,
  disabled,
  onSubmit,
}: OrderEntryFormProps) => {
  const [side, setSide] = useState<OrderSide>("BUY");
  const [price, setPrice] = useState("100");
  const [quantity, setQuantity] = useState("1");
  const [feedback, setFeedback] = useState<string>("");
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitting(true);
    setFeedback("");

    try {
      const response = await onSubmit({
        side,
        price: Number(price),
        quantity: Number(quantity),
      });
      setFeedback(
        `Order ${response.orderId} accepted with ${response.remainingQuantity} remaining.`,
      );
    } catch (error) {
      setFeedback(
        error instanceof Error ? error.message : "Order could not be submitted.",
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section className="card">
      <div className="section-header">
        <h2>Order entry</h2>
        <span className="tag">{symbol}</span>
      </div>
      <form className="order-form" onSubmit={handleSubmit}>
        <label>
          Side
          <select
            value={side}
            disabled={disabled || submitting}
            onChange={(event) => setSide(event.target.value as OrderSide)}
          >
            <option value="BUY">Buy</option>
            <option value="SELL">Sell</option>
          </select>
        </label>
        <label>
          Price
          <input
            type="number"
            min="0"
            step="0.01"
            value={price}
            disabled={disabled || submitting}
            onChange={(event) => setPrice(event.target.value)}
          />
        </label>
        <label>
          Quantity
          <input
            type="number"
            min="1"
            step="1"
            value={quantity}
            disabled={disabled || submitting}
            onChange={(event) => setQuantity(event.target.value)}
          />
        </label>
        <button className="primary-button" disabled={disabled || submitting}>
          {submitting ? "Submitting..." : "Submit order"}
        </button>
      </form>
      {feedback ? <p className="inline-feedback">{feedback}</p> : null}
    </section>
  );
};

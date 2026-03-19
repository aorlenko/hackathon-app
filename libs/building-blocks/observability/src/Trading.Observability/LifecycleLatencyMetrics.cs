using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Trading.Observability;

public sealed class LifecycleLatencyMetrics
{
    private readonly Histogram<double> _orderToTradeMilliseconds;
    private readonly Histogram<double> _tradeToSettlementMilliseconds;
    private readonly Counter<long> _degradedStateCounter;

    public LifecycleLatencyMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Trading.Observability");
        _orderToTradeMilliseconds = meter.CreateHistogram<double>("trading.order_to_trade_ms");
        _tradeToSettlementMilliseconds = meter.CreateHistogram<double>("trading.trade_to_settlement_ms");
        _degradedStateCounter = meter.CreateCounter<long>("trading.degraded_state_count");
    }

    public void RecordOrderToTrade(TimeSpan latency) => _orderToTradeMilliseconds.Record(latency.TotalMilliseconds);

    public void RecordTradeToSettlement(TimeSpan latency) => _tradeToSettlementMilliseconds.Record(latency.TotalMilliseconds);

    public void RecordDegradedState(string reason) => _degradedStateCounter.Add(1, new KeyValuePair<string, object?>("reason", reason));
}

public static class ActivitySources
{
    public static readonly ActivitySource Market = new("Trading.MarketService");
    public static readonly ActivitySource Trade = new("Trading.TradeService");
    public static readonly ActivitySource Settlement = new("Trading.SettlementService");
}

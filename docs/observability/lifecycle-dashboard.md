# Lifecycle Dashboard

This environment uses workspace-based Application Insights and Log Analytics from `infra/bicep/modules/monitoring.bicep`.

## Dashboard Focus

- End-to-end latency for `OrderPlaced -> OrderMatched -> TradeRecorded -> SettlementCompleted`
- Failed request rate across `market-service`, `trade-service`, and `settlement-service`
- Live service health for the frontend and API apps deployed to Azure Container Apps

## Recommended Workbook Sections

### 1. Request Health

Use this query to track success/failure volume by service:

```kusto
requests
| where timestamp > ago(30m)
| where cloud_RoleName in~ ("market-service", "trade-service", "settlement-service")
| summarize total = count(), failures = countif(success == false) by cloud_RoleName, bin(timestamp, 5m)
| order by timestamp desc
```

### 2. Lifecycle Event Latency

Assumes services emit custom dimensions for `eventType` and `correlationId`:

```kusto
traces
| where timestamp > ago(30m)
| where customDimensions.eventType in ("OrderPlaced", "OrderMatched", "TradeRecorded", "SettlementCompleted")
| project timestamp, correlationId = tostring(customDimensions.correlationId), eventType = tostring(customDimensions.eventType)
| summarize
    orderPlacedAt = minif(timestamp, eventType == "OrderPlaced"),
    tradeRecordedAt = minif(timestamp, eventType == "TradeRecorded"),
    settlementCompletedAt = minif(timestamp, eventType == "SettlementCompleted")
  by correlationId
| extend orderToTradeMs = datetime_diff("millisecond", tradeRecordedAt, orderPlacedAt)
| extend orderToSettlementMs = datetime_diff("millisecond", settlementCompletedAt, orderPlacedAt)
| project correlationId, orderToTradeMs, orderToSettlementMs
| order by orderToSettlementMs desc
```

### 3. Container App Availability

```kusto
ContainerAppSystemLogs_CL
| where TimeGenerated > ago(30m)
| summarize restarts = countif(Log_s contains "Restarting"), revisions = dcount(RevisionName_s) by ContainerAppName_s
| order by restarts desc
```

## Alerting

The Bicep monitoring module provisions **Log Analytics + Application Insights only** (no scheduled alert rules or action groups). Add Azure Monitor alerts or workbooks manually if you need notifications.

## Validation Steps

1. Deploy `infra/bicep/main.bicep`.
2. Open the generated Application Insights instance.
3. Create or import a workbook using the queries above.
4. During demo rehearsals, capture screenshots of the request health and latency sections.

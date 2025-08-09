using System;
using System.Collections.Generic;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// Account balance data structure (계정 잔고)
    /// </summary>
    public class SBalance
    {
        /// <summary>
        /// Exchange name
        /// </summary>
        public string exchange { get; set; }

        /// <summary>
        /// Account ID (if multiple accounts)
        /// </summary>
        public string accountId { get; set; }

        /// <summary>
        /// Update timestamp
        /// </summary>
        public long timestamp { get; set; }

        /// <summary>
        /// List of balance items
        /// </summary>
        public List<SBalanceItem> balances { get; set; }

        /// <summary>
        /// Total value in USD/USDT
        /// </summary>
        public decimal totalValueUSD { get; set; }
    }

    /// <summary>
    /// Individual balance item
    /// </summary>
    public class SBalanceItem
    {
        /// <summary>
        /// Currency symbol (BTC, ETH, USDT, etc.)
        /// </summary>
        public string currency { get; set; }

        /// <summary>
        /// Free/available balance
        /// </summary>
        public decimal free { get; set; }

        /// <summary>
        /// Used/locked balance (in orders)
        /// </summary>
        public decimal used { get; set; }

        /// <summary>
        /// Total balance (free + used)
        /// </summary>
        public decimal total { get; set; }

        /// <summary>
        /// Value in USD/USDT
        /// </summary>
        public decimal valueUSD { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public long updateTime { get; set; }
    }

    /// <summary>
    /// Order data structure (주문 정보)
    /// </summary>
    public class SOrderItem
    {
        /// <summary>
        /// Order ID
        /// </summary>
        public string orderId { get; set; }

        /// <summary>
        /// Client order ID (user defined)
        /// </summary>
        public string clientOrderId { get; set; }

        /// <summary>
        /// Trading symbol
        /// </summary>
        public string symbol { get; set; }

        /// <summary>
        /// Order type (limit, market, stop, etc.)
        /// </summary>
        public OrderType type { get; set; }

        /// <summary>
        /// Order side (buy/sell)
        /// </summary>
        public OrderSide side { get; set; }

        /// <summary>
        /// Order status
        /// </summary>
        public OrderStatus status { get; set; }

        /// <summary>
        /// Order price (for limit orders)
        /// </summary>
        public decimal price { get; set; }

        /// <summary>
        /// Stop price (for stop orders)
        /// </summary>
        public decimal? stopPrice { get; set; }

        /// <summary>
        /// Order quantity
        /// </summary>
        public decimal quantity { get; set; }

        /// <summary>
        /// Filled quantity
        /// </summary>
        public decimal filledQuantity { get; set; }

        /// <summary>
        /// Remaining quantity
        /// </summary>
        public decimal remainingQuantity { get; set; }

        /// <summary>
        /// Average fill price
        /// </summary>
        public decimal avgFillPrice { get; set; }

        /// <summary>
        /// Total cost (filled quantity * avg price)
        /// </summary>
        public decimal cost { get; set; }

        /// <summary>
        /// Fee amount
        /// </summary>
        public decimal fee { get; set; }

        /// <summary>
        /// Fee currency
        /// </summary>
        public string feeCurrency { get; set; }

        /// <summary>
        /// Order creation time
        /// </summary>
        public long createTime { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public long updateTime { get; set; }

        /// <summary>
        /// Time in force (GTC, IOC, FOK, etc.)
        /// </summary>
        public string timeInForce { get; set; }

        /// <summary>
        /// Is post-only order
        /// </summary>
        public bool postOnly { get; set; }

        /// <summary>
        /// Is reduce-only order (futures)
        /// </summary>
        public bool reduceOnly { get; set; }

        /// <summary>
        /// Trades/fills for this order
        /// </summary>
        public List<SOrderFill> fills { get; set; }
    }

    /// <summary>
    /// Order fill/trade data
    /// </summary>
    public class SOrderFill
    {
        /// <summary>
        /// Trade ID
        /// </summary>
        public string tradeId { get; set; }

        /// <summary>
        /// Fill price
        /// </summary>
        public decimal price { get; set; }

        /// <summary>
        /// Fill quantity
        /// </summary>
        public decimal quantity { get; set; }

        /// <summary>
        /// Fee amount
        /// </summary>
        public decimal fee { get; set; }

        /// <summary>
        /// Fee currency
        /// </summary>
        public string feeCurrency { get; set; }

        /// <summary>
        /// Fill timestamp
        /// </summary>
        public long timestamp { get; set; }

        /// <summary>
        /// Is maker (provided liquidity)
        /// </summary>
        public bool isMaker { get; set; }
    }

    /// <summary>
    /// Position data structure (포지션 - for futures/derivatives)
    /// </summary>
    public class SPositionItem
    {
        /// <summary>
        /// Position ID
        /// </summary>
        public string positionId { get; set; }

        /// <summary>
        /// Trading symbol
        /// </summary>
        public string symbol { get; set; }

        /// <summary>
        /// Position side (long/short)
        /// </summary>
        public PositionSide side { get; set; }

        /// <summary>
        /// Position mode (hedge/one-way)
        /// </summary>
        public string mode { get; set; }

        /// <summary>
        /// Position size (contracts or base currency)
        /// </summary>
        public decimal size { get; set; }

        /// <summary>
        /// Notional value
        /// </summary>
        public decimal notional { get; set; }

        /// <summary>
        /// Average entry price
        /// </summary>
        public decimal entryPrice { get; set; }

        /// <summary>
        /// Mark price
        /// </summary>
        public decimal markPrice { get; set; }

        /// <summary>
        /// Liquidation price
        /// </summary>
        public decimal? liquidationPrice { get; set; }

        /// <summary>
        /// Unrealized PnL
        /// </summary>
        public decimal unrealizedPnl { get; set; }

        /// <summary>
        /// Realized PnL
        /// </summary>
        public decimal realizedPnl { get; set; }

        /// <summary>
        /// PnL percentage
        /// </summary>
        public decimal pnlPercentage { get; set; }

        /// <summary>
        /// Initial margin
        /// </summary>
        public decimal initialMargin { get; set; }

        /// <summary>
        /// Maintenance margin
        /// </summary>
        public decimal maintenanceMargin { get; set; }

        /// <summary>
        /// Margin ratio
        /// </summary>
        public decimal marginRatio { get; set; }

        /// <summary>
        /// Leverage
        /// </summary>
        public decimal leverage { get; set; }

        /// <summary>
        /// Position creation time
        /// </summary>
        public long createTime { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public long updateTime { get; set; }

        /// <summary>
        /// Is isolated margin
        /// </summary>
        public bool isIsolated { get; set; }

        /// <summary>
        /// Is auto add margin enabled
        /// </summary>
        public bool isAutoAddMargin { get; set; }
    }

    /// <summary>
    /// Order type enumeration
    /// </summary>
    public enum OrderType
    {
        Market,
        Limit,
        Stop,
        StopLimit,
        TakeProfit,
        TakeProfitLimit,
        TrailingStop
    }

    /// <summary>
    /// Order side enumeration
    /// </summary>
    public enum OrderSide
    {
        Buy,
        Sell
    }

    /// <summary>
    /// Order status enumeration
    /// </summary>
    public enum OrderStatus
    {
        New,            // New order, not yet in orderbook
        Open,           // Open order in orderbook
        PartiallyFilled,// Partially filled
        Filled,         // Completely filled
        Canceled,       // Canceled by user
        Rejected,       // Rejected by exchange
        Expired         // Expired (time in force)
    }

    /// <summary>
    /// Position side enumeration
    /// </summary>
    public enum PositionSide
    {
        Long,
        Short,
        Both    // For one-way mode
    }

    /// <summary>
    /// Account event type
    /// </summary>
    public enum AccountEventType
    {
        BalanceUpdate,
        OrderUpdate,
        OrderNew,
        OrderCanceled,
        OrderFilled,
        OrderPartiallyFilled,
        PositionUpdate,
        PositionClosed,
        MarginCall,
        Liquidation
    }

    /// <summary>
    /// Account event wrapper
    /// </summary>
    public class SAccountEvent
    {
        /// <summary>
        /// Event type
        /// </summary>
        public AccountEventType eventType { get; set; }

        /// <summary>
        /// Event timestamp
        /// </summary>
        public long timestamp { get; set; }

        /// <summary>
        /// Exchange name
        /// </summary>
        public string exchange { get; set; }

        /// <summary>
        /// Event data (can be SBalance, SOrder, or SPosition)
        /// </summary>
        public object data { get; set; }

        /// <summary>
        /// Event message/description
        /// </summary>
        public string message { get; set; }
    }
}
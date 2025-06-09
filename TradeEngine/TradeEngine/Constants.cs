namespace TradeEngine
{
    public class Constants
    {
        public static readonly string TradeStatusOpen = "OPEN";
        public static readonly string TradeStatusClosed = "CLOSED";
        public static readonly string TradeStatusSLHit = "STOP_LOSS_HIT";
        public static readonly string TradeStatusTargetHit = "TARGET_HIT";

        
        public static readonly string TradeSideBuy = "BUY";
        public static readonly string TradeSideSell = "SELL";


        public static readonly string DataBaseName = "TradeExecution.db";
        public static readonly string TradeTableName = "Trade";
        public static readonly string SignalTableName = "Signal";
        public static readonly string UserTableName = "User";
    }
}

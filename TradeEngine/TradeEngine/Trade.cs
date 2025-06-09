using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeEngine
{
    /// <summary>
    /// Represents a trade with details such as symbol, side, prices, status, and timestamp.
    /// </summary>
    public class Trade
    {
        public int ID { get; set; }
        public int SignalId { get; set; }
        public string Symbol { get; set; }
        public string Side { get; set; }
        public double Entry_Price { get; set; }
        public double Stop_Loss { get; set; }
        public double Target { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"{Timestamp} :: {Symbol}({Side}): Trade_Status: {Status}, EN_Price: {Entry_Price}, SL_Price: {Stop_Loss}, Target_Price: {Target}";
        }
    }
}

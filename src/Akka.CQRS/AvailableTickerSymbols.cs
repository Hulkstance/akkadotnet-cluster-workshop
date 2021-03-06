using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS
{
    /// <summary>
    /// The set of available ticker symbols for this demo.
    /// </summary>
    public static class AvailableTickerSymbols
    {
        public static readonly string[] Symbols = 
            { "MSFT", "AMZN", "GOOG", "TSLA", "TEAM", "AMD", "WDC", "STX", "UBER", "SNAP", "FB", "NET", "DT", "ESTC",
        "FSLY", "UPWK", "INTC", "HPE", "BB", "QCOM", "APPL", "DDOG", "NEWR", "RACE", "SAVE", "AAL", "UAL", "DAL"};
    }
}

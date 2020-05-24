namespace CCXT.Collector.Deribit.Model
{
    /*
    {
        "jsonrpc": "2.0",
        "method": "subscription",
        "params": {
            "channel": "book.BTC-PERPETUAL.raw",
            "data": {
                "timestamp": 1589371213152,
                "prev_change_id": 3973277205,
                "instrument_name": "BTC-PERPETUAL",
                "change_id": 3973277206,
                "bids": [
                    [
                        "change",
                        9030,
                        30
                    ],
                    [
                        "new",
                        9034.5,
                        9030
                    ]   
                ],
                "asks": []
            }
        }
    }
    */

    /// <summary>
    ///
    /// </summary>
    public class DWsOrderBook 
    {
        public string type
        {
            get; set;
        }

        public long timestamp
        {
            get; set;
        }

        public long prev_change_id
        {
            get; set;
        }

        public string instrument_name
        {
            get; set;
        }

        public long change_id
        {
            get; set;
        }

        public object[][] bids
        {
            get; set;
        }

        public object[][] asks
        {
            get; set;
        }
    }
}
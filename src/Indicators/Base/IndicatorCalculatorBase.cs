using CCXT.Collector.Service;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class IndicatorCalculatorBase<T>
    {
        /// <summary>
        ///
        /// </summary>
        protected abstract List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        public virtual void Load(string path)
        {
            //using (CsvReader csv = new CsvReader(new StreamReader(path), true))
            //{
            //    int fieldCount = csv.FieldCount;
            //    string[] headers = csv.GetFieldHeaders();
            //    OhlcList = new List<Ohlc>();
            //    while (csv.ReadNextRecord())
            //    {
            //        Ohlc ohlc = new Ohlc();
            //        for (var i = 0; i < fieldCount; i++)
            //        {
            //            switch (headers[i])
            //            {
            //                case "Date":
            //                    ohlc.datetime = new DateTime(Int32.Parse(csv[i].Substring(0, 4)), Int32.Parse(csv[i].Substring(5, 2)), Int32.Parse(csv[i].Substring(8, 2)));
            //                    break;
            //                case "Open":
            //                    ohlc.openPrice = double.Parse(csv[i], CultureInfo.InvariantCulture);
            //                    break;
            //                case "High":
            //                    ohlc.highPrice = double.Parse(csv[i], CultureInfo.InvariantCulture);
            //                    break;
            //                case "Low":
            //                    ohlc.lowPrice = double.Parse(csv[i], CultureInfo.InvariantCulture);
            //                    break;
            //                case "Close":
            //                    ohlc.closePrice = double.Parse(csv[i], CultureInfo.InvariantCulture);
            //                    break;
            //                case "Volume":
            //                    ohlc.volume = int.Parse(csv[i]);
            //                    break;
            //                case "Adj Close":
            //                    ohlc.adjClose = double.Parse(csv[i], CultureInfo.InvariantCulture);
            //                    break;
            //                default:
            //                    break;
            //            }
            //        }

            //        OhlcList.Add(ohlc);
            //    }
            //}
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ohlcList"></param>
        public virtual void Load(List<SOhlcvItem> ohlcList)
        {
            this.OhlcList = ohlcList;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public abstract T Calculate();
    }
}
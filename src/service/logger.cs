using System;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void LogEventHandler(object sender, CCEventArgs e);

    /// <summary>
    ///
    /// </summary>

    public class CCEventArgs : EventArgs
    {
        /// <summary>
        ///
        /// </summary>
        public string command
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string exchange
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string message
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CCLogger
    {
        public static event LogEventHandler LogEvent;

        public readonly string exchange_name;

        public CCLogger(string exchange)
        {
            this.exchange_name = exchange;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public void WriteQ(object sender, string message)
        {
            if (LogEvent != null)
            {
                LogEvent(sender, new CCEventArgs
                {
                    command = "WQ",
                    exchange = exchange_name,
                    message = message
                });
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public void WriteO(object sender, string message)
        {
            if (LogEvent != null)
            {
                LogEvent(sender, new CCEventArgs
                {
                    command = "WO",
                    exchange = exchange_name,
                    message = message
                });
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public void WriteX(object sender, string message)
        {
            if (LogEvent != null)
            {
                LogEvent(sender, new CCEventArgs
                {
                    command = "WX",
                    exchange = exchange_name,
                    message = message
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void WriteC(object sender, string message)
        {
            if (LogEvent != null)
            {
                LogEvent(sender, new CCEventArgs
                {
                    command = "WC",
                    exchange = exchange_name,
                    message = message
                });
            }
        }
    }
}
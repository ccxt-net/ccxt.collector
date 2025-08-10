using System;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// Represents a trading market/pair
    /// </summary>
    public struct Market : IEquatable<Market>
    {
        /// <summary>
        /// Base currency (e.g., BTC, ETH)
        /// </summary>
        public string Base { get; }

        /// <summary>
        /// Quote currency (e.g., KRW, USDT)
        /// </summary>
        public string Quote { get; }

        /// <summary>
        /// Creates a new Market instance
        /// </summary>
        /// <param name="baseCurrency">Base currency</param>
        /// <param name="quoteCurrency">Quote currency</param>
        public Market(string baseCurrency, string quoteCurrency)
        {
            Base = baseCurrency ?? throw new ArgumentNullException(nameof(baseCurrency));
            Quote = quoteCurrency ?? throw new ArgumentNullException(nameof(quoteCurrency));
        }

        /// <summary>
        /// Returns the standard format: BASE/QUOTE
        /// </summary>
        public override string ToString()
        {
            return $"{Base}/{Quote}";
        }

        /// <summary>
        /// Parses a symbol string into a Market
        /// </summary>
        /// <param name="symbol">Symbol in format "BASE/QUOTE"</param>
        /// <returns>Market instance</returns>
        public static Market Parse(string symbol)
        {
            if (String.IsNullOrEmpty(symbol))
                throw new ArgumentNullException(nameof(symbol));

            var parts = symbol.Split('/');
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid symbol format: {symbol}. Expected format: BASE/QUOTE");

            return new Market(parts[0], parts[1]);
        }

        /// <summary>
        /// Tries to parse a symbol string into a Market
        /// </summary>
        public static bool TryParse(string symbol, out Market market)
        {
            market = default;
            
            if (String.IsNullOrEmpty(symbol))
                return false;

            var parts = symbol.Split('/');
            if (parts.Length != 2)
                return false;

            market = new Market(parts[0], parts[1]);
            return true;
        }

        public bool Equals(Market other)
        {
            return Base == other.Base && Quote == other.Quote;
        }

        public override bool Equals(object obj)
        {
            return obj is Market other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Base, Quote);
        }

        public static bool operator ==(Market left, Market right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Market left, Market right)
        {
            return !left.Equals(right);
        }
    }
}
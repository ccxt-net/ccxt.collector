using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace CCXT.Collector.Library
{
    /// <summary>
    /// Extension methods for collections and LINQ operations
    /// </summary>
    public static partial class LinqExtension
    {
        #region Thread-Safe Collections

        /// <summary>
        /// Adds multiple items to a ConcurrentBag
        /// </summary>
        public static ConcurrentBag<T> AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> items)
        {
            if (bag == null) throw new ArgumentNullException(nameof(bag));
            if (items == null) throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
                bag.Add(item);

            return bag;
        }

        /// <summary>
        /// Adds or updates an item in a ConcurrentDictionary
        /// </summary>
        public static bool AddOrUpdate<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            dictionary.AddOrUpdate(key, value, (k, v) => value);
            return true;
        }

        /// <summary>
        /// Removes all items matching a predicate from a list (thread-safe)
        /// </summary>
        public static int RemoveAll<T>(this List<T> list, Predicate<T> match)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (match == null) throw new ArgumentNullException(nameof(match));

            // Use built-in RemoveAll which is optimized
            return list.RemoveAll(match);
        }

        /// <summary>
        /// Thread-safe remove all with locking
        /// </summary>
        public static int RemoveAllThreadSafe<T>(this List<T> list, Predicate<T> match, object syncRoot)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (syncRoot == null) throw new ArgumentNullException(nameof(syncRoot));

            lock (syncRoot)
            {
                // More efficient: create new list without matched items
                var itemsToKeep = new List<T>(list.Count);
                int removedCount = 0;

                foreach (var item in list)
                {
                    if (match(item))
                        removedCount++;
                    else
                        itemsToKeep.Add(item);
                }

                list.Clear();
                list.AddRange(itemsToKeep);

                return removedCount;
            }
        }

        #endregion

        #region Immutable Collections

        /// <summary>
        /// Updates or inserts an item in an ImmutableList
        /// </summary>
        public static ImmutableList<T> UpdateOrInsert<T>(
            this ImmutableList<T> list,
            T item,
            Func<T, bool> match)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (match == null) throw new ArgumentNullException(nameof(match));

            var index = list.FindIndex(new Predicate<T>(match));
            if (index >= 0)
            {
                return list.SetItem(index, item);
            }
            else
            {
                return list.Add(item);
            }
        }

        /// <summary>
        /// Efficiently removes all matching items from an ImmutableList
        /// </summary>
        public static ImmutableList<T> RemoveAll<T>(
            this ImmutableList<T> list,
            Func<T, bool> match)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (match == null) throw new ArgumentNullException(nameof(match));

            var builder = list.ToBuilder();
            // Wrap Func<T,bool> into Predicate<T> to satisfy builder.RemoveAll signature explicitly.
            builder.RemoveAll(new Predicate<T>(match));
            return builder.ToImmutable();
        }

        #endregion

        #region Dictionary-based Operations

        /// <summary>
        /// Updates or inserts an item in a dictionary with O(1) lookup
        /// </summary>
        public static bool UpdateOrInsert<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            bool existed = dictionary.ContainsKey(key);
            dictionary[key] = value;
            return existed;
        }

        /// <summary>
        /// Thread-safe update or insert with custom comparison
        /// </summary>
        public static bool UpdateOrInsertThreadSafe<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value,
            object syncRoot)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (syncRoot == null) throw new ArgumentNullException(nameof(syncRoot));

            lock (syncRoot)
            {
                bool existed = dictionary.ContainsKey(key);
                dictionary[key] = value;
                return existed;
            }
        }

        #endregion

        #region Random String Generation

        // Thread-safe random number generator
        private static readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();
        // Fast non-crypto random per-thread instance for netstandard2.0 compatibility (no Random.Shared)
        private static readonly ThreadLocal<Random> FastRandom = new ThreadLocal<Random>(() =>
            new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));
        
        // Character sets for random string generation
        private const string AlphanumericChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string AlphabeticChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string NumericChars = "0123456789";
        private const string HexChars = "0123456789ABCDEF";

        /// <summary>
        /// Generates a cryptographically secure random string
        /// </summary>
        /// <param name="length">Length of the string to generate</param>
        /// <param name="charSet">Character set to use (defaults to alphanumeric)</param>
        /// <returns>Random string of specified length</returns>
        public static string GenerateRandomString(int length, string charSet = AlphanumericChars)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive");
            if (string.IsNullOrEmpty(charSet)) throw new ArgumentException("Character set cannot be empty", nameof(charSet));

            var result = new StringBuilder(length);
            var buffer = new byte[length * 4]; // Overprovision to reduce calls
            CryptoRandom.GetBytes(buffer);

            for (int i = 0, j = 0; i < length; i++, j += 4)
            {
                // Use 4 bytes to get better distribution
                uint randomValue = BitConverter.ToUInt32(buffer, j);
                result.Append(charSet[(int)(randomValue % (uint)charSet.Length)]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Generates a random alphanumeric string (non-cryptographic, faster)
        /// </summary>
        /// <param name="length">Length of the string to generate</param>
        /// <returns>Random alphanumeric string</returns>
        public static string GenerateRandomStringFast(int length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive");

            var result = new StringBuilder(length);
            var random = FastRandom.Value; // Thread-local random for thread safety

            for (int i = 0; i < length; i++)
            {
                result.Append(AlphanumericChars[random.Next(AlphanumericChars.Length)]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Generates a random hexadecimal string
        /// </summary>
        /// <param name="length">Length of the string to generate</param>
        /// <returns>Random hex string</returns>
        public static string GenerateRandomHexString(int length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive");

            var bytes = new byte[(length + 1) / 2];
            CryptoRandom.GetBytes(bytes);
            
            var hex = BitConverter.ToString(bytes).Replace("-", "");
            return hex.Substring(0, length);
        }

        #endregion

        #region Additional Utility Methods

        /// <summary>
        /// Safely gets a value from a dictionary or returns default
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Batches an enumerable into chunks of specified size
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

            return BatchIterator(source, batchSize);
        }

        private static IEnumerable<IEnumerable<T>> BatchIterator<T>(IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            
            foreach (var item in source)
            {
                batch.Add(item);
                
                if (batch.Count == batchSize)
                {
                    yield return batch.ToArray(); // Return a copy to avoid mutation
                    batch.Clear();
                }
            }
            
            // Return any remaining items as the last batch
            if (batch.Count > 0)
            {
                yield return batch.ToArray();
            }
        }

        #endregion
    }
}
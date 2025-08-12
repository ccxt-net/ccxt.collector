using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using CCXT.Collector.Library;

namespace CCXT.Collector.Tests.Utilities
{
    public class LinqExtensionTests
    {
        #region ConcurrentBag Tests

        [Fact]
        public void ConcurrentBag_AddRange_AddsAllItems()
        {
            var bag = new ConcurrentBag<int>();
            var items = new[] { 1, 2, 3, 4, 5 };

            bag.AddRange(items);

            Assert.Equal(5, bag.Count);
            Assert.All(items, item => Assert.Contains(item, bag));
        }

        [Fact]
        public async Task ConcurrentBag_ThreadSafe()
        {
            var bag = new ConcurrentBag<int>();
            var tasks = new List<Task>();

            // Add items from multiple threads
            for (int i = 0; i < 10; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(() =>
                {
                    bag.AddRange(Enumerable.Range(threadId * 100, 10));
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(100, bag.Count);
        }

        #endregion

        #region Dictionary Operations Tests

        [Fact]
        public void Dictionary_UpdateOrInsert_UpdatesExisting()
        {
            var dict = new Dictionary<string, int> { ["key1"] = 1 };

            bool existed = dict.UpdateOrInsert("key1", 2);

            Assert.True(existed);
            Assert.Equal(2, dict["key1"]);
        }

        [Fact]
        public void Dictionary_UpdateOrInsert_InsertsNew()
        {
            var dict = new Dictionary<string, int>();

            bool existed = dict.UpdateOrInsert("key1", 1);

            Assert.False(existed);
            Assert.Equal(1, dict["key1"]);
        }

        [Fact]
        public async Task Dictionary_ThreadSafe_UpdateOrInsert()
        {
            var dict = new Dictionary<int, int>();
            var syncRoot = new object();
            var tasks = new List<Task>();

            // Multiple threads updating same keys
            for (int i = 0; i < 100; i++)
            {
                int value = i;
                tasks.Add(Task.Run(() =>
                {
                    dict.UpdateOrInsertThreadSafe(value % 10, value, syncRoot);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(10, dict.Count);
        }

        #endregion

        #region ImmutableList Tests

        [Fact]
        public void ImmutableList_UpdateOrInsert_Updates()
        {
            var list = ImmutableList.Create(1, 2, 3);

            var newList = list.UpdateOrInsert(99, x => x == 2);

            Assert.Equal(3, newList.Count);
            Assert.Equal(99, newList[1]);
            Assert.NotSame(list, newList); // Immutability check
        }

        [Fact]
        public void ImmutableList_UpdateOrInsert_Inserts()
        {
            var list = ImmutableList.Create(1, 2, 3);

            var newList = list.UpdateOrInsert(4, x => x == 99);

            Assert.Equal(4, newList.Count);
            Assert.Equal(4, newList[3]);
        }

        [Fact]
        public void ImmutableList_RemoveAll_RemovesMatching()
        {
            var list = ImmutableList.Create(1, 2, 3, 4, 5);

            var newList = list.RemoveAll(x => x % 2 == 0);

            Assert.Equal(3, newList.Count);
            Assert.Equal(new[] { 1, 3, 5 }, newList);
        }

        #endregion

        #region RemoveAll Performance Tests

        [Fact]
        public void RemoveAllThreadSafe_PerformanceImprovement()
        {
            var list = new List<int>(Enumerable.Range(1, 1000));
            var syncRoot = new object();

            // Remove all even numbers
            int removed = list.RemoveAllThreadSafe(x => x % 2 == 0, syncRoot);

            Assert.Equal(500, removed);
            Assert.Equal(500, list.Count);
            Assert.All(list, x => Assert.True(x % 2 == 1));
        }

        #endregion

        #region Random String Generation Tests

        [Fact]
        public void GenerateRandomString_Cryptographic_GeneratesCorrectLength()
        {
            var result = LinqExtension.GenerateRandomString(20);

            Assert.Equal(20, result.Length);
            Assert.Matches("^[a-zA-Z0-9]+$", result);
        }

        [Fact]
        public void GenerateRandomString_CustomCharSet()
        {
            var result = LinqExtension.GenerateRandomString(10, "ABC123");

            Assert.Equal(10, result.Length);
            Assert.Matches("^[ABC123]+$", result);
        }

        [Fact]
        public void GenerateRandomStringFast_Performance()
        {
            var start = DateTime.Now;
            
            for (int i = 0; i < 1000; i++)
            {
                var result = LinqExtension.GenerateRandomStringFast(100);
                Assert.Equal(100, result.Length);
            }

            var elapsed = (DateTime.Now - start).TotalMilliseconds;
            Assert.True(elapsed < 100, $"Fast random generation took {elapsed}ms");
        }

        [Fact]
        public void GenerateRandomHexString_GeneratesValidHex()
        {
            var result = LinqExtension.GenerateRandomHexString(16);

            Assert.Equal(16, result.Length);
            Assert.Matches("^[0-9A-F]+$", result);
        }

        [Fact]
        public void GenerateRandomString_Uniqueness()
        {
            var results = new HashSet<string>();

            // Generate 100 random strings
            for (int i = 0; i < 100; i++)
            {
                results.Add(LinqExtension.GenerateRandomString(10));
            }

            // Should all be unique (with very high probability)
            Assert.Equal(100, results.Count);
        }

        #endregion

        #region Additional Utility Tests

        [Fact]
        public void GetValueOrDefault_ReturnsValue()
        {
            var dict = new Dictionary<string, int> { ["key"] = 42 };

            var value = LinqExtension.GetValueOrDefault(dict, "key", -1);

            Assert.Equal(42, value);
        }

        [Fact]
        public void GetValueOrDefault_ReturnsDefault()
        {
            var dict = new Dictionary<string, int>();

            var value = LinqExtension.GetValueOrDefault(dict, "missing", -1);

            Assert.Equal(-1, value);
        }

        [Fact]
        public void Batch_CreatesCorrectBatches()
        {
            var source = Enumerable.Range(1, 10);

            var batches = source.Batch(3).ToList();

            Assert.Equal(4, batches.Count);
            Assert.Equal(new[] { 1, 2, 3 }, batches[0]);
            Assert.Equal(new[] { 4, 5, 6 }, batches[1]);
            Assert.Equal(new[] { 7, 8, 9 }, batches[2]);
            Assert.Equal(new[] { 10 }, batches[3]);
        }

        [Fact]
        public void Batch_EmptySource_ReturnsEmpty()
        {
            var source = Enumerable.Empty<int>();

            var batches = source.Batch(5).ToList();

            Assert.Empty(batches);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void GenerateRandomString_InvalidLength_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LinqExtension.GenerateRandomString(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => LinqExtension.GenerateRandomString(-1));
        }

        [Fact]
        public void GenerateRandomString_EmptyCharSet_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => LinqExtension.GenerateRandomString(10, ""));
            Assert.Throws<ArgumentException>(() => LinqExtension.GenerateRandomString(10, null));
        }

        [Fact]
        public void Batch_InvalidBatchSize_ThrowsException()
        {
            var source = new[] { 1, 2, 3 };
            
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Batch(0).ToList());
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Batch(-1).ToList());
        }

        #endregion
    }
}
using CCXT.Collector.Library.Types;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Library.Service
{
    public partial class AProcessing
    {
        private static ConcurrentQueue<ABookTickerItem> __recv_queue = null;

        /// <summary>
        ///
        /// </summary>
        private static ConcurrentQueue<ABookTickerItem> ReceiveQ
        {
            get
            {
                if (__recv_queue == null)
                    __recv_queue = new ConcurrentQueue<ABookTickerItem>();

                return __recv_queue;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void SendReceiveQ(ABookTickerItem message)
        {
            ReceiveQ.Enqueue(message);
        }

        private static ConcurrentBag<ABookTickerItem> __book_tickers = new ConcurrentBag<ABookTickerItem>();

        private async Task BuyAndSell(ABookTickerItem buy, ABookTickerItem sell)
        {
            if (buy.last_askQty != buy.askQty && sell.last_bidQty != sell.bidQty)
            {
                buy.last_askQty = buy.askQty;
                {
                    buy.price = buy.askPrice + buy.askPrice * buy.market.taker_fee;
                    buy.quantity = buy.askQty >= sell.bidQty ? sell.bidQty : buy.askQty;
                    if (buy.market.quote.total_amt < buy.price * buy.quantity)
                        buy.quantity = buy.market.quote.total_amt / buy.price;
                    buy.amount = buy.price * buy.quantity;
                    buy.profit = 0;
                }

                sell.last_bidQty = sell.bidQty;
                {
                    sell.price = sell.bidPrice - sell.bidPrice * sell.market.taker_fee;
                    sell.quantity = sell.bidQty >= buy.quantity ? buy.quantity : sell.bidQty;
                    if (sell.market.quantity < sell.quantity)
                        sell.quantity = sell.market.quantity;
                    sell.amount = sell.price * sell.quantity;
                    sell.profit = 0;
                }

                var _queue_out = 0;

                if (buy.quantity > 0 && sell.quantity > 0)
                {
                    if (sell.quantity != buy.quantity)
                    {
                        buy.quantity = sell.quantity;
                        buy.amount = buy.price * buy.quantity;
                    }

                    if (await DoSell(buy, sell, true) == true)
                    {
                        _queue_out = 1;
                        if (await DoBuy(buy, sell, true) == true)
                            _queue_out = 2;
                    }
                }
                else
                {
                    if (buy.quantity > 0 && buy.market.quote_id == "USDT")
                        if (await DoBuy(buy, sell) == true)
                            _queue_out = 3;

                    if (sell.quantity > 0 && sell.market.quote_id == "USDT")
                        if (await DoSell(buy, sell) == true)
                            _queue_out = 4;
                }

                await MessageOut(buy, sell, _queue_out, _queue_out < 1);
            }
        }

        private async Task ProfitStop(ABookTickerItem buy, ABookTickerItem sell)
        {
            if (sell.last_bidQty != sell.bidQty)
            {
                sell.last_bidQty = sell.bidQty;
                sell.quantity = sell.market.quantity;

                var _queue_out = 5;
                if (await DoSell(buy, sell, false, 10.0m) == true)
                    _queue_out = 6;

                await MessageOut(buy, sell, _queue_out, _queue_out < 6);
            }
        }

        private async Task<bool> DoBuy(ABookTickerItem buy, ABookTickerItem sell, bool swap_exchange = false)
        {
            var _result = false;

            if (buy.market.quote.total_amt > 0 && buy.quantity > 0)
            {
                if (buy.market.quote.invest_amt >= buy.market.quote.total_amt)
                {
                    var _profit_net = buy.market.quote.total_amt - buy.market.quote.invest_amt
                                    + __book_tickers.Where(b => b.market.quote_id == buy.market.quote_id)
                                                    .Sum(b => b.market.quantity * b.bidPrice * (1.0m - b.market.taker_fee));

                    if (_profit_net >= 0 || swap_exchange == true)
                    {
                        buy.price = buy.askPrice * (1.0m + buy.market.taker_fee);
                        sell.quantity = buy.market.quote.total_amt / buy.price;

                        buy.quantity = buy.askQty >= buy.quantity ? buy.quantity : buy.askQty;
                        buy.quantity = sell.quantity >= buy.quantity ? buy.quantity : sell.quantity;

                        buy.amount = buy.quantity * buy.price;

                        if (buy.market.quote.total_amt >= buy.amount)
                        {
                            // DO IT!!
                            await Task.Delay(1000);

                            buy.market.quote.total_amt -= buy.amount;
                            buy.market.quantity += buy.quantity;

                            if (buy.market.quote_id != "USDT")
                            {
                                var _quote = buy.market.quotes.Where(q => q.quote_id == buy.market.quote_id).SingleOrDefault();
                                if (_quote != null)
                                {
                                    _quote.invest_amt = buy.market.quote.total_amt;
                                    _quote.total_amt = buy.market.quote.total_amt;
                                }
                            }
                            else
                            {
                                var _quote = buy.market.quotes.Where(q => q.quote_id == buy.market.base_id).SingleOrDefault();
                                if (_quote != null)
                                {
                                    _quote.invest_amt = buy.market.quantity;
                                    _quote.total_amt = buy.market.quantity;
                                }
                            }

                            buy.market.buy_count++;
                            _result = true;
                        }
                    }
                }
                else
                {
                    buy.market.quote.income += buy.market.quote.total_amt - buy.market.quote.invest_amt;
                    buy.market.quote.total_amt = buy.market.quote.invest_amt;
                }
            }

            return _result;
        }

        private async Task<bool> DoSell(ABookTickerItem buy, ABookTickerItem sell, bool swap_exchange = false, decimal profit_rate = 2.0m)
        {
            var _result = false;

            if (sell.market.quantity > 0 && sell.quantity > 0)
            {
                if (sell.market.quote.invest_amt >= sell.market.quote.total_amt)
                {
                    var _net_price = (
                                        sell.market.quote.invest_amt
                                        -
                                        sell.market.quote.total_amt
                                        +
                                        __book_tickers.Where(b => b.market.quote_id == sell.market.quote_id && b.market.base_id != sell.market.base_id)
                                                      .Sum(b => b.market.quantity * b.bidPrice * (1.0m - b.market.taker_fee))
                                     )
                                     / sell.market.quantity;
                    if (sell.bidPrice > _net_price * (1.0m + sell.market.taker_fee * profit_rate) || swap_exchange == true)
                    {
                        sell.quantity = sell.bidQty >= sell.quantity ? sell.quantity : sell.bidQty;
                        sell.quantity = sell.market.quantity >= sell.quantity ? sell.quantity : sell.market.quantity;

                        buy.amount = _net_price * sell.quantity;

                        sell.amount = sell.bidPrice * sell.quantity * (1.0m - sell.market.taker_fee);
                        sell.profit = sell.amount - buy.amount;

                        if (sell.profit > 0)
                        {
                            // DO IT!!
                            await Task.Delay(1000);

                            sell.market.quote.total_amt += sell.amount;
                            sell.market.quantity -= sell.quantity;

                            if (sell.market.quote_id != "USDT")
                            {
                                var _quote = sell.market.quotes.Where(q => q.quote_id == sell.market.quote_id).SingleOrDefault();
                                if (_quote != null)
                                {
                                    _quote.invest_amt = sell.market.quote.total_amt;
                                    _quote.total_amt = sell.market.quote.total_amt;
                                }
                            }
                            else
                            {
                                var _quote = sell.market.quotes.Where(q => q.quote_id == sell.market.base_id).SingleOrDefault();
                                if (_quote != null)
                                {
                                    _quote.invest_amt = sell.market.quantity;
                                    _quote.total_amt = sell.market.quantity;
                                }
                            }

                            sell.market.sell_count++;
                            _result = true;
                        }
                    }
                }
                else
                {
                    sell.market.quote.income += sell.market.quote.total_amt - sell.market.quote.invest_amt;
                    sell.market.quote.total_amt = sell.market.quote.invest_amt;
                }
            }

            return _result;
        }

        private async Task MessageOut(ABookTickerItem buy, ABookTickerItem sell, int queue_out, bool use_queue)
        {
            var _exchg_rate = sell.exchangeRate != 1 ? sell.exchangeRate : buy.exchangeRate;

            var _income_sum = buy.market.quotes.Sum(q => q.income) + sell.market.quotes.Sum(q => q.income);
            var _profit_sum = buy.market.quotes.Sum(q => q.total_amt - q.invest_amt) + sell.market.quotes.Sum(q => q.total_amt - q.invest_amt)
                            + __book_tickers.Sum(b => b.market.quantity * b.bidPrice * (1.0m - b.market.taker_fee));

            var _message = $"{sell.symbol,8} => "
                      + $"{buy.market.exchange} <{queue_out}> {sell.market.exchange}: {_exchg_rate,13:#,##0.0000}, "
                      + $"{buy.askPrice,13:#,##0.0000} {buy.askQty,13:#,##0.0000} {buy.quantity,13:#,##0.0000} {buy.amount,13:#,##0.0000} {buy.profit,13:#,##0.0000} "
                      + $"{buy.market.quantity,13:#,##0.0000} {buy.market.quote.total_amt,13:#,##0.0000}, "
                      + $"{sell.bidPrice,13:#,##0.0000} {sell.bidQty,13:#,##0.0000} {sell.quantity,13:#,##0.0000} {sell.amount,13:#,##0.0000} {sell.profit,13:#,##0.0000} "
                      + $"{sell.market.quantity,13:#,##0.0000} {sell.market.quote.total_amt,13:#,##0.0000}, "
                      + $"{_income_sum,13:#,##0.0000} {_profit_sum,13:#,##0.0000}";

            if (use_queue == true)
                LoggerQ.WriteQ(_message);
            else
                LoggerQ.WriteO(_message);

            await Task.Delay(0);
        }

        private AsyncLock __async_lock = new AsyncLock();

        public async Task Start(CancellationTokenSource tokenSource, string baseId)
        {
            LoggerQ.WriteO($"arbitrage processing '{baseId}' service start...", FactoryQ.RootQName);

            var _processing = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        var _m = (ABookTickerItem)null;
                        if (ReceiveQ.TryPeek(out _m) == false)
                        {
                            var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                            if (_cancelled == true)
                                break;

                            continue;
                        }

                        if (_m.market.base_id != baseId)
                            continue;

                        using (await __async_lock.LockAsync(tokenSource.Token))
                        {
                            ReceiveQ.TryDequeue(out _);

                            var _o = __book_tickers.Where
                                        (
                                            b => b.market.exchange == _m.market.exchange && b.market.quote_id == _m.market.quote_id
                                            && b.market.base_id == baseId
                                        )
                                        .FirstOrDefault();
                            if (_o != null)
                            {
                                _m.last_askQty = _o.last_askQty;
                                _m.last_bidQty = _o.last_bidQty;

                                _o.sequential_id = _m.sequential_id;
                                _o.askQty = _m.askQty;
                                _o.bidQty = _m.bidQty;

                                if (_o.askPrice == _m.askPrice && _o.bidPrice == _m.bidPrice)
                                    continue;

                                _o.askPrice = _m.askPrice;
                                _o.bidPrice = _m.bidPrice;
                            }
                            else
                                __book_tickers.Add(_m);

                            var _tickers = __book_tickers.Where(b => b.market.exchange != _m.market.exchange && b.market.base_id == baseId);
                            foreach (var _t in _tickers)
                            {
                                if (_t.sequential_id >= _m.sequential_id)
                                    continue;

                                if (_t.askPrice < _m.bidPrice)
                                {
                                    await BuyAndSell(_t, _m);
                                }
                                else if (_m.askPrice < _t.bidPrice)
                                {
                                    await BuyAndSell(_m, _t);
                                }
                                else
                                {
                                    if (_t.market.quantity > 0)
                                        await ProfitStop(_m, _t);
                                    else if (_m.market.quantity > 0)
                                        await ProfitStop(_t, _m);
                                }
                            }
                        }

                        if (tokenSource.IsCancellationRequested == true)
                            break;
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        LoggerQ.WriteX(ex.ToString());
                    }
                }
            },
            tokenSource.Token
            );

            await Task.WhenAll(_processing);

            LoggerQ.WriteO($"arbitrage processing '{baseId}' service stopped...", FactoryQ.RootQName);
        }
    }
}
using Libs;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Web;
using System.Net.Http;
using System.Security.Policy;
using System.Collections.Generic;
using System.Linq;

namespace BybitBot
{
    public class Bybit : Bot
    {
        public override event ErrorHandler Error;

        public Bybit(string key, string secret, bool test) : base(key, secret, test)
        {
            if (test) baseurl = "https://api-testnet.bybit.com"; else baseurl = "https://api.bybit.com";
        }

        private HttpClient GetClient(string body, string ts, bool proxy_ = false)
        {
            HttpClientHandler httpClientHandler;
            if (MainWindow.test && proxy_)
            {
                var proxy = new WebProxy
                {
                    Address = new Uri($"http://127.0.0.1:8888"),
                    BypassProxyOnLocal = false,
                    UseDefaultCredentials = false,
                    //Credentials = new NetworkCredential(userName, password)
                };
                httpClientHandler = new HttpClientHandler { Proxy = proxy };
            }
            else httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var client = new HttpClient(httpClientHandler);

            string data = ts + key + "5000" + body;
            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            string sign = BitConverter.ToString(signature).Replace("-", "").ToLower();
            client.DefaultRequestHeaders.Add("X-BAPI-API-KEY", key);
            client.DefaultRequestHeaders.Add("X-BAPI-TIMESTAMP", ts);
            client.DefaultRequestHeaders.Add("X-BAPI-SIGN", sign);
            client.DefaultRequestHeaders.Add("X-BAPI-SIGN-TYPE", "2");
            client.DefaultRequestHeaders.Add("X-BAPI-RECV-WINDOW", "5000");

            return client;
        }

        public async Task<JObject> Go(string url, bool proxy = false)
        {
            string ts = Parser.Json(await new HttpClient().GetStringAsync(baseurl + "/v3/public/time"))["time"].ToString();
            var client = GetClient(url.Split('?')[1], ts, proxy);
            string s = await client.GetStringAsync(baseurl + url);
            var j = Parser.Json(s);
            if (j != null && j["retMsg"] != null && j["retMsg"].ToString() != "OK" && j["retMsg"].ToString() != "leverage not modified") { Error?.Invoke(j["retMsg"].ToString()); return null; }
            else return j;
        }

        public async Task<JObject> Post(string url, object o, bool proxy = false)
        {
            string body = JsonConvert.SerializeObject(o);
            string ts = Parser.Json(await new HttpClient().GetStringAsync(baseurl + "/v3/public/time"))["time"].ToString();
            var client = GetClient(body, ts, proxy);
            var response = await client.PostAsync(baseurl + url, new StringContent(body, Encoding.UTF8, "application/json"));
            string s = await response.Content.ReadAsStringAsync();
            var j = Parser.Json(s);
            if (j != null && j["retMsg"] != null && j["retMsg"].ToString() != "OK" && j["retMsg"].ToString() != "leverage not modified") { Error?.Invoke(j["retMsg"].ToString()); return null; }
            else return j;
        }

        public override async Task<List<Ticker>> Ticker()
        {
            var result = new List<Ticker>();
            try
            {
                var j = await Go($"/v5/market/tickers?category=linear");
                if (j != null)
                {
                    foreach (var item in j["result"]["list"])
                    {
                        string s = item["symbol"].ToString().Replace("USDT", "");
                        result.Add(new Ticker { Symbol = s, Price = (decimal)item["lastPrice"] });
                    }
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return result;
        }

        public override async Task<Ticker> Ticker(string symbol)
        {
            try
            {
                var j = await Go($"/v5/market/tickers?category=linear&symbol={symbol}USDT");
                if (j != null)
                {
                    return new Ticker { Symbol = symbol, Price = (decimal)j["result"]["list"][0]["lastPrice"] };
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return new Ticker();
        }

        public override async Task<int> Instrument(string symbol)
        {
            try
            {
                var j = await Go($"/v5/market/instruments-info?category=linear&symbol={symbol}USDT");
                if (j != null)
                {
                    double d = (double)j["result"]["list"][0]["lotSizeFilter"]["qtyStep"];
                    return Helper.DecimalCount(d);
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return 0;
        }

        public override async Task<decimal> Balance()
        {
            try
            {
                var j = await Go($"/v5/account/wallet-balance?accountType=UNIFIED");
                if (j != null)
                {
                    return Math.Round((decimal)j["result"]["list"][0]["totalWalletBalance"], 2);
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return 0;
        }

        public override async Task<List<Position>> Positions()
        {
            var result = new List<Position>();
            try
            {
                var j = await Go($"/v5/position/list?category=linear&settleCoin=USDT");
                if (j != null)
                {
                    foreach (var item in j["result"]["list"])
                    {
                        result.Add(new Position
                        {
                            Type = "Position",
                            Symbol = item["symbol"].ToString(),
                            Side = item["side"].ToString(),
                            Qty = (decimal)item["size"],
                            Price = (decimal)item["avgPrice"],
                            MarketPrice = (decimal)item["markPrice"],
                            UnrealizedPnl = Math.Round((decimal)item["unrealisedPnl"], 2),
                            RealizedPnl = Math.Round((decimal)item["curRealisedPnl"], 2),
                            TakeProfit = !string.IsNullOrEmpty(item["takeProfit"].ToString()) ? Math.Round((decimal)item["takeProfit"], 5) : 0,
                            StopLoss = !string.IsNullOrEmpty(item["stopLoss"].ToString()) ? Math.Round((decimal)item["stopLoss"], 5) : 0,
                            Leverage = item["leverage"] != null ? (int)item["leverage"] : 0
                        });
                    }
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return result;
        }

        public override async Task<List<Position>> Orders()
        {
            var result = new List<Position>();
            try
            {
                var j = await Go($"/v5/order/realtime?category=linear&settleCoin=USDT");
                if (j != null)
                {
                    foreach (var item in j["result"]["list"])
                    {
                        if (item["orderType"].ToString() != "Limit") continue;
                        result.Add(new Position
                        {
                            Type = "Limit",
                            Symbol = item["symbol"].ToString(),
                            Side = item["side"].ToString(),
                            Qty = (decimal)item["leavesQty"],
                            Price = (decimal)item["price"],
                            TakeProfit = !string.IsNullOrEmpty(item["takeProfit"].ToString()) ? Math.Round((decimal)item["takeProfit"], 2) : 0,
                            StopLoss = !string.IsNullOrEmpty(item["stopLoss"].ToString()) ? Math.Round((decimal)item["stopLoss"], 2) : 0,
                            OrderLinkId = item["orderLinkId"].ToString(),
                            Leverage = item["leverage"] != null ? (int)item["leverage"] : 0
                        });
                    }
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return result;
        }

        public override async Task<string> PlaceOrder(string symbol, string side, string orderType, decimal qty, decimal price = 0, string orderLinkId = "", int leverage = 0)
        {
            try
            {
                var o = new
                {
                    category = "linear",
                    symbol = symbol.ToUpper() + "USDT",
                    side,
                    orderType,
                    qty = qty.ToString().Replace(",", "."),
                    price = price.ToString().Replace(",", "."),
                    orderLinkId,
                    isLeverage = 1
                };
                var j = await Post("/v5/order/create", o, true);
                if (j != null && j["result"] != null && j["result"]["orderId"] != null)
                {
                    return j["result"]["orderId"].ToString();
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return null;
        }

        public override async Task<string> TradingStop(string symbol, decimal takeProfit, decimal stopLoss)
        {
            try
            {
                var o = new
                {
                    category = "linear",
                    symbol = symbol.ToUpper(),
                    takeProfit = takeProfit.ToString().Replace(",", "."),
                    stopLoss = stopLoss.ToString().Replace(",", ".")
                };
                var j = await Post("/v5/position/trading-stop", o, true);
                if (j != null && j["result"] != null && j["result"]["orderId"] != null)
                {
                    return j["result"]["orderId"].ToString();
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return null;
        }

        public override async Task<string> AmendOrder(string symbol, string orderLinkId, decimal qty, decimal price, decimal takeProfit, decimal stopLoss)
        {
            try
            {
                var o = new
                {
                    category = "linear",
                    symbol = symbol.ToUpper(),
                    orderLinkId,
                    qty = qty.ToString().Replace(",", "."),
                    price = price.ToString().Replace(",", "."),
                    takeProfit = takeProfit.ToString().Replace(",", "."),
                    stopLoss = stopLoss.ToString().Replace(",", ".")
                };
                var j = await Post("/v5/order/amend", o);
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return null;
        }

        public override async Task<string> CancelOrder(string symbol, string orderLinkId)
        {
            try
            {
                var o = new
                {
                    category = "linear",
                    symbol = symbol.ToUpper(),
                    orderLinkId
                };
                var j = await Post("/v5/order/cancel", o);
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return null;
        }

        public override async Task<string> SetLeverage(string symbol, int value)
        {
            try
            {
                var o = new
                {
                    category = "linear",
                    symbol = symbol.ToUpper() + "USDT",
                    buyLeverage = value.ToString(),
                    sellLeverage = value.ToString()
                };
                var j = await Post("/v5/position/set-leverage", o);
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return null;
        }

        public override async Task<List<History>> History(DateTime start)
        {
            var result = new List<History>();
            try
            {
                string nextPageCursor = "";
                while (true)
                {
                    var j = await Go($"/v5/position/closed-pnl?category=linear&limit=100&endTime={Helper.DateTimeToJavaTimeStamp(start)}&cursor={nextPageCursor}");
                    if (j != null)
                    {
                        foreach (var item in j["result"]["list"])
                        {
                            result.Add(new History
                            {
                                ApiKey = key,
                                Pnl = (decimal)item["closedPnl"],
                                Created = Helper.JavaTimeStampToDateTime((long)item["createdTime"]),
                                Updated = Helper.JavaTimeStampToDateTime((long)item["updatedTime"])
                            });
                        }
                        nextPageCursor = j["result"]["nextPageCursor"].ToString();
                        if (j["result"]["list"].Count() != 100 || string.IsNullOrEmpty(nextPageCursor)) break;
                    }
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return result;
        }
    }
}

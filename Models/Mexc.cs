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
using System.Data.Common;
using System.Drawing.Drawing2D;

namespace BybitBot
{
    public class Mexc : Bot
    {
        public override event ErrorHandler Error;

        public Mexc(string key, string secret, bool test) : base(key, secret, test)
        {
            baseurl = "https://contract.mexc.com";
        }

        private HttpClient GetClient(string body, string ts, out string data, string method, bool proxy_ = false)
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

            if (method == "GET") data = body + (string.IsNullOrEmpty(body) ? "" : "&") + "recvWindow=5000&timestamp=" + ts;
            else data = body;
            string data_ = key + ts + data;
            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(data_));
            string sign = BitConverter.ToString(signature).Replace("-", "").ToLower();
            client.DefaultRequestHeaders.Add("ApiKey", key);
            client.DefaultRequestHeaders.Add("Request-Time", ts.ToString());
            client.DefaultRequestHeaders.Add("Signature", sign);

            return client;
        }

        public async Task<JObject> Go(string url, bool proxy = false)
        {
            string ts = Parser.Json(await new HttpClient().GetStringAsync("https://api.mexc.com/api/v3/time"))["serverTime"].ToString();
            var split = url.Split('?');
            var client = GetClient(split.Length > 1 ? split[1] : "", ts, out string data, "GET", proxy);
            string u = baseurl + split[0] + "?" + data;
            string s = await client.GetStringAsync(u);
            var j = Parser.Json(s);
            if (j != null && j["code"] != null && (int)j["code"] != 0) { Error?.Invoke(j["message"].ToString()); return null; }
            else return j;
        }

        public async Task<JObject> Post(string url, object o, bool proxy = false)
        {
            string body = JsonConvert.SerializeObject(o);
            string ts = Parser.Json(await new HttpClient().GetStringAsync("https://api.mexc.com/api/v3/time"))["serverTime"].ToString();
            var client = GetClient(body, ts, out var _data, "POST", proxy);
            var response = await client.PostAsync(baseurl + url, new StringContent(body, Encoding.UTF8, "application/json"));
            string s = await response.Content.ReadAsStringAsync();
            var j = Parser.Json(s);
            if (j != null && j["code"] != null && (int)j["code"] != 0) { Error?.Invoke(j["message"].ToString()); return null; }
            else return j;
        }

        public override async Task<List<Ticker>> Ticker()
        {
            var result = new List<Ticker>();
            try
            {
                var j = await Go($"/api/v1/contract/ticker");
                if (j != null)
                {
                    foreach (var item in j["data"])
                    {
                        string s = item["symbol"].ToString().Replace("_USDT", "");
                        if (!s.Contains("_USD")) result.Add(new Ticker { Symbol = s, Price = (decimal)item["lastPrice"] });
                    }
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return result.OrderBy(z => z.Symbol).ToList();
        }

        public override async Task<Ticker> Ticker(string symbol)
        {
            try
            {
                var j = await Go($"/api/v1/contract/ticker?symbol={symbol}_USDT");
                if (j != null)
                {
                    return new Ticker { Symbol = symbol, Price = (decimal)j["data"]["lastPrice"] };
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return new Ticker();
        }

        public override async Task<int> Instrument(string symbol)
        {
            try
            {
                var j = await Go($"/api/v1/contract/detail?symbol={symbol}_USDT");
                if (j != null)
                {
                    double d = (double)j["data"]["contractSize"];
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
                var j = await Go($"/api/v1/private/account/asset/USDT");
                if (j != null)
                {
                    return Math.Round((decimal)j["data"]["availableBalance"], 2);
                }
            }
            catch (Exception ex) { Error(ex.ToString()); }
            return 0;
        }

        public override async Task<List<Position>> Positions()
        {
            var result = new List<Position>();
            
            return result;
        }

        public override async Task<List<Position>> Orders()
        {
            var result = new List<Position>();
            
            return result;
        }

        public override async Task<string> PlaceOrder(string symbol, string side, string orderType, decimal qty, decimal price = 0, string orderLinkId = "", int leverage = 0)
        {
            try
            {
                var o = new
                {
                    symbol = symbol.ToUpper() + "_USDT",
                    vol = qty.ToString().Replace(",", "."),
                    leverage,
                    side = side == "Buy" ? 1 : 3,
                    type = orderType == "Market" ? 5 : 1,
                    openType = 1,
                    externalOid = orderLinkId
                };
                var j = await Post("/api/v1/private/order/submit", o, true);
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
            
            return null;
        }

        public override async Task<string> AmendOrder(string symbol, string orderLinkId, decimal qty, decimal price, decimal takeProfit, decimal stopLoss)
        {
            
            return null;
        }

        public override async Task<string> CancelOrder(string symbol, string orderLinkId)
        {
            
            return null;
        }

        public override async Task<string> SetLeverage(string symbol, int value)
        {
            return null;
        }

        public override async Task<List<History>> History(DateTime start)
        {
            var result = new List<History>();
            
            return result;
        }
    }
}

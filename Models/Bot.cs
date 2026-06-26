using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BybitBot
{
    public abstract class Bot
    {
        public string key;
        public string secret, baseurl;

        public delegate void ErrorHandler(string s); public abstract event ErrorHandler Error;

        public Bot(string key, string secret, bool test)
        {
            this.key = key;
            this.secret = secret;
        }

        public abstract Task<List<Ticker>> Ticker();
        public abstract Task<Ticker> Ticker(string symbol);
        public abstract Task<int> Instrument(string symbol);
        public abstract Task<decimal> Balance();
        public abstract Task<List<Position>> Positions();
        public abstract Task<List<Position>> Orders();
        public abstract Task<string> PlaceOrder(string symbol, string side, string orderType, decimal qty, decimal price = 0, string orderLinkId = "", int leverage = 0);
        public abstract Task<string> TradingStop(string symbol, decimal takeProfit, decimal stopLoss);
        public abstract Task<string> AmendOrder(string symbol, string orderLinkId, decimal qty, decimal price, decimal takeProfit, decimal stopLoss);
        public abstract Task<string> CancelOrder(string symbol, string orderLinkId);
        public abstract Task<string> SetLeverage(string symbol, int value);
        public abstract Task<List<History>> History(DateTime start);
    }
}

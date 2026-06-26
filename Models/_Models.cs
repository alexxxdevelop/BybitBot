using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BybitBot
{
    public class Set
    {
        public double period = 1;
        public List<Api> apis = new List<Api>();
    }

    public class Api
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
        public bool Test { get; set; }
        public string Balance { get; set; }
        public decimal Comission { get; set; }
        public string ComissionS { get { return $"{Comission.ToString(true)}%"; } }
        public bool Active { get; set; }
        public string Exchange { get; set; }
    }

    public class Position
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Symbol { get; set; }
        public string Side { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public decimal RealizedPnl { get; set; }
        public decimal Value { get { return Math.Round(Qty * Price, 2); } }
        public decimal TakeProfit { get; set; }
        public decimal StopLoss { get; set; }
        public string OrderLinkId { get; set; }
        public int Leverage { get; set; }
    }

    public class History
    {
        public string ApiKey { get; set; }
        public decimal Pnl { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
    }

    public class HistoryTable
    {
        public string ApiName { get; set; }
        public decimal Day { get; set; }
        public decimal Week { get; set; }
        public decimal Month { get; set; }
        public decimal Year { get; set; }
        public decimal DayC { get; set; }
        public decimal WeekC { get; set; }
        public decimal MonthC { get; set; }
        public decimal YearC { get; set; }
        public decimal Filter { get; set; }
        public decimal FilterC { get; set; }
    }

    public enum HistoryPeriod { day, week, month, year }

    public class Ticker
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
    }

    public static class DecimalExtensions
    {
        public static string ToString(this decimal some, bool compactFormat)
        {
            if (compactFormat)
            {
                return some.ToString("#,##0.########").Replace(",", " ");
            }
            else
            {
                return some.ToString();
            }
        }
    }
}

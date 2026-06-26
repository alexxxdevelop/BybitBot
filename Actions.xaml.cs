using Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BybitBot
{
    public partial class Actions : Window
    {
        MainWindow main;
        public Position position;

        public Actions()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                main = this.Owner as MainWindow;
                while (position == null) { main.Log("wait"); await Task.Delay(100); }
                posPrice.Text = position.Price.ToString();
                posQty.Text = position.Qty.ToString();
                posMarketPrice.Text = position.MarketPrice.ToString();
                tpPrice.Text = position.TakeProfit.ToString();
                slPrice.Text = position.StopLoss.ToString();
                if (position.TakeProfit != 0)
                {
                    if (position.Side == "Buy") tpRoi.Text = Math.Round((position.TakeProfit - position.Price) / position.Price * 100, 2).ToString();
                    else tpRoi.Text = Math.Round((position.Price - position.TakeProfit) / position.Price * 100, 2).ToString();
                }
                if (position.StopLoss != 0)
                {
                    if (position.Side == "Buy") slRoi.Text = Math.Round((position.StopLoss - position.Price) / position.Price * 100, 2).ToString();
                    else slRoi.Text = Math.Round((position.Price - position.StopLoss) / position.Price * 100, 2).ToString();
                }
                if (position.Type == "Position") gbOrder.Visibility = Visibility.Collapsed;
                else
                {
                    gbPosition.Visibility = Visibility.Collapsed;
                    tpsl.Visibility = Visibility.Collapsed;
                    orderQty.Text = position.Qty.ToString();
                    orderPrice.Text = position.Price.ToString();
                }
            }
            catch { }
        }

        private async void tpsl_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            b.IsEnabled = false;
            decimal takeProfitPer = Helper.DecimalParse(tpRoi.Text);
            decimal stopLossPer = Helper.DecimalParse(slRoi.Text);
            foreach (var bot in main.bots.ToList())
            {
                var api = main.config.apis.FirstOrDefault(z => z.Key == bot.key);
                if (!api.Active) continue;
                var rec = (await bot.Positions()).FirstOrDefault(z => z.Symbol == position.Symbol && z.Side == position.Side && z.Type == position.Type);
                if (rec != null)
                {
                    decimal qty = rec.Qty;
                    decimal price = rec.Price;
                    decimal tp = 0;
                    if (takeProfitPer > 0)
                    {
                        if (rec.Side == "Buy") tp = rec.Price + rec.Price * (takeProfitPer / 100);
                        else tp = rec.Price - rec.Price * (takeProfitPer / 100);
                    }
                    decimal sl = 0;
                    if (stopLossPer > 0)
                    {
                        if (rec.Side == "Buy") sl = rec.Price + rec.Price * (stopLossPer / 100);
                        else sl = rec.Price - rec.Price * (stopLossPer / 100);
                    }
                    await bot.TradingStop(position.Symbol, tp, sl);
                }
            }
            main.UpdateBalance();
            b.IsEnabled = true;
            this.Close();
        }

        private async void add_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            b.IsEnabled = false;
            decimal per = Helper.DecimalParse(addValue.Text) / 100;
            decimal price = position.MarketPrice;
            int step = await main.bots.ToList()[0].Instrument(position.Symbol.Replace("USDT", ""));
            foreach (var bot in main.bots.ToList())
            {
                var api = main.config.apis.FirstOrDefault(z => z.Key == bot.key);
                if (!api.Active) continue;
                decimal balance = Helper.DecimalParse(api.Balance) * per;
                decimal qty = Math.Round(balance / price, step);
                string orderId = await bot.PlaceOrder(position.Symbol.Replace("USDT", ""), position.Side, "Market", qty);
                if (!string.IsNullOrEmpty(orderId)) main.Log(orderId);
            }
            main.UpdateBalance();
            b.IsEnabled = true;
            this.Close();
        }

        private async void close_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            b.IsEnabled = false;
            decimal per = Helper.DecimalParse(closeValue.Text) / 100;
            int step = await main.bots.ToList()[0].Instrument(position.Symbol.Replace("USDT", ""));
            foreach (var bot in main.bots.ToList())
            {
                var api = main.config.apis.FirstOrDefault(z => z.Key == bot.key);
                if (!api.Active) continue;
                foreach (var rec in (await bot.Positions()).Where(z => z.Symbol == position.Symbol && z.Side == position.Side))
                {
                    decimal qty = Math.Round(rec.Qty * per, step);
                    string orderId = await bot.PlaceOrder(position.Symbol.Replace("USDT", ""), rec.Side == "Buy" ? "Sell" : "Buy", "Market", qty);
                    if (!string.IsNullOrEmpty(orderId)) main.Log(orderId);
                }
            }
            main.UpdateBalance();
            b.IsEnabled = true;
            this.Close();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void tpRoi_KeyUp(object sender, KeyEventArgs e)
        {
            decimal v = Helper.DecimalParse(tpRoi.Text);
            if (v == 0) { tpPrice.Text = "0"; return; }
            decimal r = 0;
            if (position.Side == "Buy") r = position.Price + position.Price * (v / 100);
            else r = position.Price - position.Price * (v / 100);
            tpPrice.Text = Math.Round(r, 5).ToString();
        }

        private void tpPrice_KeyUp(object sender, KeyEventArgs e)
        {
            decimal v = Helper.DecimalParse(tpPrice.Text);
            if (v == 0) { tpRoi.Text = "0"; return; }
            decimal r = 0;
            if (position.Side == "Buy") r = (v - position.Price) / position.Price * 100;
            else r = (position.Price - v) / position.Price * 100;
            tpRoi.Text = Math.Round(r, 5).ToString();
        }

        private void slRoi_KeyUp(object sender, KeyEventArgs e)
        {
            decimal v = Helper.DecimalParse(slRoi.Text);
            if (v == 0) { slPrice.Text = "0"; return; }
            decimal r = 0;
            if (position.Side == "Buy") r = position.Price + position.Price * (v / 100);
            else r = position.Price - position.Price * (v / 100);
            slPrice.Text = Math.Round(r, 5).ToString();
        }

        private void slPrice_KeyUp(object sender, KeyEventArgs e)
        {
            decimal v = Helper.DecimalParse(slPrice.Text);
            if (v == 0) { slRoi.Text = "0"; return; }
            decimal r = 0;
            if (position.Side == "Buy") r = (v - position.Price) / position.Price * 100;
            else r = (position.Price - v) / position.Price * 100;
            slRoi.Text = Math.Round(r, 5).ToString();
        }

        private async void orderChange_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            b.IsEnabled = false;
            decimal takeProfitPer = Helper.DecimalParse(tpRoi.Text);
            decimal stopLossPer = Helper.DecimalParse(slRoi.Text);
            foreach (var bot in main.bots.ToList())
            {
                var api = main.config.apis.FirstOrDefault(z => z.Key == bot.key);
                if (!api.Active) continue;
                var rec = (await bot.Positions()).FirstOrDefault(z => z.Symbol == position.Symbol && z.Side == position.Side && z.Type == position.Type);
                if (rec != null)
                {
                    decimal qty = rec.Qty;
                    decimal price = rec.Price;
                    decimal tp = 0;
                    if (takeProfitPer > 0)
                    {
                        if (rec.Side == "Buy") tp = rec.Price + rec.Price * (takeProfitPer / 100);
                        else tp = rec.Price - rec.Price * (takeProfitPer / 100);
                    }
                    decimal sl = 0;
                    if (stopLossPer > 0)
                    {
                        if (rec.Side == "Buy") sl = rec.Price + rec.Price * (stopLossPer / 100);
                        else sl = rec.Price - rec.Price * (stopLossPer / 100);
                    }
                    await bot.AmendOrder(position.Symbol, position.OrderLinkId, qty, price, tp, sl);
                }
            }
            main.UpdateBalance();
            b.IsEnabled = true;
            this.Close();
        }

        private async void orderClose_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            b.IsEnabled = false;
            foreach (var bot in main.bots.ToList())
            {
                var api = main.config.apis.FirstOrDefault(z => z.Key == bot.key);
                if (!api.Active) continue;
                foreach (var rec in (await bot.Orders()).Where(z => z.OrderLinkId == position.OrderLinkId))
                {
                    await bot.CancelOrder(position.Symbol, position.OrderLinkId);
                }
            }
            main.UpdateBalance();
            b.IsEnabled = true;
            this.Close();
        }
    }
}

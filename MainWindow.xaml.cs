using Libs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BybitBot
{
    public partial class MainWindow : Window
    {
        public static bool test;
        public Set config = new Set();
        public List<Bot> bots = new List<Bot>();
        List<Position> positions = new List<Position>();
        List<string> coins = new List<string>(), coins1 = new List<string>();
        List<History> history = new List<History>();
        HistoryPeriod historyPeriod = HistoryPeriod.day;
        DateTime sd1, sd2;
        string exchange;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            test = Directory.Exists(@"d:\Projects\Me\_\");
            bool b = true;
            if (!test)
            {
                string mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString()).FirstOrDefault();
                if (mac != "9C5C8E7CABCF" && mac != "141333ACC1E7" && mac != "503EAAA76AC0" && mac != "AC9E174B819F") { Log("Неверный mac " + mac, Brushes.Red); b = false; }
                if (b)
                {
                    string s = await new HttpClient().GetStringAsync("http://akriplast.com/data.dat");
                    if (s != "data") { Log("Доступ закрыт", Brushes.Red); b = false; }
                }
            }
            if (b)
            {
                LoadConfig();
                //GetLog();
            }
        }

        #region События элементов
        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            playButton.Visibility = System.Windows.Visibility.Collapsed;
            pauseButton.Visibility = System.Windows.Visibility.Visible;

            SaveConfig();
            Start();
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            playButton.Visibility = System.Windows.Visibility.Visible;
            pauseButton.Visibility = System.Windows.Visibility.Collapsed;

            Stop();
        }

        private void clearLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.Delete(Helper.PathCurrent + "logs", true);
                log.Document.Blocks.Clear();
            }
            catch { }
        }

        private void addApi_Click(object sender, RoutedEventArgs e)
        {
            ShowApiWindow();
        }

        private void editApi_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            int id = (int)b.Tag;
            ShowApiWindow(id);
        }

        private void removeApi_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            int id = (int)b.Tag;
            RemoveApi(id);
        }

        private void newOrderType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (newOrderPrice == null) return;
            if (newOrderType.SelectedIndex == 1) newOrderPrice.IsEnabled = true; else newOrderPrice.IsEnabled = false;
        }

        private async void newOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var b = sender as Button;
                b.IsEnabled = false;
                decimal per = Helper.DecimalParse(newOrderAmount.Text) / 100;
                int leverage = Helper.IntParse(newOrderLeverage.Text);
                decimal price = (await bots.ToList()[0].Ticker(newOrderCoin.Text)).Price;
                int step = await bots.ToList()[0].Instrument(newOrderCoin.Text);
                string orderLinkId = Helper.RandomString(20);
                foreach (var bot in bots.ToList())
                {
                    var api = config.apis.FirstOrDefault(z => z.Key == bot.key);
                    if (!api.Active) continue;
                    decimal balance = Helper.DecimalParse(api.Balance) * per;
                    decimal qty = Math.Round(balance / price, step);
                    await bot.SetLeverage(newOrderCoin.Text, leverage);
                    string orderId = await bot.PlaceOrder(newOrderCoin.Text, newOrderSide.Text, newOrderType.Text, qty, Helper.DecimalParse(newOrderPrice.Text), orderLinkId, leverage);
                    if (!string.IsNullOrEmpty(orderId)) Log(orderId);
                }
                UpdateBalance();
                b.IsEnabled = true;
            }
            catch { }
        }

        private void coinName_MouseEnter(object sender, MouseEventArgs e)
        {
            var t = sender as TextBlock;
            t.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#333");
        }

        private void coinName_MouseLeave(object sender, MouseEventArgs e)
        {
            var t = sender as TextBlock;
            t.Background = null;
        }

        private void coinName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var t = sender as TextBlock;
            newOrderCoin.Text = t.Text;
        }

        private void coin_KeyUp(object sender, KeyEventArgs e)
        {
            coins.Clear();
            if (string.IsNullOrEmpty(coin.Text)) coins.AddRange(coins1);
            else coins.AddRange(coins1.Where(z => z.Contains(coin.Text.ToUpper())));
            l_coins.Items.Refresh();
        }

        private void actions_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            int id = (int)b.Tag;
            ShowActionsWindow(id);
        }

        private void updateHistory_Click(object sender, RoutedEventArgs e)
        {
            History();
        }

        private void activeApi_Click(object sender, RoutedEventArgs e)
        {
            var el = sender as CheckBox;
            int id = (int)el.Tag;
            var rec = config.apis.FirstOrDefault(z => z.Id == id);
            if (rec != null)
            {
                rec.Active = el.IsChecked.Value;
                SaveConfig();
            }
        }

        private void date1_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                sd1 = date1.SelectedDate.Value.AddHours(time1.SelectedIndex).ToUniversalTime();
                sd2 = date2.SelectedDate.Value.AddHours(time2.SelectedIndex).ToUniversalTime();
                HistoryRender();
            }
            catch { }
        }

        private void exchange_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            exchange = ((ComboBoxItem)exchange_cb.SelectedValue).Content.ToString();
            UpdateBots();
        }
        #endregion

        #region Таймеры
        Timer timer;
        bool busy = false;

        public void Start()
        {
            try
            {
                timer = new Timer(DoWork, null, 0, (long)TimeSpan.FromSeconds(config.period).TotalMilliseconds);
            }
            catch (Exception ex) { Log(ex, Brushes.Red); }
        }

        public void Stop()
        {
            try
            {
                timer.Dispose();
            }
            catch (Exception ex) { Log(ex, Brushes.Red); }
        }

        async void DoWork(object state)
        {
            /*if (busy) return;
            busy = true;
            await Go();
            busy = false;*/
        }
        #endregion

        #region Разное
        void LoadConfig()
        {
            config = Deserialize<Set>("config");
            if (config == null) config = new Set();

            for (int i = 0; i < 24; i++)
            {
                time1.Items.Add(i);
                time2.Items.Add(i);
            }
            time1.SelectedIndex = 0;
            time2.SelectedIndex = 23;
            date1.SelectedDate = DateTime.Today;
            date2.SelectedDate = DateTime.Today;
            addApi.Visibility = Visibility.Visible;
            dg_orders.ItemsSource = positions;
            l_coins.ItemsSource = coins;
            exchange_cb_SelectionChanged(this, null);
            History();
            Go();
        }

        bool SaveConfig()
        {


            Serialize(config, "config");
            return true;
        }

        void StopUi()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                pauseButton_Click(this, null);
            }));
        }

        void Render()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                lastUpdate.Text = DateTime.Now.ToString("G");
            }));
        }

        async void GetLog()
        {
            try
            {
                string path = Helper.PathCurrent + "logs";
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                var logs = new List<string>();
                var files = Directory.GetFiles(path).Reverse().Take(2).Reverse();
                foreach (var file in files)
                {
                    while (true)
                    {
                        try
                        {
                            logs.AddRange(Regex.Split(File.ReadAllText(file, Encoding.UTF8), @"\|\|"));
                            break;
                        }
                        catch { await Task.Delay(10); }
                    }
                }
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    log.AppendText(string.Join("", logs));
                    log.ScrollToEnd();
                }));
            }
            catch { }
        }

        public async void Serialize(object o, string fileName)
        {
            while (true)
            {
                try
                {
                    string s = JsonConvert.SerializeObject(o);
                    File.WriteAllText(string.Format("{0}{1}.json", Helper.PathCurrent, fileName), s, Encoding.UTF8);
                    break;
                }
                catch { await Task.Delay(100); }
            }
        }

        T Deserialize<T>(string fileName)
        {
            T r = default(T);

            string path = string.Format("{0}{1}.json", Helper.PathCurrent, fileName);
            if (File.Exists(path))
            {
                string s = File.ReadAllText(path, Encoding.UTF8);
                r = JsonConvert.DeserializeObject<T>(s);
            }

            return r;
        }

        public void Log(object s, Brush b = null)
        {
            s = Helper.Log(s);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (b == null) b = Brushes.Gray;
                string text = new TextRange(log.Document.ContentStart, log.Document.ContentEnd).Text;
                if (text.Split('\n').Length > 1000) log.Document.Blocks.Clear();
                TextRange tr = new TextRange(log.Document.ContentEnd, log.Document.ContentEnd);
                tr.Text = s + "\r\n";
                if (string.IsNullOrEmpty(text)) tr.Text += "\r\n";
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, b);
                log.ScrollToEnd();
            }));
        }
        #endregion

        #region go
        async void Go()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                try
                {
                    UpdateBalance();
                }
                catch (Exception ex) { Log(ex, Brushes.Red); }
            }
        }

        public void SaveApi(int id, string name, string key, string secret, string comission, bool test)
        {
            var rec = config.apis.FirstOrDefault(z => z.Id == id);
            if (rec == null) { rec = new Api { Id = config.apis.Count + 1 }; config.apis.Add(rec); }
            rec.Name = name;
            rec.Key = key;
            rec.Secret = secret;
            rec.Comission = Helper.DecimalParse(comission);
            rec.Exchange = exchange;
            rec.Test = test;
            UpdateBots();
        }

        void ShowApiWindow(int id = 0)
        {
            var window = new ApiWindow();
            window.Owner = this;
            var rec = config.apis.FirstOrDefault(z => z.Id == id);
            if (rec != null)
            {
                window.id = rec.Id;
                window.apiName.Text = rec.Name;
                window.apiKey.Text = rec.Key;
                window.apiSecret.Text = rec.Secret;
                window.apiComission.Text = rec.Comission.ToString(true);
                window.apiTest.IsChecked = rec.Test;
            }
            window.ShowDialog();
        }

        void ShowActionsWindow(int id = 0)
        {
            var window = new Actions();
            window.Owner = this;
            window.position = positions.FirstOrDefault(z => z.Id == id);
            window.ShowDialog();
        }

        void RemoveApi(int id)
        {
            config.apis.RemoveAll(z => z.Id == id);
            UpdateBots();
        }

        async void UpdateBots()
        {
            try
            {
                apis.ItemsSource = config.apis.Where(z => z.Exchange == exchange);
                bots.Clear();
                foreach (var rec in config.apis.Where(z => z.Exchange == exchange))
                {
                    Bot bot = null;
                    if (string.IsNullOrEmpty(rec.Exchange) || rec.Exchange == "Bybit") bot = new Bybit(rec.Key, rec.Secret, rec.Test);
                    else if (rec.Exchange == "Mexc") bot = new Mexc(rec.Key, rec.Secret, rec.Test);
                    bot.Error += (s) => Log(s, Brushes.Red);
                    bots.Add(bot);
                }

                if (bots.Count > 0)
                {
                    coins1.Clear();
                    coins1.AddRange((await bots[0].Ticker()).Select(z => z.Symbol));
                    coin_KeyUp(this, null);
                }
                SaveConfig();
            }
            catch { }
        }

        public async void UpdateBalance()
        {
            if (busy) return;
            busy = true;
            try
            {
                var positions_ = new List<Position>();
                if (bots.Count > 0)
                {
                    foreach (var bot in bots.ToList())
                    {
                        var api = config.apis.FirstOrDefault(z => z.Key == bot.key);
                        if (!api.Active) continue;
                        api.Balance = "$" + await bot.Balance();
                        var recs = await bot.Positions();
                        recs.AddRange(await bot.Orders());
                        foreach (var rec in recs)
                        {
                            var position = (rec.Type != "Limit") ? positions_.FirstOrDefault(z => z.Symbol == rec.Symbol && z.Type == rec.Type && z.Side == rec.Side && z.Leverage == rec.Leverage) :
                                positions_.FirstOrDefault(z => z.Symbol == rec.Symbol && z.Type == rec.Type && z.Side == rec.Side && z.OrderLinkId == rec.OrderLinkId && z.Leverage == rec.Leverage);
                            if (position == null)
                            {
                                position = new Position { Id = positions_.Count + 1, Symbol = rec.Symbol, Side = rec.Side, Type = rec.Type, OrderLinkId = rec.OrderLinkId, Leverage = rec.Leverage };
                                positions_.Add(position);
                            }
                            position.Qty += rec.Qty;
                            if (position.Price > 0) position.Price = Math.Round((position.Price + rec.Price) / 2, 5); else position.Price = rec.Price;
                            position.MarketPrice = rec.MarketPrice;
                            position.UnrealizedPnl += rec.UnrealizedPnl;
                            position.RealizedPnl += rec.RealizedPnl;
                            if (rec.TakeProfit != 0) position.TakeProfit = rec.TakeProfit;
                            if (rec.StopLoss != 0) position.StopLoss = rec.StopLoss;
                        }
                    }
                    var tickers = await bots.ToList()[0].Ticker();
                    var list = positions_.Select(x => x.Symbol).Distinct().ToList();
                    foreach (var item in list)
                    {
                        var ticker = tickers.FirstOrDefault(z => z.Symbol == item.Replace("USDT", ""));
                        if (ticker != null)
                        {
                            foreach (var position in positions_.Where(z => z.Symbol == item)) position.MarketPrice = ticker.Price;
                        }
                    }
                }
                positions.Clear();
                positions.AddRange(positions_);
                apis.Items.Refresh();
                dg_orders.Items.Refresh();
                SaveConfig();
            }
            catch { }
            busy = false;
        }

        void History()
        {
            updateHistory.IsEnabled = false;
            Task.Factory.StartNew(async () =>
            {
                history.Clear();
                var d = DateTime.Now;
                while (d > DateTime.Now.AddYears(-1))
                {
                    foreach (var bot in bots)
                    {
                        if (bot.GetType().Name == "Mexc") continue;
                        history.AddRange(await bot.History(d));
                        HistoryRender();
                    }
                    d = d.AddDays(-7);
                }
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    updateHistory.IsEnabled = true;
                }));
            });
        }

        void HistoryRender()
        {
            var hts = new List<HistoryTable>();
            
            foreach (var api in config.apis.Where(z => z.Exchange == exchange))
            {
                var ht = new HistoryTable { ApiName = api.Name };
                var hs = history.Where(z => z.ApiKey == api.Key).ToList();
                ht.Day = Math.Round(hs.Where(z => z.Created > DateTime.Now.AddDays(-1)).Sum(z => z.Pnl), 2);
                ht.Week = Math.Round(hs.Where(z => z.Created > DateTime.Now.AddDays(-7)).Sum(z => z.Pnl), 2);
                ht.Month = Math.Round(hs.Where(z => z.Created > DateTime.Now.AddMonths(-1)).Sum(z => z.Pnl), 2);
                ht.Year = Math.Round(hs.Where(z => z.Created > DateTime.Now.AddYears(-1)).Sum(z => z.Pnl), 2);
                ht.Filter = Math.Round(hs.Where(z => z.Created >= sd1 && z.Created <= sd2).Sum(z => z.Pnl), 2);
                ht.DayC = Math.Round(ht.Day * api.Comission / 100, 2);
                ht.WeekC = Math.Round(ht.Week * api.Comission / 100, 2);
                ht.MonthC = Math.Round(ht.Month * api.Comission / 100, 2);
                ht.YearC = Math.Round(ht.Year * api.Comission / 100, 2);
                ht.FilterC = Math.Round(ht.Filter * api.Comission / 100, 2);
                hts.Add(ht);
            }
            {
                var ht = new HistoryTable { ApiName = "Всего" };
                var hs = history.ToList();
                ht.Day = Math.Round(hts.Sum(z => z.Day), 2);
                ht.Week = Math.Round(hts.Sum(z => z.Week), 2);
                ht.Month = Math.Round(hts.Sum(z => z.Month), 2);
                ht.Year = Math.Round(hts.Sum(z => z.Year), 2);
                ht.Filter = Math.Round(hts.Sum(z => z.Filter), 2);
                ht.DayC = Math.Round(hts.Sum(z => z.DayC), 2);
                ht.WeekC = Math.Round(hts.Sum(z => z.WeekC), 2);
                ht.MonthC = Math.Round(hts.Sum(z => z.MonthC), 2);
                ht.YearC = Math.Round(hts.Sum(z => z.YearC), 2);
                ht.FilterC = Math.Round(hts.Sum(z => z.FilterC), 2);
                hts.Insert(0, ht);
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                dg_history.ItemsSource = hts;
                dg_history.Items.Refresh();
            }));
        }
        #endregion
    }
}


/*
 * Style="{StaticResource WindowStyled}" Title="Настройки" Height="600" Width="800" ShowInTaskbar="False" WindowStartupLocation="CenterScreen" WindowStyle="ToolWindow" ResizeMode="NoResize" Loaded="Window_Loaded"
 * 
        MainWindow main;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            main = this.Owner as MainWindow;
        }

        public void ShowSettings()
        {
            var window = new Settings();
            window.Owner = this;
            window.ShowDialog();
        }
*/

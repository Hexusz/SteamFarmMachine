using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
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
using BananaShooterFarm.Core;
using BananaShooterFarm.Entities;
using SteamLibrary.Core;
using SteamLibrary.Entities;

namespace BananaShooterFarm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Timer timerCheckAccounts;
        private static Timer timerCheckItems;
        private static int currentCheckItems;
        private static bool updateNow;
        private static string master = "";

        Dictionary<string, int> accItems = new Dictionary<string, int>();
        private static List<Account> Accounts { get; set; }
        private static AppConfig AppConfig { get; set; }
        private ObservableCollection<AccountStats> accountStatses = new ObservableCollection<AccountStats>();

        public MainWindow()
        {
            InitializeComponent();
            NLogger.Log.Info("Запуск приложения");
            AppFunc.LoadCryptKey();
            Accounts = AppFunc.LoadAccounts().Result.OrderBy(x => x.SteamGuardAccount.AccountName).ToList();
            AppConfig = BSFunc.LoadConfig().Result;

            foreach (var account in Accounts)
            {
                accountStatses.Add(new AccountStats() { Account = account.SteamGuardAccount.AccountName, Items = 0, Status = AccountStatus.Wait, PID = -1 });

                MenuItem masterItem = new MenuItem();
                masterItem.Header = account.SteamGuardAccount.AccountName;
                MenuItem.Items.Add(masterItem);
                masterItem.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(SetMaster));
            }

            ListViewAccounts.ItemsSource = accountStatses;
            LoginAccountsItem.IsEnabled = false;
        }

        private async void LoginAccounts_Click(object sender, RoutedEventArgs e)
        {

            LoginAccountsItem.IsEnabled = false;
            await Task.Run(LoginAccounts);
            LoginAccountsItem.IsEnabled = true;
        }

        private async Task LoginAccounts()
        {
            //Проверка наличия аккаунтов в песочнице
            if (!SteamFunc.CheckSandIni(Accounts))
            {
                MessageBox.Show("Sand not configured");
                return;
            }

            int index = 0;

            foreach (var account in Accounts)
            {
                NLogger.Log.Info($"----------Текущий аккаунт {account.SteamGuardAccount.AccountName}----------");

                accItems.Add(account.SteamGuardAccount.AccountName, SteamFunc.GetItemsCount(account.SteamGuardAccount.Session.SteamID.ToString(), "1949740", "2"));

                var currentAcc = accountStatses.FirstOrDefault(x => x.Account == account.SteamGuardAccount.AccountName);
                currentAcc.Status = AccountStatus.Launching;

                var steamLogin = await SteamFunc.SandLogin(account, AppConfig.SteamPath, AppConfig.SandBoxiePath);

                if (steamLogin)
                {
                    var proc = await BSFunc.BSStart(account, AppConfig.SteamPath, AppConfig.SandBoxiePath);

                    currentAcc.PID = proc.Id;
                    currentAcc.Status = AccountStatus.Launched;
                }
                else
                {
                    currentAcc.Status = AccountStatus.Error;
                }

                index++;
            }

            await Task.Delay(5000);

            timerCheckItems = new Timer { Interval = 31000 };
            timerCheckItems.Elapsed += CheckItemsTimer;
            timerCheckItems.Start();

            timerCheckAccounts = new Timer { Interval = 5000 };
            timerCheckAccounts.Elapsed += WatchingAccountsTimer;
            timerCheckAccounts.Start();
        }

        private async void WatchingAccountsTimer(Object source, ElapsedEventArgs e)
        {
            if (updateNow) { return; }

            updateNow = true;

            //Обновляем все PID
            BSFunc.RefreshPIDs(accountStatses);

            //Проверяем работу аккаунтов и чиним не рабочие
            await BSFunc.CheckingAndFixRunningAccounts(accountStatses);

            //Настраиваем аккаунты
            await BSFunc.SettingAccounts(accountStatses, master);

            //Проверяем состояние аккаунта
            await BSFunc.CheckStateAccounts(accountStatses, master);

            updateNow = false;

        }

        private void CheckItemsTimer(Object source, ElapsedEventArgs e)
        {
            try
            {
                var currentAccount = Accounts[currentCheckItems].SteamGuardAccount;

                var oldAccItems = accItems
                    .FirstOrDefault(x => x.Key == currentAccount.AccountName).Value;

                var currentAccountItems =
                    SteamFunc.GetItemsCount(currentAccount.Session.SteamID.ToString(), "1949740", "2");

                var difference =  currentAccountItems - oldAccItems;

                if (difference != 0)
                {
                    accountStatses.FirstOrDefault(x => x.Account == currentAccount.AccountName).Items = difference;
                }

            }
            catch (Exception exception)
            {
                NLogger.Log.Error("Ошибка получения списка предметов " + exception);
            }

            if (currentCheckItems < Accounts.Count - 1)
            {
                currentCheckItems++;
            }
            else
            {
                currentCheckItems = 0;
            }
        }

        private void SetMaster(object sender, RoutedEventArgs e)
        {
            if (master == "")
            {
                LoginAccountsItem.IsEnabled = true;
            }

            master = (sender as MenuItem)?.Header.ToString();
        }
    }
}

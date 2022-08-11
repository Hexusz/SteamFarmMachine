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
using SteamLibrary;
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


        public MainWindow()
        {
            InitializeComponent();
            NLogger.Log.Info("Запуск приложения");
            AppFunc.LoadCryptKey();
            BSFunc.Accounts = AppFunc.LoadAccounts().Result.OrderBy(x => x.SteamGuardAccount.AccountName).ToList();
            BSFunc.AppConfig = BSFunc.LoadConfig().Result;

            foreach (var account in BSFunc.Accounts)
            {
                BSFunc.AccountStatses.Add(new AccountStats()
                {
                    Account = account.SteamGuardAccount.AccountName,
                    Items = 0,
                    Status = AccountStatus.Wait,
                    PID = -1,
                    LastStatusChange = DateTime.Now
                });

                BSFunc.LastAccountStatus.Add(new LastAccountStatus()
                {
                    Account = account.SteamGuardAccount.AccountName,
                    LastStatus = AccountStatus.Wait
                });

                MenuItem masterItem = new MenuItem();
                masterItem.Header = account.SteamGuardAccount.AccountName;
                MenuItem.Items.Add(masterItem);
                masterItem.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(SetMaster));
            }

            ListViewAccounts.ItemsSource = BSFunc.AccountStatses;
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
            if (!SteamFunc.CheckSandIni(BSFunc.Accounts))
            {
                MessageBox.Show("Sand not configured, check log");
                return;
            }

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
            BSFunc.RefreshPIDs();

            //Проверяем работу аккаунтов и чиним не рабочие
            await BSFunc.CheckingAndFixRunningAccounts(BSFunc.AccountStatses);

            //Настраиваем аккаунты
            await BSFunc.SettingAccounts(BSFunc.AccountStatses, master);

            //Проверяем состояние аккаунта
            await BSFunc.CheckStateAccounts(BSFunc.AccountStatses, master);

            //Если статус аккаунта завис, то перезагружаем аккаунты
            await BSFunc.CheckChangeStateAccounts(BSFunc.AccountStatses);

            await Task.Delay(1000);
            SteamUtils.ReturnFocus(Process.GetCurrentProcess().MainWindowHandle);
            await Task.Delay(1000);

            updateNow = false;

        }

        private void CheckItemsTimer(Object source, ElapsedEventArgs e)
        {
            try
            {
                var currentAccount = BSFunc.Accounts[currentCheckItems].SteamGuardAccount;

                var oldAccItems = BSFunc.AccItems
                    .FirstOrDefault(x => x.Key == currentAccount.AccountName).Value;

                var currentAccountItems =
                    SteamFunc.GetItemsCount(currentAccount.Session.SteamID.ToString(), "1949740", "2");

                var difference = currentAccountItems - oldAccItems;

                if (difference != 0)
                {
                    BSFunc.AccountStatses.FirstOrDefault(x => x.Account == currentAccount.AccountName).Items = difference;
                }

            }
            catch (Exception exception)
            {
                NLogger.Log.Error("Ошибка получения списка предметов " + exception);
            }

            if (currentCheckItems < BSFunc.Accounts.Count - 1)
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

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            await BSFunc.RefreshAllAccount();
        }
    }
}

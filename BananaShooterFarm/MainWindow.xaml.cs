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
        private static bool updateNow;
        private static string master = "";

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

                var currentAcc = accountStatses.FirstOrDefault(x => x.Account == account.SteamGuardAccount.AccountName);
                currentAcc.Status = AccountStatus.Launch;

                var steamLogin = await SteamFunc.SandLogin(account, AppConfig.SteamPath, AppConfig.SandBoxiePath);

                if (steamLogin)
                {
                    var proc = await BSFunc.BSStart(account, AppConfig.SteamPath, AppConfig.SandBoxiePath);

                    currentAcc.PID = proc.Id;
                    currentAcc.Status = AccountStatus.Ready;
                }
                else
                {
                    currentAcc.Status = AccountStatus.Error;
                }

                index++;
            }

            await Task.Delay(5000);

            timerCheckAccounts = new Timer { Interval = 2000 };
            timerCheckAccounts.Elapsed += WatchingAccountsTimer;
            timerCheckAccounts.Start();
        }

        private async void WatchingAccountsTimer(Object source, ElapsedEventArgs e)
        {
            if (updateNow) { return; }

            updateNow = true;

            //Обновляем все PID
            BSFunc.RefreshPIDs(accountStatses);

            //Проверяем работу аккаунтов
            await BSFunc.CheckingAndFixRunningAccounts(accountStatses);

            updateNow = false;

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using BananaShooterFarm.Core;
using SteamLibrary.Core;
using SteamLibrary.Entities;

namespace BananaShooterFarm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Timer _timer = null;

        private static List<Account> Accounts { get; set; }
        private static Dictionary<string, Process> PIDs = new Dictionary<string, Process>();

        public MainWindow()
        {
            InitializeComponent();
            NLogger.Log.Info("Запуск приложения");
            AppFunc.LoadCryptKey();
            Accounts = AppFunc.LoadAccounts().Result.OrderBy(x => x.SteamGuardAccount.AccountName).ToList();
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
                return;
            }

            int index = 0;

            foreach (var account in Accounts)
            {
                NLogger.Log.Info($"----------Текущий аккаунт {account.SteamGuardAccount.AccountName}----------");

                var steamLogin = await SteamFunc.SandLogin(account, "C:\\Sandbox\\Steam", "C:\\Program Files\\Sandboxie-Plus");

                if (steamLogin)
                {
                    var proc = await BSFunc.BSStart(account, "C:\\Sandbox\\Steam", "C:\\Program Files\\Sandboxie-Plus");
                }

                index++;
            }

            await Task.Delay(5000);

            _timer = new Timer(TimerRefreshPIDs, null, 0, 5000);
        }

        private static void TimerRefreshPIDs(Object o)
        {
            PIDs = BSFunc.RefreshPIDs(Accounts);
        }
    }
}

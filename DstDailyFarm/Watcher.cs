using DstDailyFarm.Core;
using DstDailyFarm.Entities;
using SteamLibrary.Core;
using SteamLibrary.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DstDailyFarm
{
    public class Watcher
    {
        public DstAppConfig MainConfig { get; set; }
        public List<Account> Accounts { get; set; }

        public async void StartWatch()
        {
            NLogger.Log.Info("Запуск приложения");
            AppFunc.LoadCryptKey();
            MainConfig = DstFunc.LoadConfig().Result;
            Accounts = AppFunc.LoadAccounts().Result.OrderBy(x => x.SteamGuardAccount.AccountName).ToList();

            var index = 0;
            foreach (var account in Accounts)
            {
                NLogger.Log.Info($"----------Текущий аккаунт {account.SteamGuardAccount.AccountName}----------");
                NLogger.Log.Info($"Предметов в инвентаре было: {SteamFunc.GetItemsCount(account.SteamGuardAccount.Session.SteamID.ToString(), "322330", "1")}");

                var steamLogin = SteamFunc.Login(account, MainConfig.SteamPath);

                if (await steamLogin)
                {
                    var farmTask = DstFunc.StartFarm();
                }
                NLogger.Log.Info($"Предметов в инвентаре стало: {SteamFunc.GetItemsCount(account.SteamGuardAccount.Session.SteamID.ToString(), "322330", "1")}");
                index++;
            }

        }
    }
}
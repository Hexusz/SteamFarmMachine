using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSTvmFarm.Core;
using DSTvmFarm.Entities;
using NLog;

namespace DSTvmFarm
{
    public class Watcher
    {
        public AppConfig MainConfig { get; set; }
        public List<Account> Accounts { get; set; }

        public async void StartWatch()
        {
            NLogger.Log.Info("Запуск приложения");
            MainConfig = AppFunc.LoadConfig().Result;
            NLogger.Log.Info("Загрузка конфига завершена");
            Accounts = AppFunc.LoadAccounts().Result.OrderBy(x => x.Name).ToList();
            NLogger.Log.Info("Загрузка аккаунтов завершена");

            var index = 0;
            foreach (var account in Accounts)
            {
                NLogger.Log.Info($"----------Текущий аккаунт {account.Name}----------");
                NLogger.Log.Info($"Предметов в инвентаре было: {SteamFunc.GetItemsCount(account.SteamId)}");

                var steamLogin = SteamFunc.Login(index);

                if (await steamLogin)
                {
                    var farmTask = DstFunc.StartFarm();
                }
                NLogger.Log.Info($"Предметов в инвентаре стало: {SteamFunc.GetItemsCount(account.SteamId)}");
                index++;
            }

        }
    }
}
using System;
using System.Collections.Generic;
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
            Accounts = AppFunc.LoadAccounts().Result;
            NLogger.Log.Info("Загрузка аккаунтов завершена");

            var steamLogin = SteamFunc.Login(0);

            if (await steamLogin)
            {
                NLogger.Log.Info("FARM");
            }
        }
    }
}
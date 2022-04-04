using System;
using System.Collections.Generic;
using DSTvmFarm.Core;
using DSTvmFarm.Entities;
using NLog;

namespace DSTvmFarm
{
    public class Watcher
    {
        public AppConfig MainConfig { get; set; }
        public List<Account> Accounts { get; set; }

        public void StartWatch()
        {
            NLogger.Log.Info("Запуск приложения");
            MainConfig = AppFunc.LoadConfig().Result;
            NLogger.Log.Info("Загрузка конфига завершена");
            Accounts = AppFunc.LoadAccounts().Result;
            NLogger.Log.Info("Загрузка аккаунтов завершена");

            SteamFunc.Login(0, 1);
        }
    }
}